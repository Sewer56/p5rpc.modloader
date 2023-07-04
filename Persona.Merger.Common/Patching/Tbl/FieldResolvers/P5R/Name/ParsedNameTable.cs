using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;
using Persona.Merger.Utilities;
using Reloaded.Memory.Streams;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;

/// <summary>
/// Abstracts a name table parsed from a binary file.
/// </summary>
public unsafe struct ParsedNameTable
{
    /// <summary>
    /// Segments of the parsed table.
    /// </summary>
    public List<NameTableSegment> Segments = new(19);

    public ParsedNameTable() { }

    /// <summary>
    /// Converts the parsed table back to full name.tbl.
    /// </summary>
    public byte[] ToArray()
    {
        // Get total length of serialized data.
        int totalLength = 0;
        foreach (var segment in CollectionsMarshal.AsSpan(Segments))
            totalLength += segment.CalculateTotalLength();

        // Serialize the segments into new name table.
        var arr = GC.AllocateUninitializedArray<byte>(totalLength);
        using var stream = new ExtendedMemoryStream(arr);
        foreach (var segment in CollectionsMarshal.AsSpan(Segments))
            segment.Serialize(stream);

        return arr;
    }

    /// <summary>
    /// Parses the name table.
    /// </summary>
    /// <param name="data">Array of bytes in memory.</param>
    public static ParsedNameTable ParseTable(byte[] data)
    {
        fixed (byte* dataPtr = &data[0])
            return ParseTable(dataPtr);
    }

    /// <summary>
    /// Parses the name table.
    /// </summary>
    /// <param name="data">Pointer to the TBL data.</param>
    /// <returns>The name table, after parsing.</returns>
    public static ParsedNameTable ParseTable(byte* data)
    {
        var table = new ParsedNameTable();
        var numSegments = P5RTblSegmentFinder.GetSegmentCount(TblType.Name);
        var segments = stackalloc PointerLengthTuple[numSegments];
        P5RTblSegmentFinder.Populate(data, numSegments, segments);

        // Name TBL consists of tuples of segments of pointers and segments of strings.
        for (int x = 0; x < numSegments - 1; x += 2)
        {
            var tableSegment = new NameTableSegment();
            var pointerSegment = *(segments + x);

            var pointerSegmentPtr = pointerSegment.Pointer;
            var numStrings = pointerSegment.Length / 2; // each ptr is a big endian u16
            var nameSegmentPtr = (segments + x + 1)->Pointer;

            // Parse all the strings.
            for (int y = 0; y < numStrings; y++)
            {
                var offset = BinaryPrimitives.ReverseEndianness(*(ushort*)pointerSegmentPtr);
                var stringAddr = nameSegmentPtr + offset;
                var strLen = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(stringAddr).Length + 1;

                var resultArray = GC.AllocateUninitializedArray<byte>(strLen);
                new Span<byte>(stringAddr, strLen).CopyTo(resultArray);
                tableSegment.Names.Add(resultArray);
                pointerSegmentPtr += sizeof(ushort);
            }

            // Add the segment.
            table.Segments.Add(tableSegment);
        }

        return table;
    }
}