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
}
