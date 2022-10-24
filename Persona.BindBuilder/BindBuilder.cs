using System.Collections.Concurrent;
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

    /// <summary>
    /// If set all data will be bound under this name, else not.
    /// </summary>
    public string? BindFolderName { get; private set; } = null!;

    /// <summary/>
    /// <param name="outputFolder">The folder where the generated data to be bound will be stored.</param>
    public BindBuilder(string outputFolder, string? bindFolderName = null)
    {
        OutputFolder = outputFolder;
        BindFolderName = bindFolderName;
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
        
        // Normalize keys so all mods go in same base directory

        // TODO: Add the merging infrastructure. For now, we will accept last added file as the winner.
        // For the merging infra, we will commit the merging (check against cache first), put result in cache folder.
        // And replace the key,value combination with just the cached merged file.
        
        // Note: We are not worried about threading in this hashSet. 
        // Lack of synchronization just means it might accidentally create directory when it shouldn't, but given the
        // (small) number of directories this will be unlikely. Performance wise this is better than using concurrent one.
        var createdFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (files.Count > 1000) // TODO: Benchmark around for a good number.
        {
            var keyValuePairs = files.ToArray();
            Parallel.ForEach(Partitioner.Create(0, files.Count), (range, state) =>
            {
                for (int x = range.Item1; x < range.Item2; x++)
                    HardlinkFile(keyValuePairs[x], createdFolders);
            });
        }
        else
        {
            foreach (var file in files)
                HardlinkFile(file, createdFolders);
        }
        
        return OutputFolder;
    }

    private void HardlinkFile(KeyValuePair<string, List<string>> file, HashSet<string> createdFolders)
    {
        var hardlinkPath = Path.Combine(OutputFolder, file.Key);
        var newFile = file.Value.Last();
        var directory = Path.GetDirectoryName(hardlinkPath);

        if (!createdFolders.Contains(directory))
        {
            Directory.CreateDirectory(directory);
            createdFolders.Add(directory);
        }

        Native.CreateHardLink(hardlinkPath, newFile, IntPtr.Zero);
    }

    /// <summary>
    /// Finds all files within the given builder items.
    /// </summary>
    /// <param name="duplicates">List of all duplicates stored between mods.</param>
    /// <returns>A dictionary of relative path [in custom bind folder] to full paths of duplicate files that would potentially need merging.</returns>
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
                
                // Inject custom bind folder name.
                relativePath = string.IsNullOrEmpty(BindFolderName) ? relativePath : ReplaceFirstFolderInPath(relativePath, BindFolderName);
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
    
    private string ReplaceFirstFolderInPath(string originalRelativePath, string newFolderName)
    {
        var separatorIndex = originalRelativePath.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex == -1)
            separatorIndex = originalRelativePath.IndexOf(Path.AltDirectorySeparatorChar);
        
        return newFolderName + Path.DirectorySeparatorChar + originalRelativePath.Substring(separatorIndex + 1);
    }
}

/// <summary>
/// Represents an individual item that can be submitted to the builder.
/// </summary>
/// <param name="folderPath">Path to the base folder containing the contents.</param>
/// <param name="Files">The contents of said base folder.</param>
public record struct BuilderItem(string folderPath, List<FileInformation> Files);