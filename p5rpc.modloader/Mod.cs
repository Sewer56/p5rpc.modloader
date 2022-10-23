using System.Diagnostics;
using System.Runtime.CompilerServices;
using p5rpc.modloader.Configuration;
using p5rpc.modloader.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

// Free perf gains, but you'll need to remember that any stackalloc isn't 0 initialized.
[module: SkipLocalsInit]

namespace p5rpc.modloader;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public unsafe class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private readonly MiscPatches _misc;
    private readonly CpkPatches _cpk;
    private CpkRedirectorBuilder _cpkBuilder;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        _modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
        var scan = new SigScan(_logger, startupScanner);

        _cpk = new CpkPatches(_hooks, _logger, _configuration, scan);
        _cpk.Activate();

        _misc = new MiscPatches(_hooks, _logger, _configuration, scan);
        _misc.Activate();
        
        // TODO: Hey Lipsum, I added the boilerplate for getting files from inside mods.
        // please wire this up with your rewritten binder code, thanks!
        _cpkBuilder = new CpkRedirectorBuilder(_modLoader, _logger);
        _modLoader.ModLoading += OnModLoading;
        _modLoader.OnModLoaderInitialized += OnLoaderInitialized;
    }

    private void OnModLoading(IModV1 arg1, IModConfigV1 arg2) => _cpkBuilder.Add((IModConfig)arg2);

    private void OnLoaderInitialized()
    {
        _modLoader.ModLoading -= OnModLoading;
        _cpkBuilder.Build(); // Return something that stores only the data we need here.
        _cpkBuilder = null;  // We don't need this anymore :)
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion Standard Overrides

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Mod()
    { }

#pragma warning restore CS8618

    #endregion For Exports, Serialization etc.
}