using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Utilities;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R;

public unsafe struct P5RTblSegmentFinder
{
    /// <summary>
    /// Alignment of individual tbl segments.
    /// </summary>
    public const int TblSegmentAlignment = 16;

    /// <summary/>
    /// <param name="tblPointer">Pointer to start of tbl.</param>
    /// <param name="segments">The span of address and length tuples to fill.</param>
    public static void Populate(byte* tblPointer, ref Span<PointerLengthTuple> segments)
    {
        fixed (PointerLengthTuple* currentSegment = &segments[0])
            Populate(tblPointer, segments.Length, currentSegment);
    }

    /// <summary/>
    /// <param name="tblPointer">Pointer to start of tbl.</param>
    /// <param name="numSegments">Number of segments for this TBL section.</param>
    /// <param name="segments">The span of address and length tuples to fill.</param>
    public static void Populate(byte* tblPointer, int numSegments, PointerLengthTuple* segments)
    {
        int currentSegmentNo = 0;
        var currentPtr = tblPointer;
        while (currentSegmentNo < numSegments)
        {
            ref var currentSegment = ref segments[currentSegmentNo];
            currentSegment.Pointer = (currentPtr + 4);
            currentSegment.Length = BinaryPrimitives.ReverseEndianness(*(int*)currentPtr);
            currentPtr += Mathematics.RoundUp(currentSegment.Length + 4, TblSegmentAlignment);
            currentSegmentNo++;
        }
    }

    /// <summary>
    /// Returns the segment count for a given table type.
    /// </summary>
    public static int GetSegmentCount(TblType type)
    {
        return type switch
        {
            TblType.Persona => 4,
            TblType.Player => 6,
            TblType.Exist => 1,
            TblType.Elsai => 2,
            TblType.AiCalc => 7,
            TblType.Encount => 3,
            TblType.Skill => 4,
            TblType.Item => 10,
            TblType.Unit => 6,
            TblType.Visual => 6,
            TblType.Name => 38,
            _ => ThrowUnsupportedTblTypeException(type)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ThrowUnsupportedTblTypeException(TblType type) => throw new ArgumentOutOfRangeException(nameof(type), type, null);
}

