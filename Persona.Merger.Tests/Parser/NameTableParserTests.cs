using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;

namespace Persona.Merger.Tests.Parser;

public class NameTableParserTests
{
    [Fact]
    public void ParseNameTable_ParsesAllSections()
    {
        var name  = File.ReadAllBytes(P5RAssets.NameBefore);
        var table = ParsedNameTable.ParseTable(name);
        var sections = P5RTblSegmentFinder.GetSegmentCount(TblType.Name) / 2;
        
        Assert.True(table.Segments.Count > 0);
        Assert.True(table.Segments[0].Names.Count > 0);
        Assert.Equal(sections, table.Segments.Count);
    }
}