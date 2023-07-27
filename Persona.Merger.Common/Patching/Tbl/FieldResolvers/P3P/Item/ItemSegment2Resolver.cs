using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Item;

public struct ItemSegment2Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // unknown data
        moveBy = 0;
        length = 1;
        return false;
    }
}