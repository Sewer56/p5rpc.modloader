using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using FileEmulationFramework.Lib;
using FileEmulationFramework.Lib.IO;

namespace Persona.BindBuilder;

/// <summary>
/// Utility that builds paths for binding multiple files with BindFiles.
/// </summary>
public class CpkBindStringBuilder
{
    /// <summary>
    /// The delimiter character used.
    /// </summary>
    public const char Delimiter = ',';
    
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
    public CpkBindStringBuilder(string? bindFolderName = null) => BindFolderName = bindFolderName;

    /// <summary>
    /// Adds an item to be used in the output.
    /// </summary>
    /// <param name="item">The item to be included in the output.</param>
    public void AddItem(BuilderItem item) => Items.Add(item);
    
    /// <summary>
    /// Builds the bind file string.
    /// </summary>
    /// <returns>Complete list of files to be bound using the CRI bind function.</returns>
    public string Build()
    {
        // This code finds duplicate files should we ever need to do merging in the future.
        var files = GetFiles(out var duplicates);
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append(file.Value[^1]);
            builder.Append(Delimiter);
        };
        
        builder.Length--;
        builder.Append('\0');
        return builder.ToString();
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