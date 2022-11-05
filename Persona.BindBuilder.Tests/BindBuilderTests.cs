using Persona.BindBuilder.Tests.Utils;
using static Persona.BindBuilder.Tests.Utils.Utilities;

namespace Persona.BindBuilder.Tests;

public class BindBuilderTests
{
    [Fact]
    public void DuplicateDetector_NoDuplicates()
    {
        // Arrange
        using var tempFolder = new TemporaryFolderAllocation(Assets.TempFolder);
        var builder = new CpkBindStringBuilder(tempFolder.FolderPath);
        builder.AddItem(new BuilderItem(Assets.ButtonPromptsMod1Cpk, GetFilesInDirectory(Assets.ButtonPromptsMod1Cpk)));
        builder.AddItem(new BuilderItem(Assets.ModelModCpk, GetFilesInDirectory(Assets.ModelModCpk)));
        
        // Act
        builder.GetFiles(out var duplis);

        // Assert
        Assert.Equal(0, duplis.Count);
    }
    
    [Fact]
    public void DuplicateDetector_DetectsDuplicates()
    {
        // Arrange
        using var tempFolder = new TemporaryFolderAllocation(Assets.TempFolder);
        var builder = new CpkBindStringBuilder(tempFolder.FolderPath);
        builder.AddItem(new BuilderItem(Assets.ButtonPromptsMod1Cpk, GetFilesInDirectory(Assets.ButtonPromptsMod1Cpk)));
        builder.AddItem(new BuilderItem(Assets.ButtonPromptsMod2Cpk, GetFilesInDirectory(Assets.ButtonPromptsMod2Cpk)));

        // Act
        builder.GetFiles(out var duplis);

        // Assert
        Assert.Equal(2, duplis.Count);
    }
    
    [Fact]
    public void Build_BaseLine()
    {
        // Arrange
        using var tempFolder = new TemporaryFolderAllocation(Assets.TempFolder);
        var builder = new CpkBindStringBuilder(tempFolder.FolderPath);
        builder.AddItem(new BuilderItem(Assets.ButtonPromptsMod1Cpk, GetFilesInDirectory(Assets.ButtonPromptsMod1Cpk)));
        builder.AddItem(new BuilderItem(Assets.ButtonPromptsMod2Cpk, GetFilesInDirectory(Assets.ButtonPromptsMod2Cpk)));

        // Act
        var outputDir = builder.Build();
        
        // Assert
        Assert.Equal(2, outputDir.Split(CpkBindStringBuilder.Delimiter).Length); // include empty line
    }
}