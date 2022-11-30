using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Persona.Merger.Utilities;

/// <summary>
/// Utility methods related to serialization.
/// </summary>
public static class Serialization
{
    /// <summary>
    /// Loads a file from a given path asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute file path of the file.</param>
    /// <param name="info">Info for serializer.</param>
    /// <param name="token">Token that can be used to cancel deserialization</param>
    public static async Task<T> FromPathAsync<T>(string filePath, JsonTypeInfo<T> info, CancellationToken token = default)
    {
        int numAttempts = 0;
        int sleepTime   = 32;

        while (true)
        {
            try
            {
                await using var stream = await IOEx.OpenFileAsync(filePath, FileMode.Open, FileAccess.Read, token);
                if (stream != null)
                    return (await JsonSerializer.DeserializeAsync(stream, info, token))!;

                throw new TaskCanceledException();
            }
            catch (Exception)
            {
                if (numAttempts >= 6)
                    throw;

                numAttempts++;
                await Task.Delay(sleepTime, token);
                sleepTime *= 2;
            }
        }
    }

    /// <summary>
    /// Writes a given file path to disk.
    /// </summary>
    /// <param name="config">The mod configurations to commit to file.</param>
    /// <param name="info">Info for the serializer.</param>
    /// <param name="filePath">The absolute path to write the configurations file to.</param>
    /// <param name="token">Token that can be used to cancel deserialization</param>
    public static async Task ToPathAsync<T>(T config, JsonTypeInfo<T> info, string filePath, CancellationToken token = default)
    {
        string fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var tempPath = $"{fullPath}.{Path.GetRandomFileName()}";
        try
        {
            await using (var stream = await IOEx.OpenFileAsync(tempPath, FileMode.Create, FileAccess.Write, token))
            {
                if (token.IsCancellationRequested)
                    return;
                
                await JsonSerializer.SerializeAsync(stream!, config, info, token);
            }

            await IOEx.MoveFileAsync(tempPath, fullPath, token);
        }
        catch (TaskCanceledException)
        {
            File.Delete(tempPath);
        }
    }
}