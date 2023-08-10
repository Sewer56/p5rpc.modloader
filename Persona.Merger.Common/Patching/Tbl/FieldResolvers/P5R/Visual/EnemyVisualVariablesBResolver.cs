using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Visual;

public struct EnemyVisualVariablesBResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Known to be u32s
        moveBy = (int)(offset - offset / 4 * 4);
        length = 4;
        return true;
    }
}