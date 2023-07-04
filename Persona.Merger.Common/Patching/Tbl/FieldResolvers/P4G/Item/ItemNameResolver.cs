using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Item;

public struct ItemNameResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Names are all 24 characters
        var align = offset / 24 * 24;
        moveBy = (int)(offset - align);
        length = 24;
        return true;
    }
}