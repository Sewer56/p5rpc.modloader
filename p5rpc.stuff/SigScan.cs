using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace p5rpc.stuff;

internal class SigScan
{
    private readonly IStartupScanner? _startupScanner;
    private readonly ILogger? _logger;

    public SigScan(ILogger? logger, IStartupScanner? startupScanner)
    {
        _logger = logger;
        _startupScanner = startupScanner;
    }

    public void FindPatternOffset(string? pattern, Action<uint> action, string? name = null)
    {
        _startupScanner?.AddMainModuleScan(pattern, (res) =>
        {
            if (res.Found)
            {
                if (!string.IsNullOrEmpty(name))
                    _logger?.WriteLine($"{name} found");

                action((uint)res.Offset);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                _logger?.WriteLine($"{name} not found");
            }
        });
    }
}