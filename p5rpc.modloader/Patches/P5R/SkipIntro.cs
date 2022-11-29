using p5rpc.modloader.Patches.Common;
using Reloaded.Memory.Sources;

namespace p5rpc.modloader.Patches.P5R;

/// <summary>
/// Skips the game introduction video.
/// </summary>
internal class SkipIntro
{
    public static void Activate(in PatchContext context)
    {
        var baseAddr = context.BaseAddress;
        if (!context.Config.P5RConfig.IntroSkip) 
            return;
        
        context.ScanHelper.FindPatternOffset("74 10 C7 07 0C 00 00 00", (offset) => 
            Memory.Instance.SafeWriteRaw((nuint)(baseAddr + offset), new byte[] { 0x90, 0x90 }),
            "Introduction Skip");
    }
}