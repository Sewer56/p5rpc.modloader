using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.Utilities;
using Persona.BindBuilder;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace p5rpc.modloader;

public class CpkBindBuilder
{
    private readonly IModLoader _loader;
    private readonly Logger _logger;
    private readonly CpkBindStringBuilder _stringBuilder;

    public CpkBindBuilder(IModLoader loader, Logger logger, IModConfig modConfig)
    {
        _loader = loader;
        _logger = logger;
        
        // Get binding directory & cleanup.
        _stringBuilder = new CpkBindStringBuilder("R2");
    }

    /// <summary>
    /// (Conditionally) adds CPK folders for binding to the builder.
    /// </summary>
    /// <param name="modConfig">Mod configuration.</param>
    public void Add(IModConfig modConfig)
    {
        var path = _loader.GetDirectoryForModId(modConfig.ModId);
        if (!TryGetCpkFolder(path, out var cpkFolder)) 
            return;
        
        _logger.Info("Adding CPK Folder: {0}", cpkFolder);
        
        // Get all CPK folders
        WindowsDirectorySearcher.TryGetDirectoryContents(cpkFolder, out _, out var directories);
        foreach (var directory in directories)
        {
            WindowsDirectorySearcher.GetDirectoryContentsRecursive(directory.FullPath, out var files, out _);
            _stringBuilder.AddItem(new BuilderItem(directory.FullPath, files));
        }
    }

    /// <summary>
    /// Builds the bind folders :P.
    /// </summary>
    public CpkBinder Build(IReloadedHooks hooks)
    {
        // This code finds duplicate files should we ever need to do merging in the future.
        var bindString = _stringBuilder.Build();
        return new CpkBinder(bindString, _logger, hooks);
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