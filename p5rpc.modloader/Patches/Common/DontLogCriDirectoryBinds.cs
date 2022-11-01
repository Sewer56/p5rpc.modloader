using Reloaded.Memory.Sources;

namespace p5rpc.modloader.Patches.Common;

/// <summary>
/// Disables logging of files loaded in from directory binds.
/// </summary>
internal class DontLogCriDirectoryBinds
{
    public static void Activate(in PatchContext context)
    {
        var baseAddr = context.BaseAddress;
        if (!context.Config.Common.DisableCriBindLogging) 
            return;
        
        context.ScanHelper.FindPatternOffset("48 8B FA 75 7E", (offset) =>
            {
                var nopJmpOne = (nuint)((nint)baseAddr + offset + 3);
                var nopJmpTwo = nopJmpOne + 10;
                var nopJmp = new byte[] { 0x90, 0x90 };
                Memory.Instance.SafeWriteRaw(nopJmpOne, nopJmp);
                Memory.Instance.SafeWriteRaw(nopJmpTwo, nopJmp);
            }, "Disable CRI Bind Logging");
    }
}