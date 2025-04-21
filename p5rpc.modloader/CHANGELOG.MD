# 2.9.2

- Fix JSON serialisation error due to trimming Persona.Merger.Common.

# 2.9.0

- Added support for [Localisation Framework](https://github.com/AnimatedSwine37/Reloaded.Universal.Localisation.Framework) allowing for improved mod translation support.

This will automatically pick up your language from Steam and/or Windows settings,
and apply it to the game. No more need to manually change the language.

Mod authors can now also prepare files specific to a given language, as opposed
to only supporting English.

# 2.8.0

- Fixed TBL Merging bug related to expanded TBL files
- Added generic file merging for a bunch of files for P3P, P4G and P5R

# 2.7.1

- Fixed: Introskip on P5R Ver 1.04

# 2.0.0

- Added: TBL Merging

# 1.2.2

- Fixed: Incorrect update info for dependencies.  
- Changed: Cleaned up code a bit.  

# 1.2.0

- Changed: All CRI Related functionality moved to separate mod `Cri FileSystem V2 Hook`.  

This also means that all settings related to that functionality moved to said mod too.  

- Added: Support for `Hot Reload`, files can now be replaced at runtime without requiring a game restart.  
[Note: Some files may be cached by game and a restart will be required anyway]

- Fixed: Mod load order not being respected during build.  
- Added: Support for injecting custom music via AWB Emulator extension.  
- Added: Support for Render in Background for P4G 64-bit & P5R GamePass.  

# 1.1.0

- Added: Support for Persona 4 Golden Remaster Edition (a.k.a. P4G64).  

# 1.0.2

- Changed: Mod now supports folders not ending with CPK.

# 1.0.1

- Added: Source Code URL to launcher.