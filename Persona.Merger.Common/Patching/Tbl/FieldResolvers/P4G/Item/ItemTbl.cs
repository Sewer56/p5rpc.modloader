using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Utilities;
using System.Buffers.Binary;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Item;

public unsafe class ItemTbl
{
    /// <summary/>
    /// <param name="tblPointer">Pointer to start of itemtbl.bin</param>
    /// <param name="segments">The span of address and length tuples to fill.</param>
    private static void Populate(byte* tblPointer, ref Span<PointerLengthTuple> segments)
    {
        fixed (PointerLengthTuple* currentSegment = &segments[0])
            Populate(tblPointer, segments.Length, currentSegment);
    }

    /// <summary/>
    /// <param name="tblPointer">Pointer to start of itemtbl.bin</param>
    /// <param name="numSegments">Number of segments for this TBL section.</param>
    /// <param name="segments">The span of address and length tuples to fill.</param>
    public static void Populate(byte* tblPointer, int numSegments, PointerLengthTuple* segments)
    {
        var currentPtr = tblPointer;

        // Item info segment
        ref var currentSegment = ref segments[0];
        currentSegment.Pointer = (currentPtr + 4);
        int numEntries = (*(int*)currentPtr);
        currentSegment.Length = numEntries * 68; // itemtbl.bin stores number of entries, not total length
        currentPtr += currentSegment.Length + 4;

        // Segment 1 (unknown data)
        currentSegment = ref segments[1];
        currentSegment.Pointer = currentPtr;
        currentSegment.Length = 28; // that's just how long it is
        currentPtr += 28;

        // Item names
        currentSegment = ref segments[2];
        currentSegment.Pointer = currentPtr;
        currentSegment.Length = numEntries * 24; // Names are all 24 characters
    }

}
