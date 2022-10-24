using System.Runtime.InteropServices;
using p5rpc.modloader.Patches.Common;
using Reloaded.Hooks.Definitions;
using static p5rpc.modloader.Utilities.Native;

namespace p5rpc.modloader.Patches;

/// <summary>
/// Patch that disables pause on focus loss.
/// </summary>
internal static unsafe class NoPauseOnFocusLoss
{
    private static bool _active = false;
    private static IHook<WndProcFn>? _wndProcHook;
    
    public static void Activate(in PatchContext context)
    {
        var hooks = context.Hooks;
        var baseAddr = context.BaseAddress;
        context.ScanHelper.FindPatternOffset("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 40 49 8B F1 4C", (offset) => 
            _wndProcHook = hooks.CreateHook<WndProcFn>(typeof(NoPauseOnFocusLoss), nameof(WndProcImpl), (nint)baseAddr + offset).Activate(),
            "WndProc");
    }
    
    [UnmanagedCallersOnly]
    private static IntPtr WndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (!Mod.Configuration.RenderInBackground) 
            return _wndProcHook!.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);
        
        switch (uMsg)
        {
            case WM_ACTIVATE:
                _active = (ushort)wParam != 0;
                if (!_active) 
                    return IntPtr.Zero;
                
                break;

            case WM_KILLFOCUS:
                _active = false;
                return IntPtr.Zero;
        }

        return _wndProcHook!.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);
    }
}