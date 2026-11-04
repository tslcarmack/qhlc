using Qhlc.Core.Nts;
using Qhlc.Core.Public;
using Qhlc.Player.Public;

namespace Qhlc.World.Public;

/// <summary>Map roam session: collision context, character party, map NPCs, encounter gate.</summary>
public sealed class WorldSession
{
    public const int EnemyHitboxPixels = NtsMap.CellPixels;

    public ResolvedContent Content { get; }
    public NtsMap Map { get; }
    public PlayerCharacter Player { get; }
    public Party Party { get; }
    public IReadOnlyList<NpcCharacter> MapNpcs { get; }

    /// <summary>Player foot X; mirrors <see cref="Player"/>.</summary>
    public int PlayerX
    {
        get => Player.FootX;
        set => Player.FootX = value;
    }

    /// <summary>Player foot Y; mirrors <see cref="Player"/>.</summary>
    public int PlayerY
    {
        get => Player.FootY;
        set => Player.FootY = value;
    }

    /// <summary>Map enemy foot positions; mirrors <see cref="MapNpcs"/>.</summary>
    public IReadOnlyList<(int X, int Y)> EnemyPositions { get; }

    private bool _encounterConsumed;

    public WorldSession(
        ResolvedContent content,
        NtsMap map,
        PlayerCharacter player,
        Party party,
        IReadOnlyList<NpcCharacter> mapNpcs)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Party = party ?? throw new ArgumentNullException(nameof(party));
        MapNpcs = mapNpcs ?? throw new ArgumentNullException(nameof(mapNpcs));
        EnemyPositions = mapNpcs.Select(n => (n.FootX, n.FootY)).ToList();
    }

    /// <summary>Player-centered AABB vs map NPC foot hitboxes (20×20 px).</summary>
    public bool TryEncounter()
    {
        var half = EnemyHitboxPixels / 2;
        return TryEncounter((Player.FootX - half, Player.FootY - half, EnemyHitboxPixels, EnemyHitboxPixels));
    }

    /// <param name="playerRect">Player AABB in map pixels (x, y, width, height).</param>
    public bool TryEncounter((int X, int Y, int W, int H) playerRect)
    {
        if (_encounterConsumed)
            return false;

        foreach (var npc in MapNpcs)
        {
            if (RectsOverlap(
                    playerRect.X,
                    playerRect.Y,
                    playerRect.W,
                    playerRect.H,
                    npc.FootX,
                    npc.FootY,
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
