using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using p5rpc.modloader.Utilities;
using Reloaded.Hooks.Definitions;
using static p5rpc.modloader.Utilities.Native;

namespace p5rpc.modloader.Patches.Common;

/// <summary>
/// Patch that disables pause on focus loss.
/// </summary>
internal static class NoPauseOnFocusLoss
{
    private static IReloadedHooks _hooks = null!;
    private static Logger _logger = null!;
    private static WndProcHook _wndProcHook = null!;
    
    public static void Activate(in PatchContext context)
    {
        _hooks = context.Hooks;
        _logger = context.Logger;
        string windowClass = Mod.Game switch
        {
            Game.P4G => "DX11_P4G__app",
            Game.P5R => "p5r",
            _ => ""
        };

        if (windowClass == "")
            context.Logger.Warning("Not supported for game.");
        else
            _ = Task.Run(async () =>
            {
                await TryHookWndProc(windowClass);
            });
    }

    private static async Task TryHookWndProc(string windowClass)
    {
        while (true)
        {
            var window = FindWindow(windowClass, null);
            if (window == IntPtr.Zero)
            {
                await Task.Delay(1000);
                continue;
            }
            
            unsafe
            {
                _logger.Info("Found Window, Hooking WndProc.");
                var wndProcHandlerPtr = (IntPtr)_hooks.Utilities.GetFunctionPointer(typeof(NoPauseOnFocusLoss), nameof(WndProcImpl));
                _wndProcHook = WndProcHook.Create(_hooks, window, Unsafe.As<IntPtr, WndProcFn>(ref wndProcHandlerPtr));
                return;
            }
            
        }
    }
    
    [UnmanagedCallersOnly]
    private static unsafe IntPtr WndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        var renderInBg = Mod.Configuration.GetShouldRenderInBackground(Mod.Game);
        if (!renderInBg) 
            return _wndProcHook.Hook.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);
        
        switch (uMsg)
        {
            case WM_ACTIVATE:
            case WM_ACTIVATEAPP:
                if (wParam == IntPtr.Zero) 
                    return IntPtr.Zero;
                
                break;

            case WM_KILLFOCUS:
                return IntPtr.Zero;
        }

        return _wndProcHook.Hook.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);
    }
}