using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FileEmulationFramework.Lib;
using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.Utilities;
using Native = Persona.BindBuilder.Utilities.Native;

[module: SkipLocalsInit]

namespace Persona.BindBuilder;

/// <summary>
/// Utility that builds folders with files that we will inject into games through any of the following methods
/// - File Emulation
/// - Binding (e.g. CRI CPK Binding)
/// </summary>
public class BindBuilder
{
    // TODO: Caching.
    // TODO: Multi-thread.
    
    /// <summary>
    /// The folder where all the data to be bound will be stored.
    /// </summary>
    public string OutputFolder { get; private set; }

    /// <summary>
    /// Current list of items that will constitute the final output.
    /// </summary>
    public List<BuilderItem> Items { get; private set; } = new();

    /// <summary/>
    /// <param name="outputFolder">The folder where the generated data to be bound will be stored.</param>
    public BindBuilder(string outputFolder)
    {
        OutputFolder = outputFolder;
    }

    /// <summary>
    /// Adds an item to be used in the output.
    /// </summary>
    /// <param name="item">The item to be included in the output.</param>
    public void AddItem(BuilderItem item) => Items.Add(item);
    
    /// <summary>
    /// Builds the bind folders :P.
    /// </summary>
    /// <returns>The folder inside which bound data is contained.</returns>
    public string Build()
    {
        // This code finds duplicate files should we ever need to do merging in the future.
        var files = GetFiles(out var duplicates);
        
        // TODO: Add the merging infrastructure. For now, we will accept last added file as the winner.
        // For the merging infra, we will commit the merging (check against cache first), put result in cache folder.
        // And replace the key,value combination with just the cached merged file.
        foreach (var file in files)
        {
            var hardlinkPath = Path.Combine(OutputFolder, file.Key);
            var newFile = file.Value.Last();
            Directory.CreateDirectory(Path.GetDirectoryName(hardlinkPath));
            Native.CreateHardLink(hardlinkPath, newFile, IntPtr.Zero);
        }

        return OutputFolder;
    }

    /// <summary>
    /// Finds all files within the given builder items.
    /// </summary>
    /// <param name="duplicates">List of all duplicates stored between mods.</param>
    /// <returns>A dictionary of relative path to full paths of duplicate files that would potentially need merging.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Dictionary<string, List<string>> GetFiles(out Dictionary<string, List<string>> duplicates)
    {
        var _relativeToFullPaths = new Dictionary<string, List<string>>();
        foreach (var item in CollectionsMarshal.AsSpan(Items))
        {
            foreach (var file in item.Files)
            {
                var fullPath = Path.Combine(file.DirectoryPath, file.FileName);
                var relativePath = Route.GetRoute(Path.GetDirectoryName(item.folderPath)!, fullPath);
                if (!_relativeToFullPaths.TryGetValue(relativePath, out var existingPaths))
                {
                    existingPaths = new List<string>();
                    _relativeToFullPaths[relativePath] = existingPaths;
                }
                    
                existingPaths.Add(fullPath);
            }
        }
        
        // Filter out the necessary items.
        duplicates = new Dictionary<string, List<string>>(_relativeToFullPaths);
        foreach (var item in _relativeToFullPaths)
        {
            if (item.Value.Count <= 1)
                duplicates.Remove(item.Key);
        }

        return _relativeToFullPaths;
    }
}

/// <summary>
/// Represents an individual item that can be submitted to the builder.
/// </summary>
/// <param name="folderPath">Path to the base folder containing the contents.</param>
/// <param name="Files">The contents of said base folder.</param>
public record struct BuilderItem(string folderPath, List<FileInformation> Files);