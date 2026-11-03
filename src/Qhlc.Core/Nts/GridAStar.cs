namespace Qhlc.Core.Nts;

/// <summary>
/// 4-connected A* on <see cref="NtsMap"/> walkable cells (column-major grid, 0 = walk).
/// </summary>
public static class GridAStar
{
    private static readonly (int Dx, int Dy)[] Neighbors =
    [
        (0, -1),
        (0, 1),
        (-1, 0),
        (1, 0),
    ];

    public static bool TryFindPath(
        NtsMap map,
        int startGx,
        int startGy,
        int goalGx,
        int goalGy,
        out List<(int Gx, int Gy)> path)
    {
        path = new List<(int Gx, int Gy)>();
        if (!map.IsCellWalkable(startGx, startGy) || !map.IsCellWalkable(goalGx, goalGy))
            return false;

        if (startGx == goalGx && startGy == goalGy)
        {
            path.Add((startGx, startGy));
            return true;
        }

        var open = new PriorityQueue<(int Gx, int Gy), int>();
        var closed = new HashSet<(int, int)>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var gScore = new Dictionary<(int, int), int> { [(startGx, startGy)] = 0 };

        open.Enqueue((startGx, startGy), Heuristic(startGx, startGy, goalGx, goalGy));

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (!closed.Add(current))
                continue;

            if (current.Gx == goalGx && current.Gy == goalGy)
            {
                Reconstruct(cameFrom, current, path);
                return true;
            }

            var currentG = gScore[current];
            foreach (var (dx, dy) in Neighbors)
            {
                var nx = current.Gx + dx;
                var ny = current.Gy + dy;
                var key = (nx, ny);
                if (closed.Contains(key) || !map.IsCellWalkable(nx, ny))
                    continue;

                var tentative = currentG + 1;
                if (gScore.TryGetValue(key, out var known) && tentative >= known)
                    continue;

                cameFrom[key] = current;
                gScore[key] = tentative;
                var f = tentative + Heuristic(nx, ny, goalGx, goalGy);
                open.Enqueue(key, f);
            }
        }

        return false;
    }

    /// <summary>
    /// If <paramref name="gx"/>/<paramref name="gy"/> is blocked, BFS to the nearest walkable cell.
    /// </summary>
    public static bool TryFindNearestWalkable(NtsMap map, int gx, int gy, out int walkGx, out int walkGy)
    {
        walkGx = walkGy = 0;
        if (map.IsCellWalkable(gx, gy))
        {
            walkGx = gx;
            walkGy = gy;
            return true;
        }

        var q = new Queue<(int Gx, int Gy)>();
        var seen = new HashSet<(int, int)> { (gx, gy) };
        q.Enqueue((gx, gy));
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            foreach (var (dx, dy) in Neighbors)
            {
                var nx = cx + dx;
                var ny = cy + dy;
                if (!seen.Add((nx, ny)))
                    continue;
                if (nx < 0 || ny < 0 || nx >= map.Gw - 1 || ny >= map.Gh - 1)
                    continue;
                if (map.IsCellWalkable(nx, ny))
                {
                    walkGx = nx;
                    walkGy = ny;
                    return true;
                }

                q.Enqueue((nx, ny));
            }
        }

        return false;
    }

    private static int Heuristic(int ax, int ay, int bx, int by) =>
        Math.Abs(ax - bx) + Math.Abs(ay - by);

    private static void Reconstruct(
        Dictionary<(int, int), (int, int)> cameFrom,
        (int Gx, int Gy) current,
        List<(int Gx, int Gy)> path)
    {
        path.Add(current);
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
    }
}
