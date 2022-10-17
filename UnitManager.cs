using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

[System.Serializable]
public class UnitPair
{
    public GameObject unit1;
    public GameObject unit2;
    public float pairDistance;
}

[System.Serializable]
public class FlockMoveData
{
    public GameObject Unit;
    public Vector3 destinationDirection;
    public Vector3 avoidanceDirection;
    public Vector3 alignDirection;
    public Vector3 finalDirection;
    public Vector3 previousDirection;
}

public class UnitManager : MonoBehaviour
{
    public static UnitManager UM;

    public List<Material> unitMaterialList = new List<Material>();//list of unit materials
    public List<BasicObjects> allGameplayObj = new List<BasicObjects>(); //Inspector.
    public List<BotSpawner> allSpawners = new List<BotSpawner>();//inspector
    public Collider directFireColliderPrefab;
    public GameObject botDest;

    public List<GameObject> allFlockers = new List<GameObject>();
    public GameObject marker; //mouse clicks //todo this is a terrible place for this

    //flock behavior
    public bool bypassFlocker;
    public float unitAvoidDistance;//avoid distnace
    public float unitAvoidFactor;
    public float unitAlignDistance; //alignment distance
    public float unitAlignFactor;//
    public float flockCenterFactor; //offset the center of the flock
    public float unitPreviousFactor;
    //spawning
    public float flockUnitCount;
    public float flockFlockerCount;

    //Job System for map tiles
    public NativeList<Vector3> unitLocations; //location of units.  We want to find out which tile they are on.
    public NativeArray<int> tileUnitCount; //index of all tiles (1800 total in a 30x60 grid) and the number of units on the tile
    public NativeArray<float> mapTileSize; //Index of 1.  only sends the tile size
    public NativeArray<float> terrainScale; //misc data needed about the map. global position of the map, width of map, and height
    public JobHandle UnitMapTileHandle;

    void Awake()
    {
        if (UM != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            UM = this;
        }
    }

    private void Start()
    {
        //initial allocations of NativeContainers
        unitLocations = new NativeList<Vector3>(allGameplayObj.Count, Allocator.Persistent);
        tileUnitCount = new NativeArray<int>(1800, Allocator.Persistent);
        mapTileSize = new NativeArray<float>(1, Allocator.Persistent);
        terrainScale = new NativeArray<float>(4, Allocator.Persistent);
        mapTileSize[0] = MapManager.MM.tileSize;
        terrainScale[0] = MapManager.MM.terrain.transform.position.x;
        terrainScale[1] = MapManager.MM.terrain.transform.position.z;
        terrainScale[2] = MapManager.MM.terrain.transform.localScale.x;
        terrainScale[3] = MapManager.MM.terrain.transform.localScale.z;

        //print("tileUnitCount " + tileUnitCount.Length);
        //print("UnitLocations " + UnitLocations.Length);
        //print("MapTileSize " + MapTileSize.Length);
    }

    // Update is called once per frame
    void Update()
    {
        unitLocations.Clear();

        foreach (BasicObjects obj in allGameplayObj)
        {
            unitLocations.Add(obj.transform.position);
        }

        var jobData = new UnitTileUpdatesJob() {
            unitLocations = unitLocations,
            tileUnitCount = tileUnitCount,
            mapTileSize = mapTileSize,
            terrainScale = terrainScale,
        };

        UnitMapTileHandle = jobData.Schedule(allGameplayObj.Count-1, 64);

        if (GameManager.GM.currentMode == GameManager.gameModes.Commander)  //Todo should not be selected units anymore
        {
            if(Input.GetMouseButtonUp(1)) //if RMB up
            {
                MoveCommand();
            }
        }

    }

    private void LateUpdate()
    {
        UnitMapTileHandle.Complete();
        for(int i=0; i < tileUnitCount.Length -1 ;i++)
        {
            //print("Tile " + i + " unit count: " +tileUnitCount[i]);
            MapManager.MM.allTiles[i].tileTeam1UnitCount = tileUnitCount[i];  //assign data to that map tile
            tileUnitCount[i] = 0; //reset count for next cycle
        }
    }

    private void OnDestroy()
    {
        unitLocations.Dispose();
        tileUnitCount.Dispose();
        mapTileSize.Dispose();
        terrainScale.Dispose();
    }

    [BurstCompile]
    public struct UnitTileUpdatesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeList<Vector3> unitLocations;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> tileUnitCount;
        [ReadOnly]
        public NativeArray<float> mapTileSize;
        [ReadOnly]
        public NativeArray<float> terrainScale; //X pos, Z pos, widthX, heightZ

        public float tileCountZAxis;
        public float tileCountXAxis;
        public int MapIndex;


