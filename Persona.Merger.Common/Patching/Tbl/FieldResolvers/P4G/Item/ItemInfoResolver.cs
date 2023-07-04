using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Item;

public struct ItemInfoResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset - offset / 68 * 68;

        // There's one u32
        if(offsetInStruct is 16)
        {
            moveBy = (int)(offsetInStruct - 4);
            length = 4;
            return true;
        }

        // I know it's not aligned, it's weird...
        if (offsetInStruct >= 5)
        {
            moveBy = (int)(offsetInStruct - 2);
            length = 2;
            return true;
        }

        // Everything else is u8s
        moveBy = 0;
        length = 1;
        return false;
    }
}
