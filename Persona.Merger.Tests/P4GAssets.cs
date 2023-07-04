namespace Persona.Merger.Tests;

public static class P4GAssets
{
    public static readonly string AssetsFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Assets", "P4G");
    public static readonly string TempFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Temp");

    public static readonly string ItemBefore = Path.Combine(AssetsFolder, "Item", "Before", "itemtbl.bin");
    public static readonly string ItemAfter = Path.Combine(AssetsFolder, "Item", "After", "itemtbl.bin");

    public static readonly string MessageOriginal = Path.Combine(AssetsFolder, "Message", "Original", "MSG.TBL");
    public static readonly string MessageEdited1 = Path.Combine(AssetsFolder, "Message", "Edited", "MSG1.TBL");
    public static readonly string MessageEdited2 = Path.Combine(AssetsFolder, "Message", "Edited", "MSG2.TBL");
    public static readonly string MessageMerged = Path.Combine(AssetsFolder, "Message", "Merged", "MSG.TBL");
}