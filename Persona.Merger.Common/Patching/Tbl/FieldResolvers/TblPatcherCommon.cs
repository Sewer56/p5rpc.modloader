using System.Runtime.InteropServices;
using Persona.Merger.Utilities;
using Reloaded.Memory.Streams;
using Sewer56.StructuredDiff;
using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers;

/// <summary>
/// Common functionality used in multiple TBL patchers.
/// </summary>
internal class TblPatcherCommon
{
    /// <summary>
    /// Converts a list of segments and their respective slices to Memory{byte} objects.
    /// </summary>
    internal static unsafe Memory<byte>[] ConvertSegmentsToMemory(int segmentCount, PointerLengthTuple* originalSegments, byte* tblData, byte[] tblDataArr)
    {
        var segments = new Memory<byte>[segmentCount];
        for (int x = 0; x < segmentCount; x++)
        {
            ref var originalSegment = ref originalSegments[x];
            var offset = originalSegment.Pointer - tblData;
            segments[x] = new Memory<byte>(tblDataArr, (int)offset, originalSegment.Length);
        }

        return segments;
    }
    internal static unsafe Memory<byte>[] ConvertSegmentsToMemoryGeneric(int segmentCount, PointerLengthTuple* originalSegments, byte* tblData, byte[] tblDataArr)
    {
        var segments = new Memory<byte>[segmentCount];
        for (int x = 0; x < segmentCount; x++)
        {
            ref var originalSegment = ref originalSegments[x];
            segments[x] = new Memory<byte>(tblDataArr, 0, tblDataArr.Length);
        }

        return segments;
    }
    
    /// <summary>
    /// Applies a given list of table patches to the current file's TBL segments.
    /// </summary>
    internal static unsafe void ApplyPatch(List<TblPatch> patches, int x, Memory<byte>[] segments)
    {
        patches.Sort((a, b) => a.SegmentDiffs[x].LengthAfterPatch.CompareTo(b.SegmentDiffs[x].LengthAfterPatch));

        int newLength = 0;

        foreach (var patch in CollectionsMarshal.AsSpan(patches))
        {
            if (patch.SegmentDiffs[x].LengthAfterPatch > newLength)
            {
                newLength = patch.SegmentDiffs[x].LengthAfterPatch;
            }
        }

        foreach (var patch in CollectionsMarshal.AsSpan(patches))
        {
            var destination = GC.AllocateUninitializedArray<byte>(Math.Max(newLength, segments[x].Length));
            var patchDiff = patch.SegmentDiffs[x].Data;

            fixed (byte* destinationPtr = &destination[0])
            fixed (byte* currentSegmentPtr = segments[x].Span)
            fixed (byte* patchPtr = patchDiff.Span)
            {
                S56DiffDecoder.Decode(currentSegmentPtr, patchPtr, destinationPtr, (nuint)patch.SegmentDiffs[x].Data.Length,
                    out var numWritten);
                segments[x] = new Memory<byte>(destination, 0, (int)numWritten);
            }
        }
    }
    
    /// <summary>
    /// Writes the segment to the stream.
    /// </summary>
    internal static void WriteSegment(ExtendedMemoryStream memoryStream, Memory<byte> segment, int alignment)
    {
        memoryStream.Write(segment.Length);
        memoryStream.Write(segment.Span);
        memoryStream.AddPadding(alignment);
    }
    
    /// <summary>
    /// Creates a diff for an individual segment and adds it to the patch.
    /// </summary>
    internal static unsafe void DiffSegment<T>(TblPatch patch, PointerLengthTuple newSegment, PointerLengthTuple originalSegment, T resolver) where T : IEncoderFieldResolver
    {
        var destination = GC.AllocateUninitializedArray<byte>((int)S56DiffEncoder.CalculateMaxDestinationLength(newSegment.Length));
        fixed (byte* destinationPtr = destination)
        {
            var numEncoded = S56DiffEncoder.Encode(originalSegment.Pointer, newSegment.Pointer,
                destinationPtr, (nuint)originalSegment.Length, (nuint)newSegment.Length, resolver);
            patch.SegmentDiffs.Add(new TblPatch.SegmentDiff()
            {
                Data = destination.AsMemory(0, (int)numEncoded),
                LengthAfterPatch = newSegment.Length
            });
        }
    }
}