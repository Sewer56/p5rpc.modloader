!!! info

    PAK is a general purpose data container used by Atlus.  
    Code for this emulator lives inside the Persona Essentials's GitHub repository.  

## File Types

This emulator should work with type of PAKs, regardless of what the actual file is named. 

The most common names/extensions for PAKs are:
- PAK
- BIN
- PAC
- ARC

## Supported Applications

The only games that have so far been found to use PAKs are Persona games, but theoretically should work with any game that uses them.

It has been tested with the following:  
- Persona 3 Portable (PC)  
- Persona 4 Golden (PC)  
- Persona 5 Royal (PC)  

## Example Usage

A. Add a dependency on this mod in your mod configuration. (via `Edit Mod` menu dependencies section, or in `ModConfig.json` directly)

```json
"ModDependencies": ["p5rpc.modloader.pak"]
```

B. Add a folder called `FEmulator/PAK` in your mod folder.  
C. Make folders corresponding to PAK Container names, e.g. `init_free.bin`.  

Files inside PAK Archives are accessed by the name of the original file, i.e. if you want to replace a file called file.tmx in the archive, you would add a file also called file.tmx into your folder.  

Inside each folder make files, with names corresponding to the file's name, if the file's name contains a folder path, place the file in a folder of the same name.  
e each folder make files, with names corresponding to the file's name, if the file's name contains a folder path, place the file in a folder of the same name.  

### Language

If you are playing the game in a language other than English, you can set your games language in the Config settings for this mod.

### PAKs within PAKs

Various PAK files in the Persona series use PAK files nested within other PAK files. 

To edit these nested PAKs, simply make a folder within the PAK file folder you already made with the same name as the nested PAK file.

### Example(s)

To replace a file in an archive named `init_free.bin`...

Adding `FEmulator/PAK/init_free.bin/file.tmx` to your mod would add or replace a file named `file.tmx` in the original PAK Container.

Adding `FEmulator/PAK/init_free.bin/field/fldEff_rainA.tmx` to your mod would add or replace a file named `field/fldEff_rainA.tmx` in the original PAK Container.

Adding `FEmulator/PAK/init_free.bin/init/loading.arc/mini_tv.tmx` to your mod would add or replace a file named `mini_tv.tmx` in the PAK file `loading.arc` within the original PAK Container.
