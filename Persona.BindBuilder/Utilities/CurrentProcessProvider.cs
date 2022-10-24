using System.Diagnostics;
using Persona.BindBuilder.Interfaces;

namespace Persona.BindBuilder.Utilities;

/// <summary>
/// Provides the ID of the current process.
/// </summary>
public class CurrentProcessProvider : ICurrentProcessProvider
{
    private int _currentProcId;
    
    public CurrentProcessProvider() => _currentProcId = Process.GetCurrentProcess().Id;

    public CurrentProcessProvider(int currentProcId) => _currentProcId = currentProcId;

    public int GetProcessId() => _currentProcId;
}