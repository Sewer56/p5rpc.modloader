using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.Persona;

public struct PartyPersonasResolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // This is a big struct, wow.
        var structOffset = offset / 622 * 622;

        // First field is a u16.
        if (structOffset < 2)
        {
            moveBy = (int)structOffset; // - 0
            length = 2;
            return true;
        }

        // Then is an array of skills
        if (structOffset is >= 4 and < 132)
            return PersonaSkillResolver.Instance.Resolve(structOffset - 4, out moveBy, out length);

        // Else it's an u8
        moveBy = 0;
        length = 1;
        return false;
    }
}