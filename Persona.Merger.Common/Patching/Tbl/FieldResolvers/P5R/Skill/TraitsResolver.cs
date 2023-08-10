using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Skill;

public struct TraitsResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 60 * 60;
        if (offsetInStruct < 4)
        {
            // First 2 elements are u16s
            moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
            length = 2;
            return true;
        }

        // Otherwise it's a u32
        moveBy = (int)(offsetInStruct - offsetInStruct / 4 * 4);
        length = 4;
        return true;
    }
}