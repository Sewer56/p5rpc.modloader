namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;

/// <summary>
/// Represents a diff for a name table.
/// </summary>
public struct NameTableDiff
{
    /// <summary>
    /// Maps segments which differ from original to their corresponding diffs.
    /// </summary>
    public Dictionary<int, SegmentDiff> SegmentDiffs = new();

    public NameTableDiff() { }
}

/// <summary>
/// Represents a diff for an individual segment.
/// </summary>
public struct SegmentDiff
{
    /// <summary>
    /// Maps string index to byte array (string) for that index.
    /// </summary>
    public Dictionary<int, byte[]> Strings = new();

    public SegmentDiff() { }
}