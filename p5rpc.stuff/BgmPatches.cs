using System.Diagnostics;
using System.Runtime.InteropServices;
using p5rpc.stuff.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace p5rpc.stuff;

internal unsafe class BgmPatches
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BgmCond
    {
        public short id;
        public short outfit;
        public int flag;
    }

    private readonly Config _conf;
    private readonly ILogger _logger;
    private readonly IReloadedHooks? _hooks;

    private readonly long _base;
    private readonly Memory _mem;
    private readonly SigScan _scan;

    private readonly Random _rnd = new((int)DateTime.Now.Ticks);

    private BgmCond _newCond;

    private bool _shouldSetBgm = false;
    private short _lastBgm = 0;

    private IHook<BtlMain>? _btlMainHook;

    private IAsmHook? _rndBgmHook;

    private IReverseWrapper<BgmPatch>? _bgmPatchRWrap;

    [Function(CallingConventions.Microsoft)]
    private delegate BgmCond* BgmPatch(BgmCond* cond);

    [Function(CallingConventions.Microsoft)]
    private delegate IntPtr BtlMain(IntPtr a1, IntPtr a2);

    public BgmPatches(IReloadedHooks? hooks, ILogger logger, Config conf, SigScan scan)
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
        _bgmPatchRWrap = _hooks?.CreateReverseWrapper<BgmPatch>(BgmTestImpl);

        _scan.FindPatternOffset("0F B7 07 48 8B 0D", (offset) =>
        {
            string[] func =
            {
                $"use64",
                "mov rcx, rdi",
                _hooks!.Utilities.GetAbsoluteCallMnemonics(_bgmPatchRWrap!.WrapperPointer, true),
                "mov rdi, rax",
            };

            _rndBgmHook = _hooks?.CreateAsmHook(func, _base + offset, AsmHookBehaviour.ExecuteAfter).Activate();
        }, "bgm patch");

        _scan.FindPatternOffset("4C 8B DC 49 89 5B 08 49 89 6B 10 49 89 73 18 57 41 56 41 57 48 83 EC 40", (offset) =>
            _btlMainHook = _hooks?.CreateHook<BtlMain>(BtlMainImpl, _base + offset).Activate()
        , "btl main");

        if (_conf.BattleBgm != Config.BattleBgmOptions.Normal)
        {
            _scan.FindPatternOffset("0F 8E E8 00 00 00 44 0F B7 07", (offset) =>
                _mem?.SafeWriteRaw((nuint)(_base + offset), new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 })
            , "bgm patch check");
        }
    }

    private IntPtr BtlMainImpl(IntPtr a1, IntPtr a2)
    {
        _shouldSetBgm = true;

        return _btlMainHook!.OriginalFunction(a1, a2);
    }

    private BgmCond* BgmTestImpl(BgmCond* cond)
    {
        if (_conf.BattleBgm == Config.BattleBgmOptions.Normal)
            return cond;

        _newCond.id = cond->id;
        _newCond.outfit = cond->outfit;
        _newCond.flag = cond->flag;

        if (_conf.BattleBgm == Config.BattleBgmOptions.Random)
        {
            if (_shouldSetBgm)
            {
                _lastBgm = (short)_rnd.Next(0, 17);
                _shouldSetBgm = false;
            }

            _newCond.id = _lastBgm;
        }
        else
        {
            if (_shouldSetBgm)
            {
                _lastBgm = (short)((_lastBgm + 1) % 17);
                _shouldSetBgm = false;
            }

            _newCond.id = _lastBgm;
        }

        fixed (BgmCond* pCond = &_newCond)
            return pCond;
    }
}