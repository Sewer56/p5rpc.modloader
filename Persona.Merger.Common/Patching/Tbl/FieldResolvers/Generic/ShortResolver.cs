using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers;

public struct ShortResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var twoByteAligned = offset / 2 * 2;
        moveBy = (int)(offset - twoByteAligned);
        length = 2;
        return true;
    }
}