# UnityDemo-JobSystem
My First functional use of the job system in Unity. I'm creating an RTS type game and am working on the performance of the targeting system.  Part of those changes are making units only attempt to target enemies if there are enemies nearby. This Job performs the same task as a method I had written that runs in the main thread, but performs it much much faster.  

I'm starting with a List of Class MapTiles that have:
  an X/Z coordinate in the Unity game space (currently the map is a flat plane, so no Y Axis yet), 
  a Tile Index #, starting with 0 at the bottom left corner and ends in the upper right corner.  
  an Int counter for number of enemies on that tile,
  standard size of 2x2.
A List of Units that contain an X and Z coordiantes.

The structure of the job itself is an IJobParallelFor.  It loops through each Unit in the game and determines which tile the unit is on using the Tile Index, this way I can return this info to the same MapTile instance after the job is finshed.

Libraries in use (mostly).  
![image](https://user-images.githubusercontent.com/107947089/196184258-57315ea0-334d-4150-814f-2896219492b8.png)

Declaration of NAtive Containers used to to pass data to the job. Be sure to install the Collections Package in Unity to allow the use of a NativeLists, I didn't have it at first and it was very helpful to have unitLocations as a list. 
![image](https://user-images.githubusercontent.com/107947089/196184587-f3d24d58-521c-4529-a9c5-617d860d03cc.png)



