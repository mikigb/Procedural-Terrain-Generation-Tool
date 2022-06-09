# Procedural-Terrain-Generation-Tool
Unity tool to procedurally generate 'infinite' worlds based on different levels of perlin noise. You can choose the amount of biomes you want to create as well as its characteristics (height, probability) and the assets to populate them (ground textures, trees, rocks and billboard grass).

# Installation
After downloading all the files in this project create a new a Unity 3D Core project. 
It has been tested on the version 2019.4.8f1 of Unity and can't assure it would work on different versions. 
Once the project has been created, import all the files as they are, with the folders and the scripts inside.

After this you're going to have to make a few changes in the "Resources/Materials" folder. Select the file "grassMaterial" and 
change the dropdown "Shader" to "Custom/Grass". With the file "meshMaterial" you are going to do the same but this time select
the option "Custom/Terrain".

The textures you will import will have to have the next parameters for the scripts to work with them:
  1. For the surface textures, the ones you will place on the ground, you will select the parameters highlighted in red:
  
      ![texturas](https://user-images.githubusercontent.com/37048338/172803859-0d22201e-8800-43ca-95b3-ae5065934b9c.PNG)
      
  2. For the grass textures, you will select the parameters highlighted in red:
  
      ![texturas hierva](https://user-images.githubusercontent.com/37048338/172804272-916eeb0e-6bd3-4c11-a6c9-4932825d7b53.PNG)

After these changes the installation is completed and you will be able to start with the customization of the terrain.

# Tool usage
First of all you must create an empty object in the scene. In that object you will have to select a new script called "Terrain",
you can also find it on the scripts folder you just imported. This is the main script that will allow you to create the terrain and the 
first time you will find it like this:

![Menu1](https://user-images.githubusercontent.com/37048338/172806560-adc00202-ef7f-4d87-a63a-74896e5eaac1.PNG)

On this first menu you can select the seed that it will generate (four digits), the number of chunks displayed at the same time and if you want to randomize the seed 
in which case it will ignore the one selected before. On the "Player Transform" option you are required to indicate the transform of the player you want to use as it will 
generate chunks around it. You also need to provide a water object which will also be displaced around the position of the player although this is optional.

## Biomes
The next thing you are going to do, is select the number of biomes you want to create (with a maximum number of 10):

![menu2](https://user-images.githubusercontent.com/37048338/172808594-663c2ccb-7b86-4272-9031-270ab7d8bfbf.PNG)

Each element represents a biome and will have the next parameters:

![menu3](https://user-images.githubusercontent.com/37048338/172809119-2a489334-cff4-4941-9c30-cc8f5d921ce0.PNG)

As you can see, you can select the randomness of the biome which will determine the chances of encountering this biome (range of -1 to 1). 
And also the maximum height of the biome (0 to 1).

If you select the option "Ground Textures" the next options will show up:

![menu4](https://user-images.githubusercontent.com/37048338/172809947-a5d0f6ec-a720-43e5-8370-d1b0474166d9.PNG)

You can select the textures for the ground based on the minimum height in which they will appear (being 0 the minimum and 1 the maximum)

The option "Ground Textures Sloped" will let you select a texture for the ground once it pass the "Slope Threshold" you could find in the "Biome Controller" menu.

The "Large Models" and "Small models" options are customized in the same way. You select the prefab you want to use and the randomness which determines how 
much it will appear in the biome:

![menu5](https://user-images.githubusercontent.com/37048338/172811132-3b246859-7c87-42a2-a80f-85edd9893870.PNG)

The main difference between these two options is the size you of the prefabs you will use in each of them since the large one will render objects farther than the small one.

Lastly, you will have the "Grass Models" option where you will be able to provide the textures of the grass as well as its randomness and the minimum height at which 
it will spawn:

![menu6](https://user-images.githubusercontent.com/37048338/172811716-fe75d036-4c53-426f-bdbd-3b2f721432da.PNG)

After all of these changes you will be able to press play and see the results of the generation.
