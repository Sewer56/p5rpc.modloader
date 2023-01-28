using System.Diagnostics;
using System.Runtime.CompilerServices;
using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using p5r.modloader.pak.Template;
using PAK.Stream.Emulator.Interfaces;
using Persona.Merger.Cache;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

// Free perf gains, but you'll need to remember that any stackalloc isn't 0 initialized.
[module: SkipLocalsInit]

namespace p5r.modloader.pak;

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
    /// Current process.
    /// </summary>
    public static Process CurrentProcess = null!;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private readonly Logger _logger;
    private readonly IPakEmulator _pakEmulator = null!;
    private ICriFsRedirectorApi _criFsApi = null!;
    private MergedFileCache _mergedFileCache = null!;
    private Task _createMergedFileCacheTask = null!;

    public Mod(ModContext context)
    {
        var modLoader = context.ModLoader;
        IReloadedHooks? hooks = context.Hooks;
        Configuration = context.Configuration;
        _logger = new Logger(context.Logger, Configuration.LogLevel);
        _modConfig = context.ModConfig;

        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        // Read merged file cache in background.
        _createMergedFileCacheTask = Task.Run(async () =>
        {
            var modFolder = modLoader.GetDirectoryForModId(context.ModConfig.ModId);
            var cacheFolder = Path.Combine(modFolder, "Cache");
            return _mergedFileCache = await MergedFileCache.FromPathAsync(context.ModConfig.ModVersion, cacheFolder);
        });

        modLoader.GetController<IPakEmulator>().TryGetTarget(out _pakEmulator!);
        modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out _criFsApi!);
        _criFsApi!.AddBindCallback(OnBind);
    }
    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        Configuration = configuration;
        _logger.LogLevel = Configuration.LogLevel;
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