using System.Runtime.InteropServices;
using p5rpc.modloader.Patches.Common;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Structs;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Pointers;
using Reloaded.Memory.Sources;

namespace p5rpc.modloader.Patches.P3P;

/// <summary>
/// Skips the game introduction sequence.
/// </summary>
internal unsafe class SkipIntro
{
    private static IHook<MenuStateMachineFn> _menuStateMachineHook = null!;
    
    public static void Activate(in PatchContext context)
    {
        var baseAddr = context.BaseAddress;
        if (!context.Config.P3PConfig.IntroSkip) 
            return;
        
        P3PSkipIntroImpl(baseAddr, "48 89 5C 24 ?? 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5F ?? 48 63 03", context);
    }

    internal static void P3PSkipIntroImpl(nint baseAddr, string pattern, in PatchContext context)
    {
        var hooks = context.Hooks;
        context.ScanHelper.FindPatternOffset(pattern,
            (offset) =>
            {
                var addr = baseAddr + offset;
                _menuStateMachineHook = hooks.CreateHook<MenuStateMachineFn>(typeof(SkipIntro), nameof(MenuStateMachineImpl), addr).Activate();
            },
            "Introduction Skip");
    }

    [UnmanagedCallersOnly]
    private static long MenuStateMachineImpl(MenuStateMachineData* param)
    {
        if (*param->State < 4)
            *param->State = 4;
        
        return _menuStateMachineHook.OriginalFunction.Value.Invoke(param);
    }
    
    [Function(CallingConventions.Microsoft)]
    public struct MenuStateMachineFn { public FuncPtr<BlittablePointer<MenuStateMachineData>, IntPtr> Value; }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MenuStateMachineData
    {
        [FieldOffset(0x48)] 
        public long* State;
    }
}