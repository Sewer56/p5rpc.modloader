using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.IO;
using Persona.BindBuilder.Interfaces;

namespace Persona.BindBuilder;

/// <summary>
/// Generator of output directories for binding that uses process IDs.
/// </summary>
public class BindingOutputDirectoryGenerator
{
    /// <summary>
    /// Base directory inside which all temporary directories will be stored.
    /// </summary>
    public string BaseDirectory { get; private set; }
    
    public BindingOutputDirectoryGenerator(string baseDirectory) => BaseDirectory = baseDirectory;

    /// <summary>
    /// Generates a directory that can contain binding data.
    /// </summary>
    /// <param name="currentProcessProvider">Provides ID of current process.</param>
    /// <returns>Directory for storing this process' binding data.</returns>
    public string Generate(ICurrentProcessProvider currentProcessProvider)
    {
        var path = Path.Combine(BaseDirectory, currentProcessProvider.GetProcessId().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Removes directories that are not in use anymore.
    /// </summary>
    /// <param name="list">List of active processes.</param>
    public void Cleanup(IProcessListProvider list)
    {
        var procIds = list.GetProcessIds().ToHashSet();
        var directories = GetAllDirectories();
        foreach (var directory in CollectionsMarshal.AsSpan(directories))
        {
            if (procIds.Contains(directory.id))
                continue;

            try { Directory.Delete(directory.fullPath, true); }
            catch (Exception) { /* ignored */ }
        }
    }

    /// <summary>
    /// Returns all directory entries.
    /// </summary>
    public List<GeneratorDirectoryEntry> GetAllDirectories()
    {
        WindowsDirectorySearcher.TryGetDirectoryContents(BaseDirectory, out _, out var directories);
        var results = new List<GeneratorDirectoryEntry>(directories.Count);
        foreach (var directory in CollectionsMarshal.AsSpan(directories))
        {
            var idStr = Path.GetFileNameWithoutExtension(directory.FullPath.AsSpan());
            if (int.TryParse(idStr, out int id))
                results.Add(new GeneratorDirectoryEntry(id, directory.FullPath));
        }

        return results;
    }
}

/// <summary>
/// Represents an entry in the list of bind output directories.
/// </summary>
/// <param name="id">ID of the directory.</param>
/// <param name="fullPath">Full path to the directory.</param>
public record struct GeneratorDirectoryEntry(int id, string fullPath);