namespace Persona.Merger.Utilities;

/// <summary>
/// A tuple consisting of a pointer and length.
/// </summary>
public unsafe struct PointerLengthTuple
{
    public byte* Pointer;
    public int Length;
}