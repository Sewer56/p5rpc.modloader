!!! info

    PAK is a general purpose data container used by Atlus.  
    Code for this emulator lives inside main project's GitHub repository.  

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

A. As currently this mod is for the PC releases of Persona games, you will need to use the extension of Persona Essentials to use with those games. The steps for using on its own are very similar to with that extension.
Add a dependency on this mod in your mod configuration. (via `Edit Mod` menu dependencies section, or in `ModConfig.json` directly)

```json
"ModDependencies": ["reloaded.universal.fileemulationframework.pak"]
```

B. Add a folder called `FEmulator/PAK` in your mod folder.  
C. Make folders corresponding to PAK Container names, e.g. `init_free.bin`.  

Files inside PAK Archives are accessed by the name of the original file, i.e. if you want to replace a file called file.tmx in the archive, you would add a file also called file.tmx into your folder.  

Inside each folder make files, with names corresponding to the file's name, if the file's name contains a folder path, place the file in a folder of the same name.  

### Example(s)

To replace a file in an archive named `init_free.bin`...

Adding `FEmulator/PAK/init_free.bin/file.tmx` to your mod would add or replace a file named `file.tmx` in the original PAK file.

Adding `FEmulator/PAK/init_free.bin/32.aix` to your mod would replace the 32th item in the original AFS Archive.

![example](../images/afs/afs_example.png)

File names can contain other text, but must start with a number corresponding to the index.  

!!! info 

    For audio playback, you can usually place ADX/AHX/AIX files interchangeably. e.g. You can place a `32.adx` file even if the original AFS archive has an AIX/AHX file inside in that slot. 

!!! info 

    A common misconception is that AF archives can only be used to store audio. This is in fact wrong. AFS archives can store any kind of data, it's just that using AFS for audio was very popular.

!!! info 

    If dealing with AFS audio; you migSht need to make sure your new files have the same channel count as the originals.   