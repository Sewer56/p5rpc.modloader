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
    
    [DisplayName("Persona 4 Golden (64-bit) Config")]
    public ConfigP4G P4GConfig { get; set; } = new();
    
    [DisplayName("Persona 3 Portable Config")]
    public ConfigP3P P3PConfig { get; set; } = new();

    /// <summary>
    /// Gets whether the game should render in background.
    /// </summary>
    public bool GetShouldRenderInBackground(Game game)
    {
        return game switch
        {
            Game.P4G => P4GConfig.RenderInBackground,
            Game.P5R => P5RConfig.RenderInBackground,
            Game.P3P => P3PConfig.RenderInBackground,
            _ => false
        };
    }
}