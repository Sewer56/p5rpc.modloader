using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Item;

public struct MaterialsResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 44 * 44;
        if (offsetInStruct < 4)
        {
            moveBy = (int)offsetInStruct; // - 0
            length = 4; // mutually exclusive bit flags.
            return true;
        }

        if (offsetInStruct is >= 20 and < 22)
        {
            // u8s
            moveBy = 0;
            length = 1;
            return false;
        }

        // Remainder are aligned u16s
        moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
        length = 2; // mutually exclusive bit flags.
        return true;
    }
}