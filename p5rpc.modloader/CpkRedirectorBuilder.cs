using System.Runtime.InteropServices;
using FileEmulationFramework.Lib;
using FileEmulationFramework.Lib.IO;
using Reloaded.Mod.Interfaces;

namespace p5rpc.modloader;

public class CpkRedirectorBuilder
{
    private List<BuilderItem> _builderItems = new();
    private readonly IModLoader _loader;
    private readonly ILogger _logger;

    public CpkRedirectorBuilder(IModLoader loader, ILogger logger)
    {
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// (Conditionally) to the 
    /// </summary>
    /// <param name="modConfig"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Add(IModConfig modConfig)
    {
        var path = _loader.GetDirectoryForModId(modConfig.ModId);
        if (!TryGetCpkFolder(path, out var cpkFolder)) 
            return;
        
        // Get all CPK folders
        WindowsDirectorySearcher.TryGetDirectoryContents(cpkFolder, out _, out var directories);
        foreach (var directory in directories.Where(x => x.FullPath.EndsWith(Routes.CpkExtension, StringComparison.OrdinalIgnoreCase)))
        {
            WindowsDirectorySearcher.GetDirectoryContentsRecursive(directory.FullPath, out var files, out _);
            _builderItems.Add(new BuilderItem(directory.FullPath, files));
        }
    }

    /// <summary>
    /// Builds the bind folders :P.
    /// </summary>
    public void Build()
    {
        // This code finds duplicate files should we ever need to do merging in the future.
        var duplicates = GetDuplicateFiles();
        
        // Setup your binds here.
        foreach (var item in CollectionsMarshal.AsSpan(_builderItems))
        {
            var cpkName = Path.GetFileName(item.CpkFolderPath);
            throw new NotImplementedException("Hey Lipsum, setup your binds as needed :P");
        }
    }

    /// <summary>
    /// Finds all duplicate files stored between mods. Will be used for merging one day, for now, unused.
    /// </summary>
    /// <returns>A dictionary of relative path to full paths of duplicate files that would potentially need merging.</returns>
    public Dictionary<string, List<string>> GetDuplicateFiles()
    {
        var _relativeToFullPaths = new Dictionary<string, List<string>>();
        foreach (var item in CollectionsMarshal.AsSpan(_builderItems))
        {
            foreach (var file in item.Files)
            {
                var fullPath = Path.Combine(file.DirectoryPath, file.FileName);
                var relativePath = Route.GetRoute(Path.GetDirectoryName(item.CpkFolderPath), fullPath);
                if (!_relativeToFullPaths.TryGetValue(relativePath, out var existingPaths))
                {
                    existingPaths = new List<string>();
                    _relativeToFullPaths[relativePath] = existingPaths;
                }
                    
                existingPaths.Add(fullPath);
            }
        }
        
        // Filter out the necessary items.
        foreach (var item in _relativeToFullPaths.ToArray())
        {
            if (item.Value.Count <= 1)
                _relativeToFullPaths.Remove(item.Key);
        }

        return _relativeToFullPaths;
    }

    /// <summary>
    /// Checks if there is a folder for redirected CPK data.
    /// </summary>
    /// <param name="folderToTest">The folder to check.</param>
    /// <param name="cpkFolder">Folder containing the CPK data to redirect.</param>
    /// <returns>True if exists, else false.</returns>
    public bool TryGetCpkFolder(string folderToTest, out string cpkFolder)
    {
        cpkFolder = Path.Combine(folderToTest, Routes.CpkRedirector);
        return Directory.Exists(cpkFolder);
    }
}

internal record struct BuilderItem(string CpkFolderPath, List<FileInformation> Files);