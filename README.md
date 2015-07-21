# UnityGendarmerie
Simple hooks for using Gendarme from inside the Unity editor!

![](/Downloads/ManualImages/UnityGendarmerie.gif)


**Go here for the .unitypackage you need to get running!**  
https://github.com/maluoi/UnityGendarmerie/tree/master/Downloads  
**And go here for the Gendarme installers, which you will also need!**  
https://github.com/spouliot/gendarme/downloads


Notes:
- First, ensure you have gendarme on your computer!
- It may also be good to be aware of the default ignore.txt file that should be at Assets/Ferr/UnityGendarmerie/ignore.txt
- UnityGendarmerie is set up by default to work on PC with the Gendarme installer! If you're on a Mac, or aren't using the installer, you'll need to specify the location in Assets/Ferr/UnityGendarmerie/configuration.txt!

Basic usage:
- Use Tools->Ferr UnityGendarmerie->Run Static Code Analysis (Runtime code) to analyze files that are distributed with your game, basically most things that aren't in the Editor folders
- Use Tools->Ferr UnityGendarmerie->Run Static Code Analysis (Editor code) to analyze your custom editor code!
- Right Click->Ferr UnityGendarmerie->Analyze Code on a .cs file, or a folder to analyze specific files, or folders of files. Use the warning levels to filter out lower level warnings.
  
Want better access to the data, or want to analyze other assemblies? Fret not!
- `UnityGendarmerie.AnalyzeCode(new Uri("filename.exe"), true);`
- `AnalyzeCode` has options for specifying specific assemblies, filters, and custom ignore files! It also returns a list of the data in case you're interested in logging or doing something else with it yourself!
- Use `AnalyzeCodePath` to do the same on a specific file or folder, this will only work for files that fit into the runtime and editor assemblies!


Also, here's an example ignore file, if you need some reference!  
https://github.com/mono/mono-tools/blob/master/gendarme/self-test.ignore
