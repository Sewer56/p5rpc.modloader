using System.Diagnostics;
using p5rpc.stuff.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.stuff;

internal class TestPatches
{
    private readonly Config _conf;
    private readonly ILogger _logger;
    private readonly IReloadedHooks? _hooks;

    private readonly long _base;
    private readonly Memory _mem;
    private readonly SigScan _scan;

    public TestPatches(IReloadedHooks? hooks, ILogger logger, Config conf, SigScan scan)
    {
        _conf = conf;
        _logger = logger;
        _hooks = hooks;

        _base = Process.GetCurrentProcess().MainModule!.BaseAddress;
        _mem = Memory.Instance;
        _scan = scan;
    }

    public unsafe void Activate()
    {
    }
}