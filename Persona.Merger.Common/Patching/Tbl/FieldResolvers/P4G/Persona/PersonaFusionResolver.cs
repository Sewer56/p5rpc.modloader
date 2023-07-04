using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Persona;

public struct PersonaFusionResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // All data are u16s.
        var twoByteAligned = offset / 2 * 2;
        moveBy = (int)(offset - twoByteAligned);
        length = 2;
        return true;
    }
}