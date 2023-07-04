using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G;

namespace Persona.Merger.Tests.Parser;

public class P4GTblPatcherTests
{
    [Fact]
    public void PatchTbl_Item()
    {
        var original = File.ReadAllBytes(P4GAssets.ItemBefore);
        var after = File.ReadAllBytes(P4GAssets.ItemAfter);
        var patcher = new P4GTblPatcher(original, TblType.Item);
        var patch = patcher.GeneratePatch(after);
        var patched = patcher.Apply(new List<TblPatch>() { patch }, TblType.Item);
        Assert.Equal(after, patched);
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
}