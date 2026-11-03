namespace Qhlc.Core.Tests;

public class NtsGroundTests
{
    private static string? AssetsRoot()
    {
        var root = Qhlc.Core.Nts.NtsPaths.ResolveAssetsRoot();
        var data = Qhlc.Core.Nts.NtsPaths.DataDirectory(root);
        return File.Exists(Path.Combine(data, "Npc.Dat")) ? root : null;
    }

    [Fact]
    public void CanWalk_matches_column_major_rule()
    {
        var map = new Qhlc.Core.Nts.NtsMap(
            imgW: 40,
            imgH: 40,
            gw: 3,
            gh: 3,
            grid: new byte[] { 0, 1, 1, 0, 1, 1, 1, 1, 1 },
            tiles: Array.Empty<Qhlc.Core.Nts.NtsMapTile>());
        Assert.True(map.CanWalk(0, 0));
        Assert.False(map.CanWalk(0, 20));
        Assert.False(map.CanWalk(100, 0));
    }

    [Fact]
    public void LoadFromGround_area10001_has_walkable_cells()
    {
        var root = AssetsRoot();
        if (root is null)
            return;

        var groundPath = Path.Combine(Qhlc.Core.Nts.NtsPaths.DataDirectory(root), "Ground.MMG");
        if (!File.Exists(groundPath))
            return;

        var map = Qhlc.Core.Nts.NtsMap.LoadFromGround(root, 10001);
        Assert.True(map.Tiles.Count >= 1);
        Assert.True(map.ImgW > 0 && map.ImgH > 0);

        var walkable = 0;
        for (var gx = 0; gx < map.Gw - 1; gx++)
        {
            for (var gy = 0; gy < map.Gh - 1; gy++)
            {
                if (map.Grid[gx * map.Gh + gy] == 0)
                    walkable++;
            }
        }

        Assert.True(walkable > 0);
    }
}
