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

In Start() I assign the values to the Arrays.  The number of Tiles and terrain info doesn't change, but the unitcount will change each time this is run so that is assigned in Update() and works like a normal list allowing me to resize as needed
![image](https://user-images.githubusercontent.com/107947089/196185430-4bc8b68e-c771-474a-9def-7874fa99a6a9.png)
![image](https://user-images.githubusercontent.com/107947089/196185540-c2bd34a7-1510-45e4-b110-91a30689da6f.png)

Creation of the Job itself. Declaring the same NativeContainers in the job so that the data can pass back and forth between the main thread and the job. Temporary variables can also be assigned for use in the Job.
![image](https://user-images.githubusercontent.com/107947089/196185940-87c6948e-8991-4993-9fb4-8807b800c316.png)

Execute method in the Job. because this is an IJobParallelFor it will act like a foreach loop on the index we supply later when scheduling the job. In this case I will provide the Unit Locations list to the job so each unit is checked one at a time.  
![image](https://user-images.githubusercontent.com/107947089/196186340-f6282e22-fe9b-4829-bf6a-dede57c426a8.png)

One issue I ran into with the IJobParallelFor:  The job is restricted from editing data in other NativeContainers in Execute as part of Unity's safety system on Jobs.  In order for me to write data to tileUnitCount[MapIndex] I had to add the tag [NativeDisableParallelForRestriction] to the declaration on the Job.  Because I am just taking the int at that index and adding one I think this is safe and race conditions shouldn't be an issue *fingers crossed*
![image](https://user-images.githubusercontent.com/107947089/196193628-6e233420-9110-430c-b094-59358f8ad101.png)








