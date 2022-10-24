using Persona.BindBuilder.Utilities;

namespace Persona.BindBuilder.Tests;

public class BindDirectoryGeneratorTests
{
    public const int ProcessId = 69;
    
    [Fact]
    public void GenerateDirectory_Baseline()
    {
        // Arrange
        using var baseDirectory = new TemporaryFolderAllocation();
        var generator = new BindingOutputDirectoryGenerator(baseDirectory.FolderPath);
        var procIdProvider = Utilities.GetCurrentProcessProvider(ProcessId);

        // Act
        var path = generator.Generate(procIdProvider);

        // Assert
        Assert.True(Directory.Exists(path));
        Assert.True(path.EndsWith(ProcessId.ToString()));
    }
    
    [Fact]
    public void DontRemove_WhenProcessStillAlive()
    {
        // Arrange
        using var baseDirectory = new TemporaryFolderAllocation();
        var generator = new BindingOutputDirectoryGenerator(baseDirectory.FolderPath);
        var procIdProvider = Utilities.GetCurrentProcessProvider(ProcessId);
        var procListProvider = Utilities.GetProcessListProvider(new[] { ProcessId });
        
        // Act
        var path = generator.Generate(procIdProvider);
        generator.Cleanup(procListProvider);

        // Assert
        Assert.True(Directory.Exists(path));
    }
    
    [Fact]
    public void Remove_WhenProcessDead()
    {
        // Arrange
        using var baseDirectory = new TemporaryFolderAllocation();
        var generator = new BindingOutputDirectoryGenerator(baseDirectory.FolderPath);
        var procIdProvider = Utilities.GetCurrentProcessProvider(ProcessId);
        var procListProvider = Utilities.GetProcessListProvider(Array.Empty<int>());
        
        // Act
        var path = generator.Generate(procIdProvider);
        generator.Cleanup(procListProvider);

        // Assert
        Assert.False(Directory.Exists(path));
    }
}