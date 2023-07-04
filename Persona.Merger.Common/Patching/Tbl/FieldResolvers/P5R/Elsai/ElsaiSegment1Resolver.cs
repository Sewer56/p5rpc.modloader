using Sewer56.StructuredDiff.Interfaces;

namespace Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Elsai;

public struct ElsaiSegment1Resolver : IEncoderFieldResolver
{
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        var offsetInStruct = offset / 320 * 320;

        switch (offsetInStruct)
        {
            case < 20:
                moveBy = (int)(offsetInStruct - offsetInStruct / 4 * 4);
                length = 4;
                return true;

            case < 308:
                {
                    // ELSAISegment2EntryEntry
                    offsetInStruct -= 20;
                    offsetInStruct = offsetInStruct - offsetInStruct / 32 * 32;

                    if (offsetInStruct < 8)
                    {
                        moveBy = (int)(offsetInStruct - offsetInStruct / 4 * 4);
                        length = 4;
                        return true;
                    }

                    moveBy = 0;
                    length = 1;
                    return false;
                }
        }

        moveBy = (int)(offsetInStruct - offsetInStruct / 2 * 2);
        length = 2;
        return true;
    }
}