using Persona.Merger.Utilities;
using Reloaded.Memory.Streams;
using static Persona.Merger.Patching.Tbl.FieldResolvers.TblPatcherCommon;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.Generic;

/// <summary>
/// Utility class for patching P5R TBL files.
/// </summary>
public struct GenericPatcher
{
    /// <summary>
    /// The table to patch.
    /// </summary>
    public byte[] TblData { get; set; }

    public GenericPatcher(byte[] tblData)
    {
        TblData = tblData;
    }

    /// <summary>
    /// Generates a table patch.
    /// </summary>
    /// <param name="otherTbl">Data of the new table.</param>
    public unsafe TblPatch GeneratePatchGeneric(byte[] otherTbl, int ResolverSize)
    {
        fixed (byte* otherTblData = &otherTbl[0])
        fixed (byte* tblData = &TblData[0])
        {
            var patch = new TblPatch();

            var originalSegments = stackalloc PointerLengthTuple[1]; // using pointer to elide bounds checks below
            var newSegments = stackalloc PointerLengthTuple[1];
            PopulateGeneric(tblData, TblData.Length, originalSegments);
            PopulateGeneric(otherTblData, otherTbl.Length, newSegments);

            if (ResolverSize == 2) DiffSegment(patch, newSegments[0], originalSegments[0], new ShortResolver());
            else if (ResolverSize == 4) DiffSegment(patch, newSegments[0], originalSegments[0], new IntResolver());
            else DiffSegment(patch, newSegments[0], originalSegments[0], new ByteResolver());

            return patch;
        }
    }

    /// <summary>
    /// Applies a list of table patches.
    /// </summary>
    /// <param name="patches">List of patches to apply.</param>
    public unsafe byte[] ApplyGeneric(List<TblPatch> patches)
    {
        fixed (byte* tblData = &TblData[0])
        {
            // Get original segments.
            var segmentCount = 1;
            var originalSegments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below
            PopulateGeneric(tblData, segmentCount, originalSegments);

            // Convert original segments into Memory<T>.
            var segments = ConvertSegmentsToMemoryGeneric(segmentCount, originalSegments, tblData, TblData);

            // Apply Patch(es).
            for (int x = 0; x < segmentCount; x++)
                ApplyPatch(patches, x, segments);

            // Produce new file.
            var fileSize = 0;
            foreach (var segment in segments)
                fileSize += segment.Length;

            var result = GC.AllocateUninitializedArray<byte>(fileSize);
            using var memoryStream = new ExtendedMemoryStream(result, true);
            foreach (var segment in segments)
            {
                // memoryStream.WriteBigEndianPrimitive(segment.Length);
                memoryStream.Write(segment.Span);
                // memoryStream.AddPadding(P5RTblSegmentFinder.TblSegmentAlignment);
            }

            return result;
        }
    }

    public unsafe static void PopulateGeneric(byte* tblPointer, int length, PointerLengthTuple* segments)
    {
        ref var currentSegment = ref segments[0];
        currentSegment.Pointer = tblPointer;
        currentSegment.Length = length;
    }
}