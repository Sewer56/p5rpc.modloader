using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;
using Persona.Merger.Patching.Tbl.FieldResolvers.Generic;

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
    
    [Fact]
    public void PatchTbl_Generic()
    {
        var patches = new List<TblPatch>();

        var original = File.ReadAllBytes(P5RAssets.PDDBefore);
        var after = File.ReadAllBytes(P5RAssets.PDDAfter);
        var after2 = File.ReadAllBytes(P5RAssets.PDDAfter2);
        var expected = File.ReadAllBytes(P5RAssets.PDDExpected);
        var patcher = new GenericPatcher(original);
        patches.Add(patcher.GeneratePatchGeneric(after, 4));
        patches.Add(patcher.GeneratePatchGeneric(after2, 4));
        var patched = patcher.ApplyGeneric(patches);

        Assert.Equal(patched, expected);
    }
    
    [Fact]
    public void PatchTbl_VISUAL()
    {
        var patches = new List<TblPatch>();

        var original = File.ReadAllBytes(P5RAssets.VisualBefore);
        var after = File.ReadAllBytes(P5RAssets.VisualAfter);
        var after2 = File.ReadAllBytes(P5RAssets.VisualAfter2);
        var expected = File.ReadAllBytes(P5RAssets.VisualExpected);
        var patcher = new P5RTblPatcher(original, TblType.Visual);
        patches.Add(patcher.GeneratePatch(after));
        patches.Add(patcher.GeneratePatch(after2));
        var patched = patcher.Apply(patches);

        Assert.Equal(patched, expected);
    }
}