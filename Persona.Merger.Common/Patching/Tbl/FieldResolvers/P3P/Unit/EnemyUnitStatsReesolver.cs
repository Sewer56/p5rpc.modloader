using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Unit;

public struct EnemyUnitStatsResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 62 * 62;

        if (offsetInStruct is >= 2 and < 4 or >= 8 and < 13)
        {
            // u8s
            moveBy = 0;
            length = 1;
            return false;
        }

        // Remainder are aligned u16s
        moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
        length = 2; 
        return true;
    }
}
