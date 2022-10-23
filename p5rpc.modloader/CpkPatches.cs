using System.Diagnostics;
using System.Runtime.InteropServices;
using p5rpc.modloader.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.modloader;

internal unsafe class CpkPatches
{
    private readonly Config _conf;
    private readonly ILogger _logger;
    private readonly IReloadedHooks? _hooks;

    private readonly long _base;
    private readonly Memory _mem;
    private readonly SigScan _scan;

    private bool _modsLoaded = false;

    #region Hooks

    private IHook<criFsBinder_BindCpk>? _bindCpkHook;
    private IHook<criFsBinder_BindCpk>? _bindDirHook;

    #endregion Hooks

    #region Delegates

    [Function(CallingConventions.Microsoft)]
    private delegate int criFsBinder_Create(IntPtr bndrhn);

    [Function(CallingConventions.Microsoft)]
    private delegate int criFsBinder_GetStatus(uint bndrid, int* status);

    [Function(CallingConventions.Microsoft)]
    private delegate int criFsBinder_BindCpk(IntPtr bndrhn, IntPtr srcbndrhn, string path, IntPtr work, int worksize, uint* bndrid);

    [Function(CallingConventions.Microsoft)]
    private delegate uint criFsBinder_SetPriority(uint bndrid, int priority);

    [Function(CallingConventions.Microsoft)]
    private delegate int criFsBinder_Unbind(uint bndrid);

    #endregion Delegates

    #region Wrappers

    private criFsBinder_GetStatus? _getStatusWrap;
    private criFsBinder_SetPriority? _setPriorityWrap;
    private criFsBinder_Unbind? _unbindWrap;

    #endregion Wrappers

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
        _scan.FindPatternOffset("48 83 EC 48 48 8B 44 24 ?? C7 44 24 ?? 01 00 00 00 48 89 44 24 ?? 8B 44 24 ??", (offset) =>
            _bindCpkHook = _hooks?.CreateHook<criFsBinder_BindCpk>(BindCpkImpl, _base + offset).Activate(),
            "bind cpk");

        _scan.FindPatternOffset("48 8B C4 48 89 58 08 48 89 68 10 48 89 70 18 48 89 78 20 41 54 41 56 41 57 48 83 EC 40 48", (offset) =>
            _bindDirHook = _hooks?.CreateHook<criFsBinder_BindCpk>(BindDirImpl, _base + offset).Activate(),
            "bind dir");

        _scan.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B FA E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 75 18", (offset) =>
            _setPriorityWrap = _hooks?.CreateWrapper<criFsBinder_SetPriority>(_base + offset, out var _),
            "set prio");

        _scan.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 48 8B DA 8B F9 85", (offset) =>
            _getStatusWrap = _hooks?.CreateWrapper<criFsBinder_GetStatus>(_base + offset, out var _),
            "get status");

        _scan.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B F9 E8 ?? ?? ?? ?? 48 8B", (offset) =>
            _unbindWrap = _hooks?.CreateWrapper<criFsBinder_Unbind>(_base + offset, out var _),
            "unbind");
    }

    private int BindCpkImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, uint* bndrid)
    {
        if (_conf.ModSupport && !_modsLoaded)
        {
            _modsLoaded = true;

            foreach (var bindMod in _conf.BindMods)
            {
                BindModsLoop(bndrhn, bindMod, 0x10000000);
            }
        }

        _logger.WriteLine($"<{path}> bind cpk");

        return _bindCpkHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }

    private int BindDirImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, uint* bndrid)
    {
        _logger.WriteLine($"<{path}> bind dir");

        return _bindDirHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }

    private uint BindModsLoop(IntPtr bndrhn, string path, int priority)
    {
        uint bndrid = 0;
        int status = 0;

        var err = 0;

        if (File.Exists(path))
        {
            _logger.WriteLine($"<{path}> bind file with priority 0x{priority:X8}");
            err = _bindCpkHook!.OriginalFunction(bndrhn, 0, path, 0, 0, &bndrid);
        }
        else if (Directory.Exists(path))
        {
            _logger.WriteLine($"<{path}> bind dir with priority 0x{priority:X8}");
            err = _bindDirHook!.OriginalFunction(bndrhn, 0, path, 0, 0, &bndrid);
        }
        else
        {
            _logger.WriteLine($"<{path}> doesn't exist, skipping");
            return 0;
        }

        if (err > 0)
        {
            // either find a way to handle bindCpk errors properly or ignore

            return 0;
        }

        while (true)
        {
            _getStatusWrap!(bndrid, &status);

            if (status == 2) // complete
            {
                _setPriorityWrap!(bndrid, priority);

                _logger.WriteLine($"<{path}> bind done - id {bndrid}");

                return bndrid;
            }

            if (status == 6) // error
                break;

            Thread.Sleep(10);
        }

        _logger.WriteLine($"<{path}> bind failed");

        _unbindWrap!(bndrid);

        return 0;
    }
}