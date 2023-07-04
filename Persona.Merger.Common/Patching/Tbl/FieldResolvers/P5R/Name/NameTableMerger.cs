namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;

/// <summary>
/// Class for merging name.tbl. Could be optimised.
/// </summary>
public class NameTableMerger
{
    /// <summary>
    /// Merges the contents of name.tbl and outputs a new one.
    /// </summary>
    /// <param name="baseTable">The table to merge other tables into.</param>
    /// <param name="tables">The other tables to create diffs for.</param>
    /// <returns>The diffs from the base table.</returns>
    public static NameTableDiff[] CreateDiffs(ParsedNameTable baseTable, Span<ParsedNameTable> tables)
    {
        var diffs = new NameTableDiff[tables.Length];
        for (int x = 0; x < tables.Length; x++)
        {
            var diff = new NameTableDiff();
            var table = tables[x];

            for (int y = 0; y < baseTable.Segments.Count; y++)
            {
                var segmentDiff = new SegmentDiff();
                var baseSegment = baseTable.Segments[y];
                var otherSegment = table.Segments[y];
                var minMaxString = Math.Min(baseSegment.Names.Count, otherSegment.Names.Count);

                // Diff the individual strings.
                for (int z = 0; z < minMaxString; z++)
                {
                    var baseName = baseSegment.Names[z];
                    var otherName = otherSegment.Names[z];
                    if (!baseName.AsSpan().SequenceEqual(otherName))
                        segmentDiff.Strings[z] = otherName;
                }

                // Add all extra names varbatim
                if (otherSegment.Names.Count > baseSegment.Names.Count)
                {
                    for (int z = baseSegment.Names.Count; z < otherSegment.Names.Count; z++)
                        segmentDiff.Strings[z] = otherSegment.Names[z];
                }

                // Add the diff for the segment.
                if (segmentDiff.Strings.Count > 0)
                    diff.SegmentDiffs[y] = segmentDiff;
            }

            diffs[x] = diff;
        }

        return diffs;
    }

    /// <summary>
    /// Merges the contents of name.tbl and outputs a new one.
    /// </summary>
    /// <param name="baseTable">The table to merge other tables into. This value will be modified.</param>
    /// <param name="diffs">The table diffs to apply.</param>
    /// <returns>The merged table.</returns>
    public static ParsedNameTable Merge(ParsedNameTable baseTable, NameTableDiff[] diffs)
    {
        foreach (var diff in diffs)
            foreach (var segmentDiff in diff.SegmentDiffs)
            {
                var segment = baseTable.Segments[segmentDiff.Key];
                foreach (var segmentStrings in segmentDiff.Value.Strings)
                {
                    // Make extra room in list if needed.
                    var neededExtraSlots = segmentStrings.Key + 1 - segment.Names.Count;
                    if (neededExtraSlots > 0)
                        segment.Names.Add(Array.Empty<byte>());

                    // Assign to list directly.
                    segment.Names[segmentStrings.Key] = segmentStrings.Value;
                }
            }

        return baseTable;
    }
}