using System.Diagnostics;
using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.Utilities;
using p5rpc.modloader.Utilities;
using Persona.BindBuilder;
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
    private readonly string _bindString;
    private readonly Logger _logger;

    private IHook<criFsBinder_BindCpk>? _bindCpkHook;
    private IHook<criFsLoader_LoadRegisteredFile_Internal>? _loadRegisteredFile;
    private IHook<criFs_CalculateWorkSizeForLibrary>? _calculateWorkSizeForLibraryHook;
    
    private criFsBinder_BindFiles? _bindFiles;
    private criFsBinder_GetWorkSizeForBindFiles? _getSizeForBindFiles;
    private criFsBinder_GetStatus? _getStatus;
    private criFsBinder_SetPriority? _setPriority;
    private criFsBinder_Unbind? _unbind;
    private bool _firstCpkLoaded;
    private object _lockObject = new();

    public CpkBinder(string bindString, Logger logger, IReloadedHooks hooks)
    {
        _bindString = bindString;
        _logger = logger;
        _bindCpkHook = hooks.CreateHook<criFsBinder_BindCpk>(BindCpkImpl, CpkBinderPointers._bindCpk).Activate();
        _loadRegisteredFile = hooks.CreateHook<criFsLoader_LoadRegisteredFile_Internal>(LoadRegisteredFileInternal, CpkBinderPointers._loadRegisteredFile).Activate();
        _calculateWorkSizeForLibraryHook = hooks.CreateHook<criFs_CalculateWorkSizeForLibrary>(CalculateWorkSizeForLibrary, CpkBinderPointers._calculateWorkSizeForLibrary).Activate();
        
        _bindFiles = hooks.CreateWrapper<criFsBinder_BindFiles>(CpkBinderPointers._bindFiles, out _);
        _getSizeForBindFiles = hooks.CreateWrapper<criFsBinder_GetWorkSizeForBindFiles>(CpkBinderPointers._getSizeForBindFiles, out _);
        _setPriority = hooks.CreateWrapper<criFsBinder_SetPriority>(CpkBinderPointers._setPriority, out _);
        _getStatus = hooks.CreateWrapper<criFsBinder_GetStatus>(CpkBinderPointers._getStatus, out _);
        _unbind = hooks.CreateWrapper<criFsBinder_Unbind>(CpkBinderPointers._unbind, out _);
    }

    private CriFsConfig* _currentConfigPtr;
    
    private CriError CalculateWorkSizeForLibrary(CriFsConfig* config, int* bufferSize)
    {
        lock (_lockObject)
        {
            // Disallow recursion on self.
            if (_currentConfigPtr != (void*)0)
                return _calculateWorkSizeForLibraryHook!.OriginalFunction(config, bufferSize);
            
            _currentConfigPtr = config;
            int maxFiles = 0; // Double in case user adds files.
            foreach (var bindChar in _bindString)
            {
                if (bindChar == CpkBindStringBuilder.Delimiter)
                    maxFiles += 1;
            }
        
            if (Mod.Configuration.Common.HotReload)
                maxFiles *= 10;

            // Increment number of max files.
            config->MaxFiles += maxFiles;
            return _calculateWorkSizeForLibraryHook!.OriginalFunction(config, bufferSize);
        }
        
    }

    private IntPtr LoadRegisteredFileInternal(IntPtr a1, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5)
    {
        if (Mod.Configuration.Common.PrintFileAccess)
        {
            var namePtr = (IntPtr*)IntPtr.Add(a1, 16);
            _logger.Info(Marshal.PtrToStringAnsi(*namePtr)!);
        }

        return _loadRegisteredFile!.OriginalFunction(a1, a2, a3, a4, a5);
    }

    private CriError BindCpkImpl(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, uint* bndrid)
    {
        if (Mod.Configuration.Common.ModSupport && !_firstCpkLoaded)
            BindAllFiles(bndrhn);

        _firstCpkLoaded = true;
        return _bindCpkHook!.OriginalFunction(bndrhn, srcbndrhn, path, work, worksize, bndrid);
    }

    private int BindAllFiles(IntPtr bndrhn)
    {
        _logger.Info("Setting Up Binds!!");
        uint bndrid = 0;
        CriFsBinderStatus status = 0;
        CriError err = 0;
        var priority = int.MaxValue;
        
        _logger.Debug("Binding {0} Files with priority {1}", _bindString, priority);
        var nativeBindStr = Marshal.StringToHGlobalAnsi(_bindString);
        try
        {
            int size = 0;
            err = _getSizeForBindFiles!(bndrhn, nativeBindStr, &size);
            if (err < 0)
            {
                _logger.Error("Binding Directory Failed: Failed to get size of Bind Directory {0}", err);
                return 0;
            }

            var workMem = Memory.Instance.Allocate(size);
            err = _bindFiles!(bndrhn, IntPtr.Zero, nativeBindStr, (nint)workMem, size, &bndrid);
        
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
                        _logger.Debug("Bind Complete! Id: {0}", bndrid);
                        return (int)bndrid;
                    case CRIFSBINDER_STATUS_ERROR:
                        _logger.Debug("Bind Failed!");
                        _unbind!(bndrid);
                        return 0;
                    default:
                        Thread.Sleep(10);
                        break;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(nativeBindStr);
        }
    }
}

internal static class CpkBinderPointers
{
    internal static long _calculateWorkSizeForLibrary;
    internal static long _bindCpk;
    internal static long _bindFiles;
    internal static long _getSizeForBindFiles;
    internal static long _loadRegisteredFile;
    internal static long _setPriority;
    internal static long _getStatus;
    internal static long _unbind;
    
    public static void Init(SigScanHelper helper, nint baseAddr)
    {
        helper.FindPatternOffset("48 89 5C 24 18 48 89 74 24 20 55 57 41 54 41 56 41 57 48 8D 6C 24 C9 48 81 EC A0", 
            (offset) => _calculateWorkSizeForLibrary = baseAddr + offset, "CRI Calculate Work Size for Library");
        
        helper.FindPatternOffset("48 83 EC 48 48 8B 44 24 78 C7 44 24 30 01 00 00 00 48 89 44 24 28 8B", 
            (offset) => _bindCpk = baseAddr + offset, "CRI Bind CPK");

        helper.FindPatternOffset("48 83 EC 48 48 8B 44 24 78 48 89 44 24 30 8B 44 24 70 89 44 24 28 4C 89 4C 24 20 41 83", 
            (offset) => _bindFiles = baseAddr + offset, "CRI Bind Files");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B FA E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 75 18", 
            (offset) => _setPriority = baseAddr + offset, "CRI Set Priority");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 48 8B DA 8B F9 85", 
            (offset) => _getStatus = baseAddr + offset, "CRI Get Status");

        helper.FindPatternOffset("48 89 5C 24 08 57 48 83 EC 20 8B F9 E8 ?? ?? ?? ?? 48 8B", 
            (offset) => _unbind = baseAddr + offset, "CRI Unbind");
        
        helper.FindPatternOffset("48 89 5C 24 08 48 89 74 24 20 57 48 81 EC 50", 
            (offset) => _getSizeForBindFiles = baseAddr + offset, "CRI Get Size for Bind Files");
            
        helper.FindPatternOffset("48 89 5C 24 10 4C 89 4C 24 20 55 56 57 41 54 41 55 41 56 41 57 48 81", 
            (offset) => _loadRegisteredFile = baseAddr + offset, "CRI Load File");
    }
}