        public void Execute(int i)
        {
            //use width and height of map to find how many tiles wide/tall the map will be
            tileCountZAxis = terrainScale[3] / mapTileSize[0];
            tileCountXAxis = terrainScale[2] / mapTileSize[0];
            //Find the X coordinate and the Y coordinate of the unit
            float Xindex = Mathf.Round((((tileCountXAxis - 1) + (unitLocations[i].x / 10)) / mapTileSize[0]));      //furthest tile in row + (unitX / 10 (even if negative)) / tilesize
            float Zindex = Mathf.Round((((tileCountZAxis - 1) + (unitLocations[i].z / 10)) / mapTileSize[0]));      //furthest tile in row + (unitX / 10 (even if negative)) / tilesize
            //get the MapTile Index 
            MapIndex = (int)Xindex + ((int)tileCountXAxis * (int)Zindex);
            //print("Location X: " + UnitLocations[i].x + " X Tile: " + Xindex);
            //print("Location Z: " + UnitLocations[i].z + " Z Tile: " + Zindex);
            //print("mapindex " + MapIndex);

            //add one unit to the unit count for that tile
            int temp = tileUnitCount[MapIndex];
            temp++;
            tileUnitCount[MapIndex] = temp;


        }
    }


    public void MoveCommand()
    {
        int terrainOnlyMask = LayerMask.GetMask( "Terrain");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f,terrainOnlyMask))
        {
            //print(hit.point);
            Vector3 moveOffset = new Vector3(hit.point.x, hit.point.y + .75f, hit.point.z);
            marker.transform.position = moveOffset;

            StartCoroutine(CommanderControls(GameManager.GM.theCommander, moveOffset)); //TODO why is this 2 different functions?  join with commander controls
        }  
    }

    public IEnumerator CommanderControls(GameObject thisObj, Vector3 moveDest) 
    {
        //print("moving " + thisObj.name + " towards: " + moveDest);
        while (Vector3.Distance(thisObj.transform.position, moveDest) >= 0.5f)
        {
            if(Input.GetMouseButtonDown(1) == true) //if mouse is hit, cancel current move
            {
                yield break;
            }

            thisObj.transform.position = Vector3.MoveTowards(thisObj.transform.position, moveDest, (thisObj.GetComponent<BasicBots>().moveSpeed * Time.deltaTime)); //move it
            yield return null;
        }
    }
    
    public void SplashDamage(AttackData thisAttack) //find out what units are hit by splash damage and send 
    {
        LayerMask targetLayer = 1 << LayerMask.NameToLayer("Targeting" + thisAttack.targetUnit.team.ToString()); //set target layer as opponent team
        Collider[] splashDamageSphere = Physics.OverlapSphere(thisAttack.targetCoordinates, thisAttack.attack.splashRadius, targetLayer);//create prefab collider for splash damage
        foreach (var objectHit in splashDamageSphere)
        {
            try 
            {
                BasicObjects thisUnit = objectHit.GetComponentInParent<BasicObjects>(); //get basic object from collider
                thisUnit.TakeSplashDamage(thisAttack);
                //print("sending splash damage to unit");
            }
            catch
            {
                print("Error splash damage caught: " + thisAttack.attack + " " + objectHit.name);
            }
        }

        GameManager.GM.allAttackData.Remove(thisAttack);
    }


    // Enter a list of objects and return list of pairs to check 
    public List<UnitPair> GetUnitPairs(List<GameObject> unitsList)
    { 
        UnitPair thisPair = new UnitPair();
        List<UnitPair> pairList = new List<UnitPair>();

        //create the list of checks to do for each tile
        for (int i = 0; i < unitsList.Count; i++)
        {
            foreach (GameObject unit2 in unitsList)
            {
                if (unitsList.IndexOf(unit2) > i)
                {
                    thisPair.unit1 = unitsList[i];
                    thisPair.unit2 = unit2;
                    pairList.Add(thisPair);
                    thisPair = new UnitPair(); //reset
                }
            }
        }
        return pairList;
    }



    public IEnumerator MoveObject(GameObject thisObj, Vector3 moveDest) //move units 
    {
        thisObj.GetComponent<BasicObjects>().moveList.Add(moveDest);
        //print("moving " + thisObj.name + " towards: " + moveDest);
        while (Vector3.Distance(thisObj.transform.position, moveDest) >= 0.5f && !thisObj.GetComponent<BasicObjects>().inQueue && thisObj.GetComponent<BasicObjects>().moveList.Count > 0)
        {
            thisObj.transform.position = Vector3.MoveTowards(thisObj.transform.position, moveDest, (thisObj.GetComponent<BasicBots>().moveSpeed * Time.deltaTime)); //continue normal move
            yield return null;
        }
        //print("move over");
        thisObj.GetComponent<BasicObjects>().moveList.Remove(moveDest);
    }


}
