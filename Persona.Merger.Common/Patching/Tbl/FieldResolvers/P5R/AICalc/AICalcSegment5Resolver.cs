using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.AICalc;

// ReSharper disable once InconsistentNaming
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