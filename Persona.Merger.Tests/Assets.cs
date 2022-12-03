namespace Persona.Merger.Tests;

public static class Assets
{
    public static readonly string AssetsFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Assets");
    public static readonly string TempFolder   = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Temp");
    
    public static readonly string SkillBefore  = Path.Combine(AssetsFolder, "Skill", "Before", "SKILL.TBL");
    public static readonly string SkillAfter   = Path.Combine(AssetsFolder, "Skill", "After", "SKILL.TBL");
    public static readonly string SkillExtend  = Path.Combine(AssetsFolder, "Skill", "Extend", "SKILL.TBL"); // Miku?
}