using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers;

public struct ByteResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        moveBy = 0;
        length = 1;
        return false;
    }
}