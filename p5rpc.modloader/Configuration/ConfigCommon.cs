using System.ComponentModel;
using FileEmulationFramework.Lib.Utilities;

namespace p5rpc.modloader.Configuration;

/// <summary>
/// Common configuiration options
/// </summary>
public class ConfigCommon
{
    [DisplayName("Log Level")]
    [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
    [DefaultValue(LogSeverity.Information)]
    public LogSeverity LogLevel { get; set; } = LogSeverity.Information;
}