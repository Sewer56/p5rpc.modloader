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
            var fourByteAligned = offsetInStruct / 4 * 4;
            moveBy = (int)(offsetInStruct - fourByteAligned);
            length = 4;
            return true;
        }

        // This one short isn't aligned, ik it's weird
        if(offsetInStruct is 5 or 6)
        {
            moveBy = (int)(offsetInStruct - 5);
            length = 2;
            return true;
        }

        // Everything after that weird one is aligned shorts
        if (offsetInStruct >= 8)
        {
            var twoByteAligned = offsetInStruct / 2 * 2;
            moveBy = (int)(offsetInStruct - twoByteAligned);
            length = 2;
            return true;
        }

        // Everything else is u8s
        moveBy = 0;
        length = 1;
        return false;
    }
}
