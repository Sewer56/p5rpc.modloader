using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.AICalc;

public struct AICalcSegment5Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // bytes
        moveBy = 0;
        length = 1;
        return false;
    }
}