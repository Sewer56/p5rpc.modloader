namespace Persona.Merger.Patching.Tbl;

/// <summary>
/// A class that contains an individual patch to be applied to a tbl.
/// </summary>
public struct TblPatch
{
    /// <summary>
    /// List of patches, with each entry representing a segment.
    /// </summary>
    public List<SegmentDiff> SegmentDiffs = new();

    public TblPatch() { }

    public struct SegmentDiff
    {
        public Memory<byte> Data;
        public int LengthAfterPatch;
    }
}