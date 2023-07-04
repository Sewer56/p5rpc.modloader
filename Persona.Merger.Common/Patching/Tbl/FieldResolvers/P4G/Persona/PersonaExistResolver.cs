using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Persona;

public struct PersonaExistResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Everything is u8s
        moveBy = 0;
        length = 1;
        return false;
    }
}