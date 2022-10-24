# Usage

## Prerequisites

### Create a Reloaded Mod

Follow the guidance in the [Reloaded wiki](https://reloaded-project.github.io/Reloaded-II/CreatingMods/) to create a new Reloaded mod.  

### Download Mod

!!! note

    This image needs updated.

If you don't have it already, download the `Persona 5 Royal Essentials` Mod.  

![DownloadMod](./images/DownloadMod.png)

### Set Dependency on P5R Essentials

In the `Edit Mod` menu (right click your mod in mods list) we're going to add `Persona 5 Royal Essentials` as a dependency.  

![AddDependency](./images/AddDependency.png)

Adding a 'dependency' to your mod will make it such that P5R Essentials will always be loaded when your mod is loaded. This is a necessary step. 

## Replacing files in CPKs

!!! info

    Files inside CPKs can be replaced by creating a folder called `P5REssentials/CPK` in your mod, and adding folders corresponding to the names of the CPKs inside those folders.

### Opening the Mod Folder

![OpenModFolder](./images/OpenModFolder.png)

Go to the folder where your mod is stored, this can be done by simply clicking the `Open Folder` button.  

### Add Some Files

Make a folder called `P5REssentials`, and inside that a folder called `CPK`.   
Inside that folder, make a folder [or multiple!] where you will store your mod files (you can call it anything you want!).  

![FileRedirectorFolder](./images/CpkRedirectorFolder.png)

I used `EN.CPK` for clarity to match the game's structure.  

![FileRedirectorFolder](./images/CpkRedirectorFolder2.png)

We will replace these two files to enable different button prompts 😇.

-----

The contents of our mod folder would now look as follows.

```
// Mod Contents
ModConfig.json
Preview.png
P5REssentials
└─CPK
  └─EN.CPK
    └─BUTTON
      ├─BUTTON_XBOX.PAK
      └─BUTTONXBOXTEXINFO.DAT
```


The connectors `└─` represent folders.

## Releasing/Uploading your Mods

Please refer to the [Reloaded wiki](https://reloaded-project.github.io/Reloaded-II/EnablingUpdateSupport/), and follow the guidance.  

You should both Enable Update Support AND Publish according to the guidelines.  

It is recommended to enable update support even if you don't plan to ship updates as [doing so will allow your mod to be used in Mod Packs.](https://reloaded-project.github.io/Reloaded-II/CreatingModPacks/)