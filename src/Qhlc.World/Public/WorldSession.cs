using Qhlc.Core.Nts;
using Qhlc.Core.Public;

namespace Qhlc.World.Public;

/// <summary>Map roam session: collision context, enemy placements, encounter gate.</summary>
public sealed class WorldSession
{
    public const int EnemyHitboxPixels = NtsMap.CellPixels;

    public ResolvedContent Content { get; }
    public NtsMap Map { get; }
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public IReadOnlyList<(int X, int Y)> EnemyPositions { get; }

    private bool _encounterConsumed;

    public WorldSession(
        ResolvedContent content,
        NtsMap map,
        int playerX,
        int playerY,
        IReadOnlyList<(int X, int Y)> enemyPositions)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Map = map ?? throw new ArgumentNullException(nameof(map));
        EnemyPositions = enemyPositions ?? throw new ArgumentNullException(nameof(enemyPositions));
        PlayerX = playerX;
        PlayerY = playerY;
    }

    /// <param name="playerRect">Player AABB in map pixels (x, y, width, height).</param>
    public bool TryEncounter((int X, int Y, int W, int H) playerRect)
    {
        if (_encounterConsumed)
            return false;

        foreach (var (ex, ey) in EnemyPositions)
        {
            if (RectsOverlap(
                    playerRect.X,
                    playerRect.Y,
                    playerRect.W,
                    playerRect.H,
                    ex,
                    ey,
                    EnemyHitboxPixels,
                    EnemyHitboxPixels))
                return true;
        }

        return false;
    }

    public void MarkEncounterConsumed() => _encounterConsumed = true;

    private static bool RectsOverlap(
        int ax,
        int ay,
        int aw,
        int ah,
        int bx,
        int by,
        int bw,
        int bh)
    {
        return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
    }
}
