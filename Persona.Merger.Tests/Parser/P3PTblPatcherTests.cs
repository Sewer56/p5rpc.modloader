using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P3P;

namespace Persona.Merger.Tests.Parser;
public class P3PTblPatcherTests
{
    [Fact]
    public void PatchTbl_Item()
    {
        var original = File.ReadAllBytes(P3PAssets.ItemBefore);
        var after = File.ReadAllBytes(P3PAssets.ItemAfter);
        var patcher = new P3PTblPatcher(original, TblType.Item);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch }, TblType.Item);
        File.WriteAllBytes("patched.bin", patched);
        Assert.Equal(after, patched);
    }

    [Fact]
    public void PatchTbl_AiCalcBf()
    {
        var original = File.ReadAllBytes(P3PAssets.AiCalcBefore);
        var after = File.ReadAllBytes(P3PAssets.AiCalcAfter);
        var patcher = new P3PTblPatcher(original, TblType.AiCalc);

        byte[][] bfs = new byte[18][];
        bfs[16] = File.ReadAllBytes(P3PAssets.AiCalcFriendBf);
        bfs[17] = File.ReadAllBytes(P3PAssets.AiCalcEnemyBf);

        var patched = patcher.Apply(new List<TblPatch>(), TblType.AiCalc, bfs);
        Assert.Equal(after, patched);
    }
}
