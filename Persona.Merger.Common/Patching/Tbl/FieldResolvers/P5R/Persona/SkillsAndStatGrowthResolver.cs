using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Persona;

public struct SkillsAndStatGrowthResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // TODO: This needs testing.
        var offsetInStruct = offset - offset / 70 * 70;

        // Before first non-byte value
        moveBy = 0;
        length = 1;
        if (offsetInStruct < 12)
            return false;

        // Move to skills section.
        offsetInStruct -= 6;
        return PersonaSkillResolver.Instance.Resolve(offsetInStruct, out moveBy, out length);
    }
}