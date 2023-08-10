using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Item;

public struct OutfitsResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 32 * 32;
        if (offsetInStruct < 4)
        {
            moveBy = (int)offsetInStruct; // - 0
            length = 4; // mutually exclusive bit flags.
            return true;
        }

        // Remainder are aligned u16s (that we know of)
        moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
        length = 2; // mutually exclusive bit flags.
        return true;
    }
}