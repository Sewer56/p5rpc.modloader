using System.ComponentModel;
using p5rpc.stuff.Template.Configuration;

namespace p5rpc.stuff.Configuration;

public class Config : Configurable<Config>
{
    public enum BattleBgmOptions
    {
        Normal,
        InOrder,
        Random,
    }

    [Category("Test")]
    [DisplayName("Intro Skip")]
    [DefaultValue(false)]
    public bool IntroSkip { get; set; } = false;

    [Category("Test")]
    [DisplayName("Mod Support")]
    [DefaultValue(false)]
    public bool ModSupport { get; set; } = false;

    [Category("Test")]
    [DisplayName("Mods")]
    public List<string> BindMods { get; set; } = new()
    { 
        @".\CPK\BIND\",
        @".\CPK\BIND1\",
        @".\CPK\BIND2\",
        @".\CPK\BIND3\",
        @".\CPK\MOD.CPK",
        @".\CPK\MOD1.CPK",
        @".\CPK\MOD2.CPK",
        @".\CPK\MOD3.CPK",
    };

    [Category("Test")]
    [DisplayName("Disable Achievements")]
    [DefaultValue(false)]
    public bool NoTrophy { get; set; } = false;

    [Category("Test")]
    [DisplayName("Render In Background")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;

    [Category("Test")]
    [DisplayName("Battle BGM")]
    [DefaultValue(BattleBgmOptions.Normal)]
    public BattleBgmOptions BattleBgm { get; set; } = BattleBgmOptions.Normal;

    [Category("Test")]
    [DisplayName("Cutscene Outfits")]
    [DefaultValue(false)]
    public bool CutsceneOutfits { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    //
}