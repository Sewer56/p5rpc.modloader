using System.ComponentModel;
using FileEmulationFramework.Lib.Utilities;
using p5rpc.modloader.Template.Configuration;

namespace p5rpc.modloader.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Intro Skip")]
    [DefaultValue(false)]
    public bool IntroSkip { get; set; } = false;

    [DisplayName("Mod Support")]
    [DefaultValue(true)]
    public bool ModSupport { get; set; } = true;

    [DisplayName("Render In Background")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;
    
    [DisplayName("Disable Bind Warnings(s)")]
    [Description("Disables warnings printed to the console as a result of CRI loading files from disk.")]
    [DefaultValue(true)]
    public bool DisableCriBindLogging { get; set; } = true;
    
    [DisplayName("Log Level")]
    [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
    [DefaultValue(LogSeverity.Warning)]
    public LogSeverity LogLevel { get; set; } = LogSeverity.Warning;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    //
}