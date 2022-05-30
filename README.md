# Zat's Kingdoms and Castles Mods

This repository contains my mods and mod frameworks for the game Kingdoms and Castles (which I highly recommend you to play).

## Frameworks

So far this project implements the following frameworks:

-   _Zat.InterModComm_: Provides functionality to perform remote procedure calls (RPC) between multiple mods. You can use this to perform calls and bind events between mods.
-   _Zat.ModMenu.API_: An API you can use in your own mod projects to interact with the mod menu. You can define settings and categories, subscribe to changes and update UI elements programmatically.

Please refer to the [wiki](https://github.com/LionShield/Kingdoms-and-Castles-Toolkit/wiki) on how to use these frameworks.

## Scripts

The "Scripts" Visual Studio solution contains all code of my mods and mod frameworks.

### Setup

In order to set this repo up on your machine, follow these steps:

1. Clone or download the respository
2. Copy all Libraries (`*.dll` files) from the folder games' folder `Kingdoms and Castles\KingdomsAndCastles_Data\Managed` into the folder `Scripts\GameData`. This way Visual Studio will detect all references to the game's code and libraries.

_> Note: If all you wish to do is interface with the mod menu, please read the [wiki article](https://github.com/LionShield/Kingdoms-and-Castles-Toolkit/wiki/2.2-%7C-ModMenu-by-Zat) instead!_

### Create your own Mod

To create your own mod, simply create a new folder in the `Scripts` folder in Visual Studio, e.g. _MyAwesomeMod_. If you wish to use my shared classes (located in `Scripts\Shared`), simply import their namespaces - and don't forget to deploy them later on!

### Deploying mods

If you want to test your mods in-game, copy your mod folder (e.g. `Scripts\MyAwesomeMod`) to the game's mod folder (`Kingdoms and Castles\KingdomsAndCastles_Data\mods`). If you use any of my shared classes, copy them over with your mods' files (`Kingdoms and Castles\KingdomsAndCastles_Data\mods\MyAwesomeMod`).
  
To automate this process use ArchieV1's compiler script. Find it [here](https://github.com/ArchieV1/KC_compiler).
Have all of the shared classes files you want to use be in the same folder as your code and set the variables inside of the "compiler"

## Unity Project

The "Unity Project" folder can be opened with Unity 2018.2.5f1. It contains the default data from the [Kingdoms and Castles Toolkit](https://github.com/mpeddicord/Kingdoms-and-Castles-Toolkit) plus all of my prefabs, sprites and models.
