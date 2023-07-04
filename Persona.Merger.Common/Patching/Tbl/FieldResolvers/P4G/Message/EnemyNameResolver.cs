using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Message
{
    public struct EnemyNameResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // All names are 21 characters
            var align = offset / 21 * 21;
            moveBy = (int)(offset - align);
            length = 21;
            return true;
        }
    }
}
