using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public class UnitManager : MonoBehaviour
{
    //Job System for map tiles
    public NativeList<Vector3> unitLocations; //location of units.  We want to find out which tile they are on.
    public NativeArray<int> tileUnitCount; //index of all tiles (1800 total in a 30x60 grid) and the number of units on the tile
    public NativeArray<float> mapTileSize; //Index of 1.  only sends the tile size
    public NativeArray<float> terrainScale; //misc data needed about the map. global position of the map, width of map, and height
    public JobHandle UnitMapTileHandle;

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
}
