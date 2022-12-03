using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.Visual;

public struct PersonaVisualVariablesBResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        moveBy = 0;
        length = 1;
        return false;
    }
}