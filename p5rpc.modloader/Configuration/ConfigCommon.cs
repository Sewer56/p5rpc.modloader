using System.ComponentModel;
using FileEmulationFramework.Lib.Utilities;

namespace p5rpc.modloader.Configuration;

/// <summary>
/// Common configuiration options
/// </summary>
public class ConfigCommon
{
    [DisplayName("Mod Support")]
    [DefaultValue(true)]
    public bool ModSupport { get; set; } = true;

    [DisplayName("Disable Bind Warnings(s)")]
    [Description("Disables warnings printed to the console as a result of CRI loading files from disk.")]
    [DefaultValue(true)]
    public bool DisableCriBindLogging { get; set; } = true;
    
    [DisplayName("Log Level")]
    [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
    [DefaultValue(LogSeverity.Information)]
    public LogSeverity LogLevel { get; set; } = LogSeverity.Information;
}