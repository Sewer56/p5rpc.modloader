using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.Utilities;
using p5rpc.modloader.Utilities;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using static p5rpc.modloader.Utilities.CRI;
using static p5rpc.modloader.Utilities.CRI.CriFsBinderStatus;

namespace p5rpc.modloader;

/// <summary>
/// Class to bind our custom CPKs via hooking.
/// </summary>
public unsafe class CpkBinder
{
    private readonly string _outputDirectory;
    private readonly Logger _logger;

    private IHook<criFsBinder_BindCpk>? _bindCpkHook;
    private criFsBinder_BindCpk? _bindDir;
    private criFsBinder_GetWorkSizeForBindDirectory? _getSizeForBindDir;
    private criFsBinder_GetStatus? _getStatus;
    private criFsBinder_SetPriority? _setPriority;
    private criFsBinder_Unbind? _unbind;
    private bool _firstCpkLoaded;

    public CpkBinder(string outputDirectory, Logger logger, IReloadedHooks hooks)
    {
        _outputDirectory = outputDirectory;
        _logger = logger;
        _bindCpkHook = hooks.CreateHook<criFsBinder_BindCpk>(BindCpkImpl, CpkBinderPointers._bindCpk).Activate();
        _bindDir = hooks.CreateWrapper<criFsBinder_BindCpk>(CpkBinderPointers._bindDir, out _);
        _getSizeForBindDir = hooks.CreateWrapper<criFsBinder_GetWorkSizeForBindDirectory>(CpkBinderPointers._getSizeForBindDir, out _);
        _setPriority = hooks.CreateWrapper<criFsBinder_SetPriority>(CpkBinderPointers._setPriority, out _);
        _getStatus = hooks.CreateWrapper<criFsBinder_GetStatus>(CpkBinderPointers._getStatus, out _);
        _unbind = hooks.CreateWrapper<criFsBinder_Unbind>(CpkBinderPointers._unbind, out _);
    }
    
    private CriError BindCpkImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, uint* bndrid)
    {
        if (Mod.Configuration.Common.ModSupport && !_firstCpkLoaded)
            BindAllModFolders(bndrhn);

        _firstCpkLoaded = true;
        return _bindCpkHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }

    private void BindAllModFolders(IntPtr bndrhn)
    {
        _logger.Info("Setting Up Binds!!");
        WindowsDirectorySearcher.TryGetDirectoryContents(_outputDirectory, out _, out var directories);
        foreach (var directory in directories)
            BindFolder(bndrhn, directory.FullPath, 0x10000000);
    }

    private uint BindFolder(IntPtr bndrhn, string path, int priority)
    {
        uint bndrid = 0;
        CriFsBinderStatus status = 0;
        CriError err = 0;
        
        _logger.Debug("Binding Directory {0} with priority {1}", path, priority);
        int size = 0;
        err = _getSizeForBindDir!(bndrhn, path, &size);
        if (err < 0)
        {
            _logger.Error("Binding Directory Failed: Failed to get size of Bind Directory {0}", err);
            return 0;
        }

        var workMem = Memory.Instance.Allocate(size);
        err = _bindDir!(bndrhn, IntPtr.Zero, path, (nint)workMem, size, &bndrid);
        
        if (err < 0)
        {
            // either find a way to handle bindCpk errors properly or ignore
            _logger.Error("Binding Directory Failed with Error {0}", err);
            return 0;
        }

        while (true)
        {
            _getStatus!(bndrid, &status);
            switch (status)
            {
                case CRIFSBINDER_STATUS_COMPLETE:
                    _setPriority!(bndrid, priority);
                    _logger.Debug("Bind Complete! {0}, Id: {1}", path, bndrid);
                    return bndrid;
                case CRIFSBINDER_STATUS_ERROR:
                    _logger.Debug("Bind Failed! {0}", path);
                    _unbind!(bndrid);
                    return 0;
                default:
                    Thread.Sleep(10);
                    break;
            }
        }
    }
}

internal static class CpkBinderPointers
{
    internal static long _bindCpk;
    internal static long _bindDir;
    internal static long _getSizeForBindDir;
    internal static long _setPriority;
    internal static long _getStatus;
    internal static long _unbind;
    
    public static void Init(SigScanHelper helper, nint baseAddr)
    {
        helper.FindPatternOffset("48 83 EC 48 48 8B 44 24 78 C7 44 24 30 01 00 00 00 48 89 44 24 28 8B", 
            (offset) => _bindCpk = baseAddr + offset, "CRI Bind CPK");

        helper.FindPatternOffset("48 8B C4 48 89 58 08 48 89 68 10 48 89 70 18 48 89 78 20 41 54 41 56 41 57 48 83 EC 40 48", 
            (offset) => _bindDir = baseAddr + offset, "CRI Bind Directory");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B FA E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 75 18", 
            (offset) => _setPriority = baseAddr + offset, "CRI Set Priority");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 48 8B DA 8B F9 85", 
            (offset) => _getStatus = baseAddr + offset, "CRI Get Status");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B F9 E8 ?? ?? ?? ?? 48 8B", 
            (offset) => _unbind = baseAddr + offset, "CRI Unbind");
        
        helper.FindPatternOffset("48 83 EC 28 4D 85 C0 75 1B", 
            (offset) => _getSizeForBindDir = baseAddr + offset, "CRI Get Size for Bind Dir");
    }
}