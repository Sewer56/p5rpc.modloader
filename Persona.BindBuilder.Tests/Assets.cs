namespace Persona.BindBuilder.Tests;

public class Assets
{
    public static readonly string AssetsFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath), "Assets");
    public static readonly string TempFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath), "Temp");

    public static readonly string ButtonPromptsMod1 = Path.Combine(AssetsFolder, "ButtonPromptsMod1");
    public static readonly string ButtonPromptsMod1Cpk = Path.Combine(ButtonPromptsMod1, "EN.CPK");
    public static readonly string ButtonPromptsMod2 = Path.Combine(AssetsFolder, "ButtonPromptsMod2");
    public static readonly string ButtonPromptsMod2Cpk = Path.Combine(ButtonPromptsMod2, "EN.CPK");
    public static readonly string ModelMod = Path.Combine(AssetsFolder, "ModelMod");
    public static readonly string ModelModCpk = Path.Combine(AssetsFolder, "BASE.CPK");
}