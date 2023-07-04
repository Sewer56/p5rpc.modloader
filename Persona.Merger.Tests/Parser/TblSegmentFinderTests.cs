using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;
using Persona.Merger.Utilities;

namespace Persona.Merger.Tests.Parser;

public unsafe class TblSegmentFinderTests
{
    [Fact]
    public void FindsAllSegments_InSkill()
    {
        var segmentCount = P5RTblSegmentFinder.GetSegmentCount(TblType.Skill);
        var tableData = File.ReadAllBytes(P5RAssets.SkillBefore);
        fixed (byte* tableDataPtr = &tableData[0])
        {
            Span<PointerLengthTuple> segments = stackalloc PointerLengthTuple[segmentCount];
            P5RTblSegmentFinder.Populate(tableDataPtr, ref segments);
            
            // Assert
            Assert.Equal(8448, segments[0].Length);
            Assert.Equal(38400, segments[1].Length);
            Assert.Equal(680, segments[2].Length);
            Assert.Equal(17940, segments[3].Length);
        }
    }
}