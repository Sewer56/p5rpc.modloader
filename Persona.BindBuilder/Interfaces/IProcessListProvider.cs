namespace Persona.BindBuilder.Interfaces;

/// <summary>
/// Provides a list of active running processes.
/// </summary>
public interface IProcessListProvider
{
    /// <summary>
    /// Returns a list of all available process IDs.
    /// </summary>
    /// <returns>List of all process IDs.</returns>
    public int[] GetProcessIds();
}