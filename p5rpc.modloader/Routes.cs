namespace p5rpc.modloader;

/// <summary>
/// Locations of items inside 3rd party mod packages.
/// </summary>
public static class Routes
{
    /// <summary>
    /// Extension for the CPK file format.
    /// </summary>
    public const string CpkExtension = ".cpk";
    
    /// <summary>
    /// Relative file path used by the CPK redirector.
    /// </summary>
    public const string CpkRedirector = "P5REssentials/CPK";

    /// <summary>
    /// Gets the base directory used for binding of CPKs.
    /// </summary>
    /// <param name="modConfigDirectory">Config directory for the Reloaded mod.</param>
    public static string GetBindBaseDirectory(string modConfigDirectory) => Path.Combine(modConfigDirectory, "Bind");
    
    /// <summary>
    /// Gets the base directory used for caching merged files.
    /// </summary>
    /// <param name="modConfigDirectory">Config directory for the Reloaded mod.</param>
    public static string GetCacheDirectory(string modConfigDirectory) => Path.Combine(modConfigDirectory, "Cache");
}