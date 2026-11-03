using Qhlc.Core.Nts;
using Qhlc.Player.Public;

namespace Qhlc.Core.Tests;

public class PlayerMoverTests
{
    private static NtsMap SampleMap() =>
        new(
            imgW: 60,
            imgH: 60,
            gw: 3,
            gh: 3,
            grid: new byte[] { 0, 1, 1, 0, 1, 1, 1, 1, 1 },
            tiles: Array.Empty<NtsMapTile>());

    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void TryMove_blocked_axis_unchanged()
    {
        var map = SampleMap();
        var (x, y) = PlayerMover.TryMove(map, 0, 0, 0, 20);
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void TryMove_diagonal_slides_on_walkable_axis_only()
    {
        var map = SampleMap();
        var (x, y) = PlayerMover.TryMove(map, 0, 0, 20, 20);
        Assert.Equal(20, x);
        Assert.Equal(0, y);
    }
}
