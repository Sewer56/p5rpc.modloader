using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Message
{
    public struct PersonaNameResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // All names are 17 characters
            var align = offset / 17 * 17;
            moveBy = (int)(offset - align);
            length = 17;
            return true;
        }
    }
}
