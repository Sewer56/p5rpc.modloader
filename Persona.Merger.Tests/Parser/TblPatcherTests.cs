using Persona.Merger.Patching.Tbl;

namespace Persona.Merger.Tests.Parser;

public class TblPatcherTests
{
    [Fact]
    public void PatchTbl_Skill()
    {
        var original = File.ReadAllBytes(Assets.SkillBefore);
        var after = File.ReadAllBytes(Assets.SkillAfter);
        var patcher = new TblPatcher(original, TblType.Skill);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch });
        Assert.Equal(after, patched);
    }
}