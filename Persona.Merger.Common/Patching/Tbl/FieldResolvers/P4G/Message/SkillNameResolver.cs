using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Message
{
    public struct SkillNameResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // All names are 23 characters
            var align = offset / 23 * 23;
            moveBy = (int)(offset - align);
            length = 23;
            return true;
        }
    }
}
