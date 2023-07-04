using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Persona;

public struct PersonaSkillResolver : IEncoderFieldResolver
{
    public static PersonaSkillResolver Instance = new PersonaSkillResolver();

    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // Get offset from current skill.
        var offsetInStruct = offset - offset / 4 * 4;

        // There's a u16 in skills section.
        if (offsetInStruct is 2 or 3)
        {
            moveBy = (int)(offsetInStruct - 2);
            length = 2;
            return true;
        }
        else
        {
            // Else it's a u8
            moveBy = 0;
            length = 1;
            return false;
        }
    }
}