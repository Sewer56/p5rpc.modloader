using System.ComponentModel;
using p5rpc.modloader.Configuration;
using p5rpc.modloader.Template.Configuration;

namespace p5rpc.modloader;

/// <summary>
/// Stores mod configurations.
/// </summary>
public class Config : Configurable<Config>
{
    [DisplayName("Common Config")]
    public ConfigCommon Common { get; set; } = new();
    
    [DisplayName("Persona 5 Royal Config")]
    public ConfigP5R P5RConfig { get; set; } = new();
}