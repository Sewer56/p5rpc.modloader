using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using Reloaded.Memory.Streams;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;

/// <summary>
/// Represents an individual segment with names in this name table.
/// </summary>
public struct NameTableSegment
{
    // Note: This is a bit inefficient but it's only used for merging one time, so I'll allow it
    //       to keep code simple. I might optimise this another day.

    /// <summary>
    /// List of names stored in this segment.
    /// </summary>
    public List<byte[]> Names { get; set; } = new();

    public NameTableSegment() { }

    /// <summary>
    /// Calculates total length of segment once serialized back to binary file.
    /// </summary>
    public int CalculateTotalLength()
    {
        const int segmentSizeLength = 4;
        int offsetSegmentLength = Mathematics.RoundUp(Names.Count * 2 + segmentSizeLength, P5RTblSegmentFinder.TblSegmentAlignment);

        int stringSegmentLength = segmentSizeLength;
        foreach (var name in CollectionsMarshal.AsSpan(Names))
            stringSegmentLength += name.Length;

        return offsetSegmentLength + Mathematics.RoundUp(stringSegmentLength, P5RTblSegmentFinder.TblSegmentAlignment);
    }

    /// <summary>
    /// Serializes segments into new name table.
    /// </summary>
    public void Serialize(ExtendedMemoryStream stream)
    {
        // Write offset section
        int offsetDataLength = Names.Count * 2;
        stream.WriteBigEndianPrimitive(offsetDataLength);

        ushort currentOffset = 0;
        foreach (var name in CollectionsMarshal.AsSpan(Names))
        {
            stream.WriteBigEndianPrimitive(currentOffset);
            currentOffset += (ushort)name.Length;
        }

        stream.AddPadding(P5RTblSegmentFinder.TblSegmentAlignment);

        // Write string section.
        stream.WriteBigEndianPrimitive<uint>(currentOffset); // size of section
        foreach (var name in CollectionsMarshal.AsSpan(Names))
            stream.Write(name);

        stream.AddPadding(P5RTblSegmentFinder.TblSegmentAlignment);
    }
}