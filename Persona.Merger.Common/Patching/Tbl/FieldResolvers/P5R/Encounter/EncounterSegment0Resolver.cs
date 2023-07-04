using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Encounter;

public struct EncounterSegment0Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var structOffset = offset / 44 * 44;

        // u16s in this struct
        if (structOffset is >= 2 and < 28 or >= 30 and < 32 or >= 36 and < 38 or >= 42 and < 44)
        {
            moveBy = (int)(structOffset - structOffset / 2 * 2);
            length = 2;
            return true;
        }

        moveBy = 0;
        length = 1;
        return false;
    }
}