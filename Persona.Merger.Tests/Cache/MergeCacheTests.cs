using Persona.Merger.Cache;

namespace Persona.Merger.Tests.Cache;

public class MergeCacheTests
{
    [Fact]
    public async Task Add_WithValidData()
    {
        // Arrange
        var folderPath = "Test/Add_WithValidData";
        DeleteDirectory(folderPath);
        Directory.CreateDirectory(folderPath);

        var data = GetRandomBytes(64);
        var dummyKey = MergedFileCache.CreateKey("coolfile.png", new[] { "uwu" });
        var lastModified = DateTime.Now;
        
        // Act
        var dummyCache = new MergedFileCache(folderPath);
        await dummyCache.AddAsync(dummyKey, new[] { new CachedFileSource() { LastWrite = lastModified } }, data);
        
        // Assert
        Assert.True(dummyCache.TryGet(dummyKey, new CachedFileSource[] { new() { LastWrite = lastModified } }, out string path));
        Assert.NotNull(path);
        Assert.True(File.Exists(path));    
    }
    
    [Fact]
    public async Task RemoveExpiredItems_AfterExpiry()
    {
        // Arrange
        var folderPath = "Test/RemoveExpiredItems_AfterExpiry";
        DeleteDirectory(folderPath);
        Directory.CreateDirectory(folderPath);

        var dummyKey = MergedFileCache.CreateKey("coolfile.png", new[] { "uwu" });
        var lastModified = DateTime.Now;
        
        // Act
        var dummyCache = new MergedFileCache(folderPath);        
        var dummyFile = new CachedFile()
        {
            RelativePath = "dummy", 
            Sources = new[] { new CachedFileSource() { LastWrite = lastModified } }, 
            LastAccessed = lastModified  - dummyCache.Expiration
        };
        
        dummyCache.KeyToFile.Add(dummyKey, dummyFile);
        
        // Assert
        Assert.True(dummyCache.KeyToFile.ContainsKey(dummyKey));
        dummyCache.RemoveExpiredItems();
        Assert.False(dummyCache.KeyToFile.ContainsKey(dummyKey));
    }

    [Fact]
    public async Task TryGet_LastAccessed_UpdatesWhen()
    {
        // Arrange
        var folderPath = "Test/TryGet_Deletes_WhenLastModifiedNotMatch";
        DeleteDirectory(folderPath);
        
        var dummyKey = "temp+coolFile.png";
        var lastModified = DateTime.Now;
        var dummyCache = new MergedFileCache(folderPath);
        var dummyFile = new CachedFile()
        {
            RelativePath = "dummy", 
            Sources = new[] { new CachedFileSource() { LastWrite = lastModified } }, 
            LastAccessed = DateTime.Now
        };
        
        dummyCache.KeyToFile.Add(dummyKey, dummyFile);

        // Assert
        Assert.True(dummyCache.KeyToFile.ContainsKey(dummyKey));
        Assert.True(dummyCache.TryGet(dummyKey, new CachedFileSource[] { new() { LastWrite = lastModified } }, out string path));
        Assert.NotNull(path);
        Assert.NotEqual(lastModified, dummyCache.KeyToFile[dummyKey].LastAccessed);    
    }
    
    [Fact]
    public async Task TryGet_Success_WhenLastModifiedMatch()
    {
        // Arrange
        var folderPath = "Test/TryGet_Deletes_WhenLastModifiedNotMatch";
        DeleteDirectory(folderPath);
        
        var dummyKey = "temp+coolFile.png";
        var lastModified = DateTime.Now;
        var dummyCache = new MergedFileCache(folderPath);
        var dummyFile = new CachedFile()
        {
            RelativePath = "dummy", 
            Sources = new[] { new CachedFileSource() { LastWrite = lastModified } }, 
            LastAccessed = DateTime.Now
        };
        
        dummyCache.KeyToFile.Add(dummyKey, dummyFile);

        // Assert
        Assert.True(dummyCache.KeyToFile.ContainsKey(dummyKey));
        Assert.True(dummyCache.TryGet(dummyKey, new CachedFileSource[] { new() { LastWrite = lastModified } }, out _));
        Assert.True(dummyCache.KeyToFile.ContainsKey(dummyKey));
    }
    
    [Fact]
    public async Task TryGet_Deletes_WhenLastModifiedNotMatch()
    {
        // Arrange
        var folderPath = "Test/TryGet_Deletes_WhenLastModifiedNotMatch";
        DeleteDirectory(folderPath);
        
        var dummyKey = "temp+coolFile.png";
        var dummyCache = new MergedFileCache(folderPath);
        var dummyFile = new CachedFile()
        {
            RelativePath = "dummy", 
            Sources = new[] { new CachedFileSource() { LastWrite = DateTime.Now } }, 
            LastAccessed = DateTime.Now
        };
        dummyCache.KeyToFile.Add(dummyKey, dummyFile);

        // Assert
        Assert.True(dummyCache.KeyToFile.ContainsKey(dummyKey));
        Assert.False(dummyCache.TryGet(dummyKey, new CachedFileSource[1] { new() }, out _));
        Assert.False(dummyCache.KeyToFile.ContainsKey(dummyKey));
    }
    
    [Fact]
    public async Task Can_CreateNewFile_WhenNotExist()
    {
        Assert.NotNull(await MergedFileCache.FromPathAsync("Super Cool Imaginary Directory"));
    }
    
    [Fact]
    public async Task Can_Write_AndRead_File()
    {
        // Arrange & Act
        var folderPath = "Test/Can_WriteToFile";
        Directory.Delete(folderPath, true);
        
        var dummyCache = new MergedFileCache(folderPath);
        var dummyFile = new CachedFile() { RelativePath = "dummy", LastAccessed = DateTime.Now };
        var dummyKey = "temp+coolFile.png";
        
        // Assert & Write Dummy File
        dummyCache.KeyToFile.Add(dummyKey, dummyFile);
        await dummyCache.ToPathAsync();
        Assert.True(File.Exists(dummyCache.GetConfigPath()));
        
        // Read
        var copy = await MergedFileCache.FromPathAsync(folderPath);
        Assert.True(copy.KeyToFile.TryGetValue(dummyKey, out var copyFile));
        Assert.Equal(dummyFile.RelativePath, copyFile.RelativePath);
        Assert.Equal(dummyFile.LastAccessed, copyFile.LastAccessed);
    }

    private static byte[] GetRandomBytes(int count)
    {
        var result = new byte[count];
        Random.Shared.NextBytes(result);
        return result;
    }

    private static void DeleteDirectory(string path)
    {
        try { Directory.Delete(path, true); }
        catch (Exception) { /* ignored */ }
    }
}