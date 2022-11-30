namespace Persona.Merger.Utilities;

/// <summary>
/// IO Related Extensions.
/// </summary>
// ReSharper disable once InconsistentNaming
public class IOEx
{
    /// <summary>
    /// Waits for write access to be available for a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="mode">A mode that determines whether the file should be opened, created etc.</param>
    /// <param name="access">Required access types for the file.</param>
    /// <param name="token">The token.</param>
    public static async Task<FileStream?> OpenFileAsync(string filePath, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, CancellationToken token = default)
    {
        FileStream? stream;
        while ((stream = TryOpenOrCreateFileStream(filePath, mode, access)) == null)
        {
            if (token.IsCancellationRequested)
                return null;

            await Task.Delay(1, token);
        }

        return stream;
    }
    
    /// <summary>
    /// Moves a file, waiting infinitely until write access is available.
    /// </summary>
    /// <param name="source">The path to the file.</param>
    /// <param name="destination">Where the file should be moved to.</param>
    /// <param name="token">The token.</param>
    public static async Task MoveFileAsync(string source, string destination, CancellationToken token = default)
    {
        while (true)
        {
            if (TryMoveFile(source, destination, token))
                break;

            await Task.Delay(1, token);
        }
    }
    
    
    
    /// <summary>
    /// Tries to open a stream for a specified file.
    /// Returns null if it fails due to file lock.
    /// </summary>
    private static FileStream? TryOpenOrCreateFileStream(string filePath, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite)
    {
        try
        {
            return File.Open(filePath, mode, access);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Tries to move a file, returns true if success, else false.
    /// </summary>
    /// <param name="source">The path to the file.</param>
    /// <param name="destination">Where the file should be moved to.</param>
    /// <param name="token">The token.</param>
    private static bool TryMoveFile(string source, string destination, CancellationToken token = default)
    {
        try
        {
            if (token.IsCancellationRequested)
                return true;

            File.Move(source, destination, true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}