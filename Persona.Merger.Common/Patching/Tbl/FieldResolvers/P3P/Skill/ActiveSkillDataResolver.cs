using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P3P.Skill
{
    public struct ActiveSkillDataResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            var offsetInStruct = offset - offset / 40 * 40;
            // There are three u16s betweeen 18 and 22
            if (offsetInStruct is >= 18 and < 24)
            {
                var twoByteAligned = offsetInStruct / 2 * 2;
                moveBy = (int)(offsetInStruct - twoByteAligned);
                length = 2;
                return true;

            }
            // Everything else is individual bytes
            moveBy = 0;
            length = 1;
            return false;
        }
    }
}
