using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G;

namespace Persona.Merger.Tests.Parser;

public class P4GTblPatcherTests
{
    [Fact]
    public void PatchTbl_Item()
    {
        var original = File.ReadAllBytes(P4GAssets.ItemOriginal);
        var edited1 = File.ReadAllBytes(P4GAssets.ItemEdited1);
        var edited2 = File.ReadAllBytes(P4GAssets.ItemEdited2);
        var merged = File.ReadAllBytes(P4GAssets.ItemMerged);
        var patcher = new P4GTblPatcher(original, TblType.Item);
        var patch1 = patcher.GeneratePatch(edited1);
        var patch2 = patcher.GeneratePatch(edited2);
        var patched = patcher.Apply(new List<TblPatch>() { patch1, patch2 }, TblType.Item);
        File.WriteAllBytes("patched.bin", patched);
        Assert.Equal(merged, patched);
    }

    [Fact]
    public void PatchTbl_Message()
    {
        var original = File.ReadAllBytes(P4GAssets.MessageOriginal);
        var edited = File.ReadAllBytes(P4GAssets.MessageEdited1);
        var merged = File.ReadAllBytes(P4GAssets.MessageMerged);
        var patcher = new P4GTblPatcher(original, TblType.Message);
        List<TblPatch> patches = new();
        patches.Add(patcher.GeneratePatch(edited));

        edited = File.ReadAllBytes(P4GAssets.MessageEdited2);
        patches.Add(patcher.GeneratePatch(edited));

        var patched = patcher.Apply(patches, TblType.Message);
        File.WriteAllBytes("merged.tbl", patched);
        Assert.Equal(merged, patched);
    }

    [Fact]
    public void PatchTbl_AiCalcBf()
    {
        var original = File.ReadAllBytes(P4GAssets.AiCalcBefore);
        var after = File.ReadAllBytes(P4GAssets.AiCalcAfter);
        var patcher = new P4GTblPatcher(original, TblType.AiCalc);

        byte[][] bfs = new byte[11][];
        bfs[9] = File.ReadAllBytes(P4GAssets.AiCalcFriendBf);
        bfs[10] = File.ReadAllBytes(P4GAssets.AiCalcEnemyBf);
        
        var patched = patcher.Apply(new List<TblPatch>(), TblType.AiCalc, bfs);
        Assert.Equal(after, patched);
    }

}