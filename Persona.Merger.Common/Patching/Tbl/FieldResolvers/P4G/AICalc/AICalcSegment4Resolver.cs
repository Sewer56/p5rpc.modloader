using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P4G.AICalc;

// ReSharper disable once InconsistentNaming
public struct AICalcSegment4Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // unknown data
        moveBy = 0;
        length = 1;
        return false;

    }
}