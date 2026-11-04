using Qhlc.Core.Nts;

namespace Qhlc.Player.Public;

/// <summary>
/// Foot-pixel movement with 8-way steps and wall sliding (spec §5.1 / §03.4 facing).
/// </summary>
public static class PlayerMover
{
    public static (int X, int Y) TryMove(NtsMap map, int x, int y, int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return (x, y);

        // Prefer true diagonal when both axes requested and the diagonal cell is free
        // (same no-corner-cut rule as GridAStar).
        if (dx != 0 && dy != 0)
        {
            var sx = Math.Sign(dx);
            var sy = Math.Sign(dy);
            var diagX = x + dx;
            var diagY = y + dy;
            if (map.CanWalk(diagX, y) && map.CanWalk(x, diagY) && map.CanWalk(diagX, diagY))
                return (diagX, diagY);

            // Slide on one axis if diagonal blocked.
            if (map.CanWalk(x + dx, y))
                return (x + dx, y);
            if (map.CanWalk(x, y + dy))
                return (x, y + dy);
            return (x, y);
        }

        var nextX = x;
        var nextY = y;

        if (dx != 0 && map.CanWalk(x + dx, y))
            nextX = x + dx;

        if (dy != 0 && map.CanWalk(nextX, y + dy))
            nextY = y + dy;

        return (nextX, nextY);
    }
}
