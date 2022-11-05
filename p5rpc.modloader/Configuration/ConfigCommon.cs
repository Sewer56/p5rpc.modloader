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
    
    [DisplayName("Print File Access")]
    [Description("Prints loaded file to console using the Info log level.")]
    [DefaultValue(false)]
    public bool PrintFileAccess { get; set; } = false;
    
    [DisplayName("Hot Reload")]
    [Description("Coming in the future :)")]
    [DefaultValue(false)]
    public bool HotReload { get; set; } = false;
    
    [DisplayName("Log Level")]
    [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
    [DefaultValue(LogSeverity.Information)]
    public LogSeverity LogLevel { get; set; } = LogSeverity.Information;
}