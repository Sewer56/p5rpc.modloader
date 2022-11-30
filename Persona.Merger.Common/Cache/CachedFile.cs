namespace Persona.Merger.Cache;

/// <summary>
/// Represents an individual cached file after merging.
/// </summary>
public sealed class CachedFile
{
    /// <summary>
    /// Relative path to root of cache folder.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Paths to the original files that produced this cached file.
    /// </summary>
    public CachedFileSource[] Sources { get; set; } = Array.Empty<CachedFileSource>();

    /// <summary>
    /// Time file was last accessed. Used for flushing old files from cache.
    /// </summary>
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns true if item is expired.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="expirationTime">How long before item should expire.</param>
    public bool IsExpired(DateTime now, TimeSpan expirationTime)
    {
        var timeSinceLastAccess = now - LastAccessed;
        return timeSinceLastAccess >= expirationTime;
    }
}

/// <summary>
/// Represents an individual source of a merged file.
/// </summary>
public struct CachedFileSource
{
    /// <summary>
    /// Last write time of the file. If this doesn't match actual file, invalidate.
    /// </summary>
    public DateTime LastWrite { get; set; } = DateTime.UtcNow;

    public CachedFileSource() { }
}