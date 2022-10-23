using System.Diagnostics;
using System.Runtime.InteropServices;
using p5rpc.stuff.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.stuff;

internal class CpkPatches
{
    private readonly Config _conf;
    private readonly ILogger _logger;
    private readonly IReloadedHooks? _hooks;

    private readonly long _base;
    private readonly Memory _mem;
    private readonly SigScan _scan;

    private IHook<BindCpk>? _bindCpkHook;
    private IHook<BindCpk>? _bindDirHook;
    private IHook<PathExists>? _pathExistHook;

    private IAsmHook? _preBindCpkHook;

    private IReverseWrapper<LoadMods>? _modSupportRWrap;

    private MountCpk? _mountCpkWrap;

    [Function(CallingConventions.Microsoft)]
    private delegate void MountCpk(int loadType, string cpkName, int a3);

    [Function(CallingConventions.Microsoft)]
    private delegate int PathExists(string path);

    [Function(CallingConventions.Microsoft)]
    private delegate int BindCpk(IntPtr bndrhn, IntPtr srcbndrhn, string path, IntPtr work, int worksize, IntPtr bndrid);

    [Function(CallingConventions.Microsoft)]
    private delegate void LoadMods();

    public CpkPatches(IReloadedHooks? hooks, ILogger logger, Config conf, SigScan scan)
    {
        _conf = conf;
        _logger = logger;
        _hooks = hooks;

        _base = Process.GetCurrentProcess().MainModule!.BaseAddress;
        _mem = Memory.Instance;
        _scan = scan;
    }

    public void Activate()
    {
        _modSupportRWrap = _hooks?.CreateReverseWrapper<LoadMods>(LoadModsImpl);

        _scan.FindPatternOffset("48 89 5C 24 ?? 48 89 7C 24 ?? 55 48 8D AC 24 ?? ?? ?? ?? 48 81 EC E0 01 00 00 48 83 3D ?? ?? ?? ?? 00", (offset) =>
            _mountCpkWrap = _hooks?.CreateWrapper<MountCpk>(_base + offset, out var _)
        , "mount cpk");

        _scan.FindPatternOffset("33 C9 E8 ?? ?? ?? ?? 41 8B CC 48 89 05", (offset) =>
        {
            string[] func =
            {
                $"use64",
                _hooks!.Utilities.GetAbsoluteCallMnemonics(_modSupportRWrap!.WrapperPointer, true),
            };

            _preBindCpkHook = _hooks?.CreateAsmHook(func, _base + offset, AsmHookBehaviour.ExecuteFirst);
            _preBindCpkHook?.Activate();
        }, "pre mount");

        _scan.FindPatternOffset("48 83 EC 28 48 89 CA", (offset) =>
            _pathExistHook = _hooks?.CreateHook<PathExists>(PathExistsImpl, _base + offset).Activate()
        , "path exists");

        _scan.FindPatternOffset("48 83 EC 48 48 8B 44 24 ?? C7 44 24 ?? 01 00 00 00 48 89 44 24 ?? 8B 44 24 ??", (offset) =>
            _bindCpkHook = _hooks?.CreateHook<BindCpk>(BindCpkImpl, _base + offset).Activate()
        , "bind cpk");

        _scan.FindPatternOffset("48 8B C4 48 89 58 08 48 89 68 10 48 89 70 18 48 89 78 20 41 54 41 56 41 57 48 83 EC 40 48", (offset) =>
            _bindDirHook = _hooks?.CreateHook<BindCpk>(BindDirImpl, _base + offset).Activate()
        , "bind dir");
    }

    private void LoadModsImpl()
    {
        if (!_conf.ModSupport)
            return;

        _mountCpkWrap!(1, "BIND/", 0);
        _mountCpkWrap!(1, "BIND1/", 0);
        _mountCpkWrap!(1, "BIND2/", 0);
        _mountCpkWrap!(1, "BIND3/", 0);

        _mountCpkWrap!(1, "MOD", 0);
        _mountCpkWrap!(1, "MOD1", 0);
        _mountCpkWrap!(1, "MOD2", 0);
        _mountCpkWrap!(1, "MOD3", 0);
    }

    private int PathExistsImpl([MarshalAs(UnmanagedType.LPStr)] string path)
    {
        if (path.Contains("/.CPK") && Directory.Exists(path.Replace("/.CPK", "/")))
            return 1;

        return _pathExistHook!.OriginalFunction(path);
    }

    private int BindCpkImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, IntPtr bndrid)
    {
        if (_bindDirHook != null && path.Contains("/.CPK"))
            return BindDirImpl(bndrhn, srcbndrhn, path.Replace("/.CPK", "/"), work, worksize, bndrid);

        _logger.WriteLine($"bind cpk {path.Replace("/.CPK", "/")}");
        return _bindCpkHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }

    private int BindDirImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, IntPtr bndrid)
    {
        _logger.WriteLine($"bind dir {path}");
        return _bindDirHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }
}