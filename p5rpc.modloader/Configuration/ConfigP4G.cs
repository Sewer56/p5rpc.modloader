using System.ComponentModel;

namespace p5rpc.modloader.Configuration;

public class ConfigP4G
{
    [DisplayName("Render In Background")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;
}