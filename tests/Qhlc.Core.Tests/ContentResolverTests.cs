namespace Qhlc.Core.Tests;

public class ContentResolverTests
{
    private static string? AssetsRoot()
    {
        var root = Qhlc.Core.Nts.NtsPaths.ResolveAssetsRoot();
        return File.Exists(Path.Combine(root, "assets", "data", "Npc.Dat")) ? root : null;
    }

    [Fact]
    public void Resolve_returns_3_allies_and_2_enemies()
    {
        var root = AssetsRoot();
        if (root is null)
            return;

        var r = Qhlc.Core.Nts.ContentResolver.Resolve(root);
        Assert.Equal(3, r.AllyNpcIds.Count);
        Assert.Equal(2, r.EnemyNpcIds.Count);
        Assert.True(r.AreaId > 0);
        Assert.Equal(5, r.NpcIdToBlockId.Count);
        foreach (var id in r.AllyNpcIds.Concat(r.EnemyNpcIds))
            Assert.True(r.NpcIdToBlockId.ContainsKey(id));
    }
}
