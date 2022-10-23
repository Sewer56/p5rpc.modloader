using System.ComponentModel;
using p5rpc.stuff.Template.Configuration;

namespace p5rpc.stuff.Configuration;

public class Config : Configurable<Config>
{
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
    [DisplayName("Render In Background")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    //
}