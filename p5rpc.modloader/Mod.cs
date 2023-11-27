using System.Diagnostics;
using System.Runtime.CompilerServices;
using BF.File.Emulator.Interfaces;
using BMD.File.Emulator.Interfaces;
using CriFs.V2.Hook.Interfaces;
using CriFsV2Lib.Definitions;
using FileEmulationFramework.Lib.Utilities;
using p5rpc.modloader.Patches.Common;
using p5rpc.modloader.Template;
using p5rpc.modloader.Utilities;
using PAK.Stream.Emulator.Interfaces;
using Persona.Merger.Cache;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

// Free perf gains, but you'll need to remember that any stackalloc isn't 0 initialized.
[module: SkipLocalsInit]

namespace p5rpc.modloader;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public partial class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to this mod's common configuration.
    /// </summary>
    public static Config Configuration = null!;

    /// <summary>
    /// Assume Persona 5 Royal unless otherwise.
    /// </summary>
    public static Game Game = Game.P5R;
    
    /// <summary>
    /// Current process.
    /// </summary>
    public static Process CurrentProcess = null!;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private readonly Logger _logger;
    private ICriFsRedirectorApi _criFsApi = null!;
    private IPakEmulator _pakEmulator = null!;
    private IBfEmulator _bfEmulator = null!;
    private IBmdEmulator _bmdEmulator = null!;
    private MergedFileCache _mergedFileCache = null!;
    private Task _createMergedFileCacheTask = null!;
    
    public Mod(ModContext context)
    {
        var modLoader = context.ModLoader;
        IReloadedHooks? hooks = context.Hooks;
        Configuration = context.Configuration;
        _logger = new Logger(context.Logger, Configuration.Common.LogLevel);
        _modConfig = context.ModConfig;

        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.
        modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
        var scanHelper = new SigScanHelper(_logger, startupScanner);
        CurrentProcess = Process.GetCurrentProcess();
        var mainModule = CurrentProcess.MainModule;
        var baseAddr = mainModule!.BaseAddress;
        
        var patchContext = new PatchContext()
        {
            BaseAddress = baseAddr,
            Config = Configuration,
            Logger = _logger,
            Hooks = hooks!,
            ScanHelper = scanHelper
        };
        
        // Game Specific Patches
        var fileName = Path.GetFileName(mainModule.FileName);
        if (fileName.StartsWith("p5r", StringComparison.OrdinalIgnoreCase))
            Game = Game.P5R;
        else if (fileName.StartsWith("p4g", StringComparison.OrdinalIgnoreCase))
            Game = Game.P4G;        
        else if (fileName.StartsWith("p3p", StringComparison.OrdinalIgnoreCase))
            Game = Game.P3P;
        else
            _logger.Warning("Executable name does not match any known game. Will use Persona 5 Royal profile.\n" +
                            "Consider renaming your EXE back to something that starts with 'p4g' or 'p5r'.");

        // Read merged file cache in background.
        _createMergedFileCacheTask = Task.Run(async () =>
        {
            var modFolder = modLoader.GetDirectoryForModId(context.ModConfig.ModId);
            var cacheFolder = Path.Combine(modFolder, "Cache", $"{Game}");
            return _mergedFileCache = await MergedFileCache.FromPathAsync(context.ModConfig.ModVersion, cacheFolder);
        });
        
        modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out _criFsApi!);
        modLoader.GetController<IPakEmulator>().TryGetTarget(out _pakEmulator!);
        modLoader.GetController<IBfEmulator>().TryGetTarget(out _bfEmulator!);
        modLoader.GetController<IBmdEmulator>().TryGetTarget(out _bmdEmulator!);
        _criFsApi.AddBindCallback(OnBind);
        
        if (Game == Game.P5R)
        {
            Patches.P5R.SkipIntro.Activate(patchContext);
            var criLib = _criFsApi.GetCriFsLib();
            criLib.SetDefaultEncryptionFunction(criLib.GetKnownDecryptionFunction(KnownDecryptionFunction.P5R)!);
        }
        else if (Game == Game.P4G)
        {
            Patches.P4G.SkipIntro.Activate(patchContext);
        }
        else if (Game == Game.P3P)
        {
            Patches.P3P.SkipIntro.Activate(patchContext);
            _criFsApi.SetMaxFilesMultiplier(6); // P3P uses 3 binders.
        }
        
        // Common Patches
        NoPauseOnFocusLoss.Activate(patchContext);
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        Configuration = configuration;
        _logger.LogLevel = Configuration.Common.LogLevel;
        _logger.Info($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion Standard Overrides

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Mod()
    { }

#pragma warning restore CS8618

    #endregion For Exports, Serialization etc.
}

/// <summary>
/// The game we're currently running.
/// </summary>
public enum Game
{
    P4G,
    P5R,
    P3P
}