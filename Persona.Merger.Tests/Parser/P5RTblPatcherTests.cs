using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;

namespace Persona.Merger.Tests.Parser;

public class P5RTblPatcherTests
{
    [Fact]
    public void PatchTbl_Skill()
    {
        var original = File.ReadAllBytes(P5RAssets.SkillBefore);
        var after = File.ReadAllBytes(P5RAssets.SkillAfter);
        var patcher = new P5RTblPatcher(original, TblType.Skill);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch });
        Assert.Equal(after, patched);
    }
    
    [Fact]
    public void PatchTbl_Unit()
    {
        var original = File.ReadAllBytes(P5RAssets.UnitBefore);
        var after = File.ReadAllBytes(P5RAssets.UnitAfter);
        var patcher = new P5RTblPatcher(original, TblType.Unit);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch });
        Assert.Equal(after, patched);
    }
    
    [Fact]
    public void PatchTbl_Item_Extend()
    {
        var original = File.ReadAllBytes(P5RAssets.ItemBefore);
        var after = File.ReadAllBytes(P5RAssets.ItemExtend);
        var patcher = new P5RTblPatcher(original, TblType.Item);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch });
        Assert.Equal(after, patched);
    }
}