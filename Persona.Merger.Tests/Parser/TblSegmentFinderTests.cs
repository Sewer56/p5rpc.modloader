using Persona.Merger.Patching.Tbl;
using Persona.Merger.Utilities;

namespace Persona.Merger.Tests.Parser;

public unsafe class TblSegmentFinderTests
{
    [Fact]
    public void FindsAllSegments_InSkill()
    {
        var segmentCount = TblSegmentFinder.GetSegmentCount(TblType.Skill);
        var tableData = File.ReadAllBytes(Assets.SkillBefore);
        fixed (byte* tableDataPtr = &tableData[0])
        {
            Span<PointerLengthTuple> segments = stackalloc PointerLengthTuple[segmentCount];
            TblSegmentFinder.Populate(tableDataPtr, ref segments);
            
            // Assert
            Assert.Equal(8448, segments[0].Length);
            Assert.Equal(38400, segments[1].Length);
            Assert.Equal(680, segments[2].Length);
            Assert.Equal(17940, segments[3].Length);
        }
    }
}