using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Visual;

public struct PersonaVisualVariablesAResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // TODO: This structure, I Sewer am lazy to reverse this for now.
        // Gonna assume mostly u16 for now.
        moveBy = (int)(offset - offset / 2 * 2);
        length = 2;
        return true;
    }
}