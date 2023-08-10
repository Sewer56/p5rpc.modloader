using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Elsai;

public struct ElsaiSegment0Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var structOffset = offset / 44 * 44;

        // First two are u16s
        if (structOffset < 4)
        {
            moveBy = (int)(structOffset - structOffset / 2 * 2);
            length = 2;
            return true;
        }

        // Rest are u32s, and conveniently they start at offset already div by 4 so we can skip subtract.
        moveBy = (int)(structOffset - structOffset / 4 * 4);
        length = 4;
        return true;
    }
}