using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Message
{
    public struct EmbeddedBmdResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            // TODO don't use this, merge with BMD emulator (you can't binary diff bmds)
            moveBy = 0;
            length = 1;
            return false;
        }
    }
}
