using System.Diagnostics;
using Persona.BindBuilder.Interfaces;

namespace Persona.BindBuilder.Utilities;

/// <summary>
/// Provides a list of active processes, using a cache.
/// </summary>
public class ProcessListProvider : IProcessListProvider
{
    private int[] _procIds;
    
    public ProcessListProvider()
    {
        var processes = Process.GetProcesses();
        _procIds = GC.AllocateUninitializedArray<int>(processes.Length);
        for (var x = 0; x < processes.Length; x++)
            _procIds[x] = processes[x].Id;
    }
    
    public int[] GetProcessIds() => _procIds;
}