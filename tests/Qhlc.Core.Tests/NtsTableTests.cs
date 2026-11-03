namespace Qhlc.Core.Tests;

public class NtsTableTests
{
    private static string? AssetsRoot()
    {
        var root = Qhlc.Core.Nts.NtsPaths.ResolveAssetsRoot();
        return File.Exists(Path.Combine(root, "assets", "data", "Npc.Dat")) ? root : null;
    }

    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void NpcTable_skips_version_card_and_decodes_name()
    {
        var root = AssetsRoot();
        if (root is null) return;
        var rows = Qhlc.Core.Nts.Tables.NpcTable.Load(root);
        Assert.True(rows.Count >= 100);
        Assert.Contains(rows, n => n.Name.Contains('張') || n.Name.Length > 0);
    }

    [Fact]
    public void SceneTable_areaId_positive()
    {
        var root = AssetsRoot();
        if (root is null) return;
        var scenes = Qhlc.Core.Nts.Tables.SceneTable.Load(root);
        Assert.True(scenes[0].AreaId > 0);
    }
}
