using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Unit;

public struct EnemyUnitStatsResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 68 * 68;

        if (offsetInStruct is >= 2 and < 6 or >= 16 and < 22 or >= 46 and < 64)
        {
            // u8s
            moveBy = 0;
            length = 1;
            return false;
        }

        if (offsetInStruct is >= 8 and < 16)
        {
            // u8s
            moveBy = (int)(offsetInStruct - offsetInStruct / 4 * 4);
            length = 1;
            return false;
        }

        // Remainder are aligned u16s
        moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
        length = 2; // mutually exclusive bit flags.
        return true;
    }
}