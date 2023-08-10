using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Skill
{
    public struct ElementsResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // All data are individual bytes.
            moveBy = 0;
            length = 1;
            return false;
        }
    }
}
