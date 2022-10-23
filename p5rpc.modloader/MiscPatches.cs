using System.Diagnostics;
using System.Runtime.InteropServices;
using p5rpc.modloader.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.modloader;

internal unsafe class MiscPatches
{
    private readonly Config _conf;
    private readonly ILogger _logger;
    private readonly IReloadedHooks? _hooks;

    private readonly long _base;
    private readonly Memory _mem;
    private readonly SigScan _scan;

    public MiscPatches(IReloadedHooks? hooks, ILogger logger, Config conf, SigScan scan)
    {
        _hooks = hooks;
        _logger = logger;
        _conf = conf;

        _base = Process.GetCurrentProcess().MainModule!.BaseAddress;
        _mem = Memory.Instance;
        _scan = scan;
    }

    public void Activate()
    {
        _scan.FindPatternOffset("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 40 49 8B F1 4C", (offset) =>
            _wndProcHook = _hooks?.CreateHook<WndProc>(WndProcImpl, _base + offset).Activate(),
            "wnd proc");

        if (_conf.IntroSkip)
        {
            _scan.FindPatternOffset("74 10 C7 07 0C 00 00 00", (offset) =>
                _mem?.SafeWriteRaw((nuint)(_base + offset), new byte[] { 0x90, 0x90 }),
                "intro");
        }
    }

    #region Run In Background

    private const uint WM_ACTIVATE = 0x0006;
    private const uint WM_KILLFOCUS = 0x0008;

    private bool _active = false;

    private IHook<WndProc>? _wndProcHook;

    [Function(CallingConventions.Microsoft)]
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private IntPtr WndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (_conf.RenderInBackground)
        {
            switch (uMsg)
            {
                case WM_ACTIVATE:
                    _active = (ushort)wParam != 0;
                    if (!_active) return IntPtr.Zero;
                    break;

                case WM_KILLFOCUS:
                    _active = false;
                    return IntPtr.Zero;
            }
        }

        return _wndProcHook!.OriginalFunction(hWnd, uMsg, wParam, lParam);
    }

    #endregion Run In Background
}