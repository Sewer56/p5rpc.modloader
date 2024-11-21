namespace Persona.Merger.Tests;

public static class P5RAssets
{
    public static readonly string AssetsFolder = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Assets", "P5R");
    public static readonly string TempFolder   = Path.Combine(Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!, "Temp");
    
    public static readonly string UnitBefore = Path.Combine(AssetsFolder, "Unit", "Before", "UNIT.TBL");
    public static readonly string UnitAfter  = Path.Combine(AssetsFolder, "Unit", "After", "UNIT.TBL");
    
    public static readonly string NameBefore = Path.Combine(AssetsFolder, "Name", "Before", "NAME.TBL");
    public static readonly string NameAfter  = Path.Combine(AssetsFolder, "Name", "After", "NAME.TBL");
    public static readonly string NameAfter2 = Path.Combine(AssetsFolder, "Name", "After2", "NAME.TBL");
    
    public static readonly string SkillBefore  = Path.Combine(AssetsFolder, "Skill", "Before", "SKILL.TBL");
    public static readonly string SkillAfter   = Path.Combine(AssetsFolder, "Skill", "After", "SKILL.TBL");
    public static readonly string SkillExtend  = Path.Combine(AssetsFolder, "Skill", "Extend", "SKILL.TBL"); // Miku?
    
    public static readonly string ItemBefore  = Path.Combine(AssetsFolder, "Item", "Before", "ITEM.TBL");
    public static readonly string ItemAfter   = Path.Combine(AssetsFolder, "Item", "After", "ITEM.TBL");
    public static readonly string ItemExtend  = Path.Combine(AssetsFolder, "Item", "Extend", "ITEM.TBL"); // Miku?
    
    public static readonly string VisualBefore = Path.Combine(AssetsFolder, "Visual", "Before", "VISUAL.TBL");
    public static readonly string VisualAfter = Path.Combine(AssetsFolder, "Visual", "After", "VISUAL.TBL");
    public static readonly string VisualAfter2 = Path.Combine(AssetsFolder, "Visual", "After2", "VISUAL.TBL");
    public static readonly string VisualExpected = Path.Combine(AssetsFolder, "Visual", "Expected", "VISUAL.TBL");

    public static readonly string PDDBefore = Path.Combine(AssetsFolder, "PDD", "Before", "SHDPERSONAENEMY.PDD");
    public static readonly string PDDAfter = Path.Combine(AssetsFolder, "PDD", "After", "SHDPERSONAENEMY.PDD");
    public static readonly string PDDAfter2 = Path.Combine(AssetsFolder, "PDD", "After2", "SHDPERSONAENEMY.PDD");
    public static readonly string PDDExpected = Path.Combine(AssetsFolder, "PDD", "Expected", "SHDPERSONAENEMY.PDD");
}