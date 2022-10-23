using System.Diagnostics;
using System.Runtime.InteropServices;
using p5rpc.stuff.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.stuff;

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
            _wndProcHook = _hooks?.CreateHook<WndProc>(WndProcImpl, _base + offset).Activate()
        , "wnd proc");

        _scan.FindPatternOffset("40 55 48 8D AC 24 30 FF FF FF 48 81 EC D0 01 00 00 48", (offset) =>
            _unlockTrophyHook = _hooks?.CreateHook<UnlockTrophy>(UnlockTrophyImpl, _base + offset).Activate()
        , "unlock trophy");

        _scan.FindPatternOffset("66 44 89 4C 24 20 44 88", (offset) =>
            _getModelMinorIdHook = _hooks?.CreateHook<GetModelMinorId>(GetModelMinorIdImpl, _base + offset).Activate()
        , "get model minor id");

        _scan.FindPatternOffset("0F B7 C1 48 8D 0D ?? ?? ?? ?? 48 69 C0 A0 02 00 00 48 01", (offset) =>
            _getPartyDatUnitWrap = _hooks?.CreateWrapper<GetPartyDatUnit>(_base + offset, out var _)
        , "get party unit dat");

        //_scan.FindPatternOffset("40 53 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 48 0F", (offset) =>
        //    _loadResourceHook = _hooks?.CreateHook<LoadResource>(LoadResourceImpl, _base + offset).Activate()
        //, "load resource");

        if (_conf.IntroSkip)
        {
            _scan.FindPatternOffset("74 10 C7 07 0C 00 00 00", (offset) =>
                _mem?.SafeWriteRaw((nuint)(_base + offset), new byte[] { 0x90, 0x90 })
            , "intro logo");

            _scan.FindPatternOffset("E8 ?? ?? ?? ?? 48 89 47 10 F6 15", (offset) =>
                _mem?.SafeWriteRaw((nuint)(_base + offset), new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 })
            , "intro movie");
        }
    }

    #region Load Resource

    private IHook<LoadResource>? _loadResourceHook;

    [Function(CallingConventions.Microsoft)]
    private delegate IntPtr LoadResource(ushort type, byte a2, ushort index, ushort major, byte minor, byte sub, short a7, IntPtr a8, ushort a9, short a10);

    private IntPtr LoadResourceImpl(ushort type, byte a2, ushort index, ushort major, byte minor, byte sub, short a7, IntPtr a8, ushort a9, short a10)
    {
        _logger.WriteLine($"load resource {type} {a2} {index} {major} {minor} {sub} {a7} {a8} {a9} {a10}");
        return _loadResourceHook!.OriginalFunction(type, a2, index, major, minor, sub, a7, a8, a9, a10);
    }

    #endregion Load Resource

    #region Cutscene Outfits

    private readonly Dictionary<int, int[]> _outfitWhitelist = new()
    {
        [001] = new[] { 50, 51, 103, 52 },
        [002] = new[] { 50, 51, 103, 106, 109 },
        [003] = new[] { 50, 51, 101 },
        [004] = new[] { 50, 51, 106, 107, 109 },
        [005] = new[] { 50, 51, 103, 106, 107 },
        [006] = new[] { 50, 51, 104, 107, 109 },
        [007] = new[] { 50, 51, 102, 103, 107 },
        [008] = new[] { 50, 51, 102, 106, 108 },
        [009] = new[] { 50, 51, 52, 102, 104, 105, 106 },
        [010] = new[] { 50, 51, 102, 106, 107 },
    };

    private IHook<GetModelMinorId>? _getModelMinorIdHook;

    private GetPartyDatUnit? _getPartyDatUnitWrap;

    [Function(CallingConventions.Microsoft)]
    private delegate byte GetModelMinorId(ushort major, byte minor, byte a3, ushort a4, ushort a5);

    [Function(CallingConventions.Microsoft)]
    private delegate BtlUnit* GetPartyDatUnit(ushort unitId);

    private byte GetModelMinorIdImpl(ushort major, byte minor, byte a3, ushort a4, ushort a5)
    {
        if (_conf.CutsceneOutfits)
        {
            if (major < 11 && _outfitWhitelist.ContainsKey(major))
            {
                if (_outfitWhitelist[major].Contains(minor))
                {
                    var outfit = _getPartyDatUnitWrap!(major)->equip[3];
                    var outfitIdx = outfit - 28688;

                    if (outfit - 28688 <= 0)
                        outfitIdx = 0;

                    var charOutfitIdx = outfitIdx / 10;

                    if (charOutfitIdx == 0)
                    {
                        if (major == 9 && outfit == 28685)
                            return 52;
                        else
                            return 51;
                    }

                    return (byte)(charOutfitIdx - 106);
                }
            }
        }

        return _getModelMinorIdHook!.OriginalFunction(major, minor, a3, a4, a5);
    }

    #endregion Cutscene Outfits

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

    #region Unlock Trophy

    private IHook<UnlockTrophy>? _unlockTrophyHook;

    [Function(CallingConventions.Microsoft)]
    private delegate IntPtr UnlockTrophy(int trophyId);

    private IntPtr UnlockTrophyImpl(int trophyId)
    {
        //_logger.WriteLine($"trophy {trophyId}");

        if (_conf.NoTrophy)
        {
            return IntPtr.Zero;
        }

        return _unlockTrophyHook!.OriginalFunction(trophyId);
    }

    #endregion Unlock Trophy
}

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct BtlUnit
{
    [FieldOffset(0x284)]
    public fixed ushort equip[5];
}