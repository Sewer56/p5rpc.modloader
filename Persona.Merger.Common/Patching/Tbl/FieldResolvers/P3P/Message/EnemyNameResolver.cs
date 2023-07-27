using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Message
{
    public struct EnemyNameResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // All names are 19 characters
            var align = offset / 19 * 19;
            moveBy = (int)(offset - align);
            length = 19;
            return true;
        }
    }
}
