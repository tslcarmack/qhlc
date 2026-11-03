using Qhlc.Core.Nts;

namespace Qhlc.Core.Tests;

public class GridAStarTests
{
    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void FindPath_goes_around_wall()
    {
        // 5x5 cell points → usable 4x4 cells. Wall blocks direct right from (0,1) to (3,1).
        // Grid column-major: index = gx * gh + gy, gh = 5
        var gh = 5;
        var gw = 5;
        var grid = new byte[gw * gh];
        for (var i = 0; i < grid.Length; i++)
            grid[i] = 0;

        // Vertical wall at gx=2 for gy=0..2
        for (var gy = 0; gy <= 2; gy++)
            grid[2 * gh + gy] = 1;

        var map = new NtsMap(80, 80, gw, gh, grid, Array.Empty<NtsMapTile>());
        Assert.True(GridAStar.TryFindPath(map, 0, 1, 3, 1, out var path));
        Assert.True(path.Count >= 4);
        Assert.Equal((0, 1), path[0]);
        Assert.Equal((3, 1), path[^1]);
        Assert.DoesNotContain(path, c => c.Gx == 2 && c.Gy <= 2);
    }

    [Fact]
    public void FindNearestWalkable_from_blocked_cell()
    {
        var gh = 3;
        var gw = 3;
        var grid = new byte[gw * gh];
        grid[0 * gh + 0] = 1; // blocked
        grid[1 * gh + 0] = 0;
        var map = new NtsMap(40, 40, gw, gh, grid, Array.Empty<NtsMapTile>());
        Assert.True(GridAStar.TryFindNearestWalkable(map, 0, 0, out var gx, out var gy));
        Assert.True(map.IsCellWalkable(gx, gy));
    }
}
