using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Encounter;

public struct EncountSegment3Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Segment is undocumented so assume it's all individual bytes
        moveBy = 0;
        length = 1;
        return false;
    }
}
