using p5rpc.modloader.Patches.Common;
using Reloaded.Memory.Sources;

namespace p5rpc.modloader.Patches.P4G;

/// <summary>
/// Skips the game introduction sequence.
/// </summary>
internal class SkipIntro
{
    public static void Activate(in PatchContext context)
    {
        var baseAddr = context.BaseAddress;
        if (!context.Config.P4GConfig.IntroSkip) 
            return;
        
        // Not a bug. Reused code.
        P3P.SkipIntro.P3PSkipIntroImpl(baseAddr, "48 89 5C 24 10 48 89 74 24 18 57 48 83 EC 40 48 8B F1 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5E 48", context);
    }
}