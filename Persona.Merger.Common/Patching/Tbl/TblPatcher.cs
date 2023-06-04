using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Patching.Tbl.FieldResolvers.AICalc;
using Persona.Merger.Patching.Tbl.FieldResolvers.Elsai;
using Persona.Merger.Patching.Tbl.FieldResolvers.Encounter;
using Persona.Merger.Patching.Tbl.FieldResolvers.Exist;
using Persona.Merger.Patching.Tbl.FieldResolvers.Item;
using Persona.Merger.Patching.Tbl.FieldResolvers.Persona;
using Persona.Merger.Patching.Tbl.FieldResolvers.Player;
using Persona.Merger.Patching.Tbl.FieldResolvers.Skill;
using Persona.Merger.Patching.Tbl.FieldResolvers.Unit;
using Persona.Merger.Patching.Tbl.FieldResolvers.Visual;
using Persona.Merger.Utilities;
using Reloaded.Memory.Streams;
using Sewer56.StructuredDiff;
using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl;

/// <summary>
/// Utility class for patching P5R TBL files.
/// </summary>
public struct TblPatcher
{
    /// <summary>
    /// The table to patch.
    /// </summary>
    public byte[] TblData { get; set; }
    
    /// <summary>
    /// Type of table contained.
    /// </summary>
    public TblType TableType { get; set; }
    
    public TblPatcher(byte[] tblData, TblType tableType)
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
            var patch        = new TblPatch();
            var segmentCount = TblSegmentFinder.GetSegmentCount(TableType);
            var originalSegments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below
            var newSegments      = stackalloc PointerLengthTuple[segmentCount];
            TblSegmentFinder.Populate(tblData, segmentCount, originalSegments);
            TblSegmentFinder.Populate(otherTblData, segmentCount, newSegments);
            
            // Skill specific code
            switch (TableType)
            {
                case TblType.Persona:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new PersonaStatsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new SkillsAndStatGrowthResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PartyLevelUpThresholdsResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new PartyPersonasResolver());
                    break;
                case TblType.Player:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new LevelUpThresholdsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new PointsPerLevelResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PlayerSegment2Resolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new PlayerSegment3Resolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new PlayerSegment4Resolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new PlayerSegment5Resolver());
                    break;
                case TblType.Exist:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ExistSegment0Resolver());
                    break;
                case TblType.Elsai:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ElsaiSegment0Resolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new ElsaiSegment1Resolver());
                    break;
                case TblType.AiCalc:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new AICalcSegment0Resolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new AICalcSegment1Resolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new AICalcSegment2Resolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new AICalcSegment3Resolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new AICalcSegment4Resolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new AICalcSegment5Resolver());
                    DiffSegment(patch, newSegments[6], originalSegments[6], new AICalcSegment6Resolver());
                    break;
                case TblType.Encount:                    
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EncounterSegment0Resolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new EncounterSegment1Resolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new EncounterSegment2Resolver());
                    break;
                case TblType.Skill:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new ElementsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new ActiveSkillDataResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new SkillSegment2Resolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new TraitsResolver());
                    break;
                case TblType.Item:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new AccessoriesResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new ArmorResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new ConsumablesResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new KeyItemsResolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new MaterialsResolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new MeleeWeaponsResolver());
                    DiffSegment(patch, newSegments[6], originalSegments[6], new OutfitsResolver());
                    DiffSegment(patch, newSegments[7], originalSegments[7], new SkillCardsResolver());
                    DiffSegment(patch, newSegments[8], originalSegments[8], new RangedWeaponsResolver());
                    DiffSegment(patch, newSegments[9], originalSegments[9], new FooterResolver());
                    break;
                case TblType.Unit:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EnemyUnitStatsResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new EnemyElementalAffinitiesResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PersonaElementalAffinitiesResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new UnitSegment3Resolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new VisualIndexResolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new UnitSegment5Resolver());
                    break;
                case TblType.Visual:
                    DiffSegment(patch, newSegments[0], originalSegments[0], new EnemyVisualVariablesAResolver());
                    DiffSegment(patch, newSegments[1], originalSegments[1], new PlayerVisualVariablesAResolver());
                    DiffSegment(patch, newSegments[2], originalSegments[2], new PersonaVisualVariablesAResolver());
                    DiffSegment(patch, newSegments[3], originalSegments[3], new PlayerVisualVariablesBResolver());
                    DiffSegment(patch, newSegments[4], originalSegments[4], new EnemyVisualVariablesBResolver());
                    DiffSegment(patch, newSegments[5], originalSegments[5], new PersonaVisualVariablesBResolver());
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
    public unsafe byte[] Apply(List<TblPatch> patches)
    {
        fixed (byte* tblData = &TblData[0])
        {
            // Get original segments.
            var segmentCount = TblSegmentFinder.GetSegmentCount(TableType);
            var originalSegments = stackalloc PointerLengthTuple[segmentCount]; // using pointer to elide bounds checks below
            TblSegmentFinder.Populate(tblData, segmentCount, originalSegments);

            // Convert original segments into Memory<T>.
            var segments = new Memory<byte>[segmentCount];
            for (int x = 0; x < segmentCount; x++)
            {
                ref var originalSegment = ref originalSegments[x];
                var offset = originalSegment.Pointer - tblData;
                segments[x] = new Memory<byte>(TblData, (int)offset, originalSegment.Length);
            }

            // Apply Patch(es).
            for (int x = 0; x < segmentCount; x++) 
            foreach (var patch in CollectionsMarshal.AsSpan(patches))
            {
                var destination = GC.AllocateUninitializedArray<byte>(Math.Max(patch.SegmentDiffs[x].LengthAfterPatch, segments[x].Length));
                var patchDiff = patch.SegmentDiffs[x].Data;
                
                fixed (byte* destinationPtr = &destination[0])
                fixed (byte* currentSegmentPtr = segments[x].Span)
                fixed (byte* patchPtr = patchDiff.Span)
                {
                    S56DiffDecoder.Decode(currentSegmentPtr, patchPtr, destinationPtr, (nuint)patch.SegmentDiffs[x].Data.Length, out var numWritten);
                    segments[x] = new Memory<byte>(destination, 0, (int)numWritten);
                }
            }
            
            // Produce new file.
            var fileSize = 0;
            foreach (var segment in segments)
                fileSize += Mathematics.RoundUp((4 + segment.Length), TblSegmentFinder.TblSegmentAlignment);
            
            var result = GC.AllocateUninitializedArray<byte>(fileSize);
            using var memoryStream = new ExtendedMemoryStream(result, true);
            foreach (var segment in segments)
            {
                memoryStream.WriteBigEndianPrimitive(segment.Length);
                memoryStream.Write(segment.Span);
                memoryStream.AddPadding(TblSegmentFinder.TblSegmentAlignment);
            }

            return result;
        }
    }
    
    /// <summary>
    /// Creates a diff for an individual segment and adds it to the patch.
    /// </summary>
    private unsafe void DiffSegment<T>(TblPatch patch, PointerLengthTuple newSegment, PointerLengthTuple originalSegment, T resolver) where T : IEncoderFieldResolver
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

/// <summary>
/// Supported table types.
/// </summary>
public enum TblType
{
    Persona,
    Player,
    Exist,
    Elsai,
    AiCalc,
    Encount,
    Skill,
    Item,
    Unit,
    Visual,
    Name
}