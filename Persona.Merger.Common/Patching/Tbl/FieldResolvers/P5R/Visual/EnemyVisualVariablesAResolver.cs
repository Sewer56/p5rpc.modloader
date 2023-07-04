using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Visual;

public struct EnemyVisualVariablesAResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 200 * 200;

        if (offsetInStruct is >= 104 and < 112 or >= 128)
        {
            // Remainder are aligned u16s
            moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
            length = 2;
            return true;
        }

        // Remainder are aligned u16s
        moveBy = (int)(offsetInStruct - offsetInStruct / 4 * 4);
        length = 4;
        return true;
    }
}