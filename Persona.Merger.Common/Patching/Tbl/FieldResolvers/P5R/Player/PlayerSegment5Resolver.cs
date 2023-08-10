using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Player;

public struct PlayerSegment5Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Unknown data.
        moveBy = 0;
        length = 1;
        return false;
    }
}