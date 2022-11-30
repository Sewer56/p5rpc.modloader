using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using Persona.Merger.Utilities;

[assembly:InternalsVisibleTo("Persona.Merger.Tests")]
namespace Persona.Merger.Cache;

/// <summary>
/// Represents a cache of merged files.
/// </summary>
public class MergedFileCache
{
    private const string JsonName = "MergedFileCache.json";
    private const string FilesFolderName = "Files";
    
    /// <summary>
    /// Time it takes for merged files to expire.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Expiration = TimeSpan.FromDays(28);

    /// <summary>
    /// Folder where this cache is contained.
    /// </summary>
    [JsonIgnore] 
    public string CacheFolder;
    
    /// <summary>
    /// Not for direct access. Public for serializer only (needed for source generation).
    /// Map of relative path to individual file.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Dictionary<string, CachedFile> KeyToFile { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    internal MergedFileCache(string cacheFolder) { CacheFolder = cacheFolder; }
    
    /// <summary> Serializer use only. Do not use directly. </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MergedFileCache() { }
    
    /// <summary>
    /// Tries to get a file from the cache.
    /// </summary>
    /// <param name="key">The key to the entry to get. Call <see cref="CreateKey"/> to get this value.</param>
    /// <param name="sources">The sources used to merge the file.</param>
    /// <param name="cachedPath">Path to the cached file.</param>
    /// <returns>
    ///    True if replacement file is present, else false.
    ///    If file is present, but is no longer valid based on last write times in sources, it is invalidated.
    /// </returns>
    public bool TryGet(string key, Span<CachedFileSource> sources, [MaybeNullWhen(false)] out string cachedPath)
    {
        cachedPath = null;
        if (!KeyToFile.TryGetValue(key, out var value))
            return false;
        
        // Note: Checking length of 2 collections is not necessary, key uses character invalid
        // in modIds, and thus should never have collisions where source count would be different.
        
        // We write it this way however to elide bounds checks.
        ref var currentSource = ref sources[0];
        ref var currentValueSource = ref value.Sources[0];
        for (int x = 0; x < sources.Length; x++)
        {
            var lastWrite = currentSource.LastWrite;
            var savedLastWrite = currentValueSource.LastWrite;
            if (lastWrite != savedLastWrite)
            {
                // Files don't match to saved, please invalidate.
                RemoveFile(key, value);
                return false;
            }

            currentSource = Unsafe.Add(ref currentSource, 1);
            currentValueSource = Unsafe.Add(ref currentValueSource, 1);
        }
        
        cachedPath = Path.Combine(CacheFolder, value.RelativePath);
        value.LastAccessed = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Adds a file to be stored in the cache.
    /// </summary>
    /// <param name="key">The key for the file.</param>
    /// <param name="sources">Sources/timestamps associated with the result file.</param>
    /// <param name="data">The data to store on disk.</param>
    public async Task AddAsync(string key, CachedFileSource[] sources, ReadOnlyMemory<byte> data)
    {
        var item = new CachedFile()
        {
            Sources = sources,
            RelativePath = GetUnusedRelativePath(out var fullPath),
            LastAccessed = DateTime.UtcNow
        };
        
        using var fileStream = new FileStream(fullPath, new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.Create,
            Options = FileOptions.SequentialScan,
            PreallocationSize = data.Length
        });

        await fileStream.WriteAsync(data);
        KeyToFile.Add(key, item);
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    public void Remove(string key)
    {
        if (KeyToFile.TryGetValue(key, out var file))
            RemoveFile(key, file);
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        var dictCopy = KeyToFile.ToArray();
        foreach (var item in dictCopy)
            RemoveFile(item.Key, item.Value);
        
        KeyToFile.Clear();
    }

    /// <summary>
    /// Removes expired items from the collection.
    /// </summary>
    public void RemoveExpiredItems()
    {
        var now = DateTime.UtcNow;
        foreach (var item in KeyToFile.ToArray())
        {
            if (!item.Value.IsExpired(now, Expiration))
                continue;
            
            RemoveFile(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Writes a file cache back to disk asynchronously.
    /// </summary>
    /// <param name="token">Token to cancel the operation.</param>
    public async Task ToPathAsync(CancellationToken token = default)
    {
        await Serialization.ToPathAsync(this, MergedFileCacheContext.Default.MergedFileCache,GetConfigPath(), token);
    }

    /// <summary>
    /// Creates a key that can be used for dictionary access. 
    /// </summary>
    /// <param name="relativePath">Relative path to the file in question.</param>
    /// <param name="modIds">Span of Mod IDs where the files are sourced from, in order.</param>
    /// <returns>Key.</returns>
    public static string CreateKey(string relativePath, Span<string> modIds)
    {
        // Very generous initial alloc, not realistic, usually IDs are around half of that size
        var builder = new StringBuilder(relativePath.Length + (modIds.Length * 50));
        foreach (var modId in modIds)
        {
            builder.Append(modId);
            builder.Append('/');
            // Mod IDs cannot (or at least should not) use forward slash because it's invalid path character.
        }
        
        builder.Append(relativePath);
        return builder.ToString();
    }
    
    /// <summary>
    /// Loads a file cache from a given path asynchronously.
    /// If cache file does not exist or is corrupted, makes new one.
    /// </summary>
    /// <param name="folderPath">The absolute file path of the cache folder.</param>
    /// <param name="token">Token that can be used to cancel deserialization</param>
    public static async Task<MergedFileCache> FromPathAsync(string folderPath, CancellationToken token = default)
    {
        var path = GetConfigPath(folderPath);
        MergedFileCache result;
        if (File.Exists(path))
        {
            result = await Serialization.FromPathAsync(path, MergedFileCacheContext.Default.MergedFileCache, token);
            result.CacheFolder = folderPath;
        }
        else
        {
            result = new MergedFileCache(folderPath);
            Directory.CreateDirectory(folderPath); // just in case.
        }
        
        return result;
    }

    /// <summary>
    /// Gets the path of the cache file config on disk.
    /// </summary>
    public string GetConfigPath() => GetConfigPath(CacheFolder);

    /// <summary>
    /// Gets the path of the cache file config on disk.
    /// </summary>
    public static string GetConfigPath(string cacheFolder) => Path.Combine(cacheFolder, JsonName);

    /// <summary>
    /// Returns an unused relative file path inside cache folder.
    /// </summary>
    private string GetUnusedRelativePath(out string fullPath)
    {
        string fileName;
        do
        {
            fileName = Path.Combine(FilesFolderName, Path.GetRandomFileName());
            fullPath = Path.Combine(CacheFolder, fileName);
        }
        while (File.Exists(fullPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fileName;
    }
    
    private void RemoveFile(string key, CachedFile value)
    {
        KeyToFile.Remove(key);
        File.Delete(value.RelativePath);
    }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(MergedFileCache))]
internal partial class MergedFileCacheContext : JsonSerializerContext { }