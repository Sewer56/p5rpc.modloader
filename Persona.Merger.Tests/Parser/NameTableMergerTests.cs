using Persona.Merger.Patching.Tbl.Name;

namespace Persona.Merger.Tests.Parser;

public class NameTableMergerTests
{
    [Fact]
    public void CreateDiffs_Baseline()
    {
        var name  = File.ReadAllBytes(Assets.NameBefore);
        var table = ParsedNameTable.ParseTable(name);
        
        var otherName  = File.ReadAllBytes(Assets.NameAfter);
        var otherTable = ParsedNameTable.ParseTable(otherName);

        var diff = NameTableMerger.CreateDiffs(table, new[] { otherTable });
        
        Assert.True(diff[0].SegmentDiffs.Count == 4);
        Assert.True(diff[0].SegmentDiffs[1].Strings.Count == 18);
        Assert.True(diff[0].SegmentDiffs[2].Strings.Count == 1);
    }
    
    [Fact]
    public void MergeTables_Baseline()
    {
        MergeTables_Common(Assets.NameAfter);
    }

    [Fact]
    public void MergeTables_DCBreaksStuffTooMuch()
    {
        MergeTables_Common(Assets.NameAfter2);
    }
    
    private static void MergeTables_Common(string afterFilePath)
    {
        var name = File.ReadAllBytes(Assets.NameBefore);
        var table = ParsedNameTable.ParseTable(name);

        var otherName = File.ReadAllBytes(afterFilePath);
        var otherTable = ParsedNameTable.ParseTable(otherName);

        var diff = NameTableMerger.CreateDiffs(table, new[] { otherTable });
        var newTable = NameTableMerger.Merge(table, diff);
        var newFile = newTable.ToArray();

        Assert.Equal(otherName, newFile);
    }
}