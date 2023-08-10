using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Effect;

public struct EffectResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // File is undocumented so assume it's all individual bytes
        moveBy = 0;
        length = 1;
        return false;
    }
}
