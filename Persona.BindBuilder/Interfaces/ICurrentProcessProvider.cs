namespace Persona.BindBuilder.Interfaces;

/// <summary>
/// Provides the id of the current process.
/// </summary>
public interface ICurrentProcessProvider
{
    /// <summary>
    /// Gets the ID of the current process.
    /// </summary>
    public int GetProcessId();
}