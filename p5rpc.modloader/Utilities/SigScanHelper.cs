using FileEmulationFramework.Lib.Utilities;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace p5rpc.modloader.Utilities;

/// <summary>
/// Utility class for querying sigscans to be done in parallel.
/// </summary>
public class SigScanHelper
{
    private readonly IStartupScanner? _startupScanner;
    private readonly Logger? _logger;

    public SigScanHelper(Logger? logger, IStartupScanner? startupScanner)
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
                    _logger?.Info("{0} found", name);

                action((uint)res.Offset);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                _logger?.Error("{0} not found. If you're using latest up to date Steam version, raise a GitHub issue.", name);
            }
        });
    }
}