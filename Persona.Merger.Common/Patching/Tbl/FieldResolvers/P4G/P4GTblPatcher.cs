using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Utilities;
using Reloaded.Memory.Streams;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Persona;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Skill;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Unit;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Model;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Encounter;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Effect;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.AICalc;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Item;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Message;
using static Persona.Merger.Patching.Tbl.FieldResolvers.TblPatcherCommon;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G;

/// <summary>
/// Utility class for patching P5R TBL files.
/// </summary>
public struct P4GTblPatcher
{
    /// <summary>
    /// The table to patch.
    /// </summary>
    public byte[] TblData { get; set; }

    /// <summary>
    /// Type of table contained.
    /// </summary>
    public TblType TableType { get; set; }

    public P4GTblPatcher(byte[] tblData, TblType tableType)
    {
        TblData = tblData;
        TableType = tableType;
    }

    /// <summary>
    /// Generates a table patch.
    /// </summary>
    /// <param name="otherTbl">Data of the new table.</param>
    public unsafe TblPatch GeneratePatch(byte[] otherTbl)
    {
        fixed (byte* otherTblData = &otherTbl[0])
        fixed (byte* tblData = &TblData[0])
        {
            var patch = new TblPatch();
            var segmentCount = P4GTblSegmentFinder.GetSegmentCount(TableType);
            var originalSegments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below
            var newSegments = stackalloc PointerLengthTuple[segmentCount];

            if (TableType is TblType.Item)
            {
                // itemtbl.bin is special
                ItemTbl.Populate(tblData, segmentCount, originalSegments);
                ItemTbl.Populate(otherTblData, segmentCount, newSegments);
            }
            else
            {
                P4GTblSegmentFinder.Populate(tblData, segmentCount, originalSegments);
                P4GTblSegmentFinder.Populate(otherTblData, segmentCount, newSegments);
            }

            // Skill specific code
            switch (TableType)
            {
                case TblType.Persona:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new PersonaStatsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new SkillsAndStatGrowthResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PartyPersonasResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new PartyLevelUpThresholdsResolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new PersonaExistResolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new PersonaFusionResolver());
                    break;
                case TblType.AiCalc:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new AICalcSegment0Resolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new AICalcSegment1Resolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new AICalcSegment2Resolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new AICalcSegment3Resolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new AICalcSegment4Resolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new AICalcSegment5Resolver());
                    DiffSegment(patch, newSegments[6], originalSegments[6], new AICalcSegment6Resolver());
                    DiffSegment(patch, newSegments[7], originalSegments[7], new AICalcSegment7Resolver());
                    DiffSegment(patch, newSegments[8], originalSegments[8], new AICalcSegment8Resolver());
                    break;
                case TblType.Encount:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EnemyEncounterResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new EncountSegment1Resolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new EncountSegment2Resolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new EncountSegment3Resolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new EncountSegment4Resolver());
                    break;
                case TblType.Effect:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EffectResolver());
                    break;
                case TblType.Skill:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ElementsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new ActiveSkillDataResolver());
                    break;
                case TblType.Model:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new PlayerVisualVariablesResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new EnemyVisualVariablesResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PersonaVisualVariablesResolver());
                    break;
                case TblType.Item:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ItemInfoResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new ItemSegment1Resolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new ItemNameResolver());
                    break;
                case TblType.Unit:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EnemyUnitStatsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new EnemyElementalAffinitiesResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PersonaElementalAffinitiesResolver());
                    break;
                case TblType.Message:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ArcanaNameResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new SkillNameResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new EnemyNameResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new PersonaNameResolver());
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported Table Type {TableType}");
            }

            return patch;
        }
    }

    /// <summary>
    /// Applies a list of table patches.
    /// </summary>
    /// <param name="patches">List of patches to apply.</param>
    /// <param name="type">Type of the TBL file.</param>
    /// <param name="overrides">Overrides for entire sections within the file.</param>
    public unsafe byte[] Apply(List<TblPatch> patches, TblType type, byte[]?[]? overrides = null)
    {
        fixed (byte* tblData = &TblData[0])
        {
            // Get original segments.
            var segmentCount = P4GTblSegmentFinder.GetSegmentCount(TableType);
            var originalSegments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below
            if (type == TblType.Item)
                ItemTbl.Populate(tblData, segmentCount, originalSegments);
            else
                P4GTblSegmentFinder.Populate(tblData, segmentCount, originalSegments);

            // Convert original segments into Memory<T>.
            var segments = ConvertSegmentsToMemory(segmentCount, originalSegments, tblData, TblData);

            // Apply Patch(es).
            for (int x = 0; x < segmentCount; x++)
            {
                if (overrides != null && overrides.Length > x && overrides[x] != null)
                {
                    segments[x] = new Memory<byte>(overrides[x]);
                    continue;
                }
                
                foreach (var patch in CollectionsMarshal.AsSpan(patches))
                {
                    if (patch.SegmentDiffs.Count <= x) 
                        continue;
                    
                    ApplyPatch(patches, x, segments);
                }
            }

            // Produce new file.
            var fileSize = 0;
            if (type == TblType.Item)
            {
                // itemtbl doesn't store segments nicely like "real" tables do
                fileSize = 4;
                foreach (var segment in segments)
                    fileSize += segment.Length;
            }
            else
            {
                foreach (var segment in segments)
                    fileSize += Mathematics.RoundUp(4 + segment.Length, P4GTblSegmentFinder.TblSegmentAlignment);
            }

            var result = GC.AllocateUninitializedArray<byte>(fileSize);
            using var memoryStream = new ExtendedMemoryStream(result, true);

            if (type == TblType.Item)
            {
                // itemtbl stores number of entries, not length
                memoryStream.Write(segments[0].Length / 68);
                foreach (var segment in segments)
                    memoryStream.Write(segment.Span);
                return result;
            }

            foreach (var segment in segments)
                WriteSegment(memoryStream, segment, P4GTblSegmentFinder.TblSegmentAlignment);

            return result;
        }
    }

    public static unsafe Memory<byte>? GetSegment(byte[] data, TblType type, int segment)
    {
        var segmentCount = P4GTblSegmentFinder.GetSegmentCount(type);
        if (segment >= segmentCount)
            return null;

        fixed (byte* tblData = &data[0])
        {
            var segments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below

            if (type is TblType.Item)
            {
                // itemtbl.bin is special
                ItemTbl.Populate(tblData, segmentCount, segments);
            }
            else
            {
                P4GTblSegmentFinder.Populate(tblData, segmentCount, segments);
            }

            var offset = segments[segment].Pointer - tblData;
            return new Memory<byte>(data, (int)offset, segments[segment].Length);
        }
    }
}

/// <summary>
/// Supported table types.
/// </summary>
public enum TblType
{
    Persona,
    AiCalc,
    Encount,
    Skill,
    Item,
    Unit,
    Model,
    Effect,
    Message
}