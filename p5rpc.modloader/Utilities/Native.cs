using Reloaded.Hooks.Definitions.Structs;
using Reloaded.Hooks.Definitions.X64;

namespace p5rpc.modloader.Utilities;

/// <summary>
/// All native definitions.
/// </summary>
public static class Native
{
    public const uint WM_ACTIVATE = 0x0006;
    public const uint WM_KILLFOCUS = 0x0008;

    // IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
    [Function(CallingConventions.Microsoft)]
    public struct WndProcFn { public FuncPtr<IntPtr, uint, IntPtr, IntPtr, IntPtr> Value; }
}