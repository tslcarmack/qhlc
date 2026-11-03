using Qhlc.Core.Nts;

namespace Qhlc.Player.Public;

/// <summary>
/// Foot-pixel movement with axis-separated wall sliding (spec §5.1).
/// </summary>
public static class PlayerMover
{
    public static (int X, int Y) TryMove(NtsMap map, int x, int y, int dx, int dy)
    {
        var nextX = x;
        var nextY = y;

        if (dx != 0 && map.CanWalk(x + dx, y))
            nextX = x + dx;

        if (dy != 0 && map.CanWalk(nextX, y + dy))
            nextY = y + dy;

        return (nextX, nextY);
    }
}
