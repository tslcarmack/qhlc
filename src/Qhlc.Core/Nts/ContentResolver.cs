using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Qhlc.Core.Nts.Tables;
using Qhlc.Core.Public;

namespace Qhlc.Core.Nts;

/// <summary>
/// Picks the first Scene + map tiles + party that satisfies vertical-slice rules (spec §4.4).
/// Faction: allies are <see cref="IsAllyFaction"/> (1–4); enemies are all other factions.
/// If that yields fewer than 3 allies or 2 enemies, falls back to the first 3 / next 2 Npc rows
/// (see <see cref="LastResolveWarning"/>).
/// </summary>
public static class ContentResolver
{
    public const int AllyCount = 3;
    public const int EnemyCount = 2;
    public const int MinWalkableCells = 50;

    private static readonly Regex MapArchivePattern = new(
        @"^map.*\.(jmg|bmg)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Set when faction pools were empty and positional fallback was used.</summary>
    public static string? LastResolveWarning { get; private set; }

    public static ResolvedContent Resolve(string assetsRoot)
    {
        if (string.IsNullOrWhiteSpace(assetsRoot))
            throw new ArgumentException("Assets root is required.", nameof(assetsRoot));

        assetsRoot = Path.GetFullPath(assetsRoot);
        LastResolveWarning = null;

        var sceneImages = IndexMapSceneJpegs(assetsRoot);
        var npcs = NpcTable.Load(assetsRoot);
        var scenes = SceneTable.Load(assetsRoot);

        foreach (var scene in scenes)
        {
            if (!TryValidateMap(assetsRoot, scene.AreaId, sceneImages, out var walkable))
                continue;

            if (walkable < MinWalkableCells)
                continue;

            if (!TryPickParty(assetsRoot, npcs, out var allies, out var enemies, out var blocks))
                continue;

            LogSelection(scene, allies, enemies, blocks);

            return new ResolvedContent(
                scene.AreaId,
                scene.Name,
                allies,
                enemies,
                blocks);
        }

        throw new InvalidOperationException(
            "无完整 Scene/Npc/jma 组合：未找到同时满足地图贴图、可走格与队伍合成的候选。");
    }

    /// <summary>Faction 1–4 are ally-eligible per slice spec (tsdocs Npc faction bands).</summary>
    public static bool IsAllyFaction(int faction) => faction is >= 1 and <= 4;

    private static bool TryValidateMap(
        string assetsRoot,
        int areaId,
        IReadOnlySet<int> sceneImages,
        out int walkable)
    {
        walkable = 0;
        NtsMap map;
        try
        {
            map = NtsMap.LoadFromGround(assetsRoot, areaId);
        }
        catch
        {
            return false;
        }

        foreach (var tile in map.Tiles)
        {
            if (!sceneImages.Contains(tile.Scene))
                return false;
        }

        for (var gx = 0; gx < map.Gw - 1; gx++)
        {
            for (var gy = 0; gy < map.Gh - 1; gy++)
            {
                if (map.Grid[gx * map.Gh + gy] == 0)
                    walkable++;
            }
        }

        return true;
    }

    private static bool TryPickParty(
        string assetsRoot,
        IReadOnlyList<NpcRecord> npcs,
        out IReadOnlyList<int> allyIds,
        out IReadOnlyList<int> enemyIds,
        out IReadOnlyDictionary<int, int> npcIdToBlockId)
    {
        allyIds = Array.Empty<int>();
        enemyIds = Array.Empty<int>();
        npcIdToBlockId = new Dictionary<int, int>();

        if (npcs.Count < AllyCount + EnemyCount)
            return false;

        var allies = new List<int>(AllyCount);
        var enemies = new List<int>(EnemyCount);

        foreach (var n in npcs)
        {
            if (allies.Count < AllyCount && IsAllyFaction(n.Faction))
                allies.Add(n.Id);
            else if (enemies.Count < EnemyCount && !IsAllyFaction(n.Faction))
                enemies.Add(n.Id);

            if (allies.Count >= AllyCount && enemies.Count >= EnemyCount)
                break;
        }

        if (allies.Count < AllyCount || enemies.Count < EnemyCount)
        {
            LastResolveWarning =
                "ContentResolver: faction pools insufficient; using first 3 Npc as allies and next 2 as enemies.";
            allies.Clear();
            enemies.Clear();
            for (var i = 0; i < AllyCount; i++)
                allies.Add(npcs[i].Id);
            for (var i = AllyCount; i < AllyCount + EnemyCount; i++)
                enemies.Add(npcs[i].Id);
        }

        var ordered = new List<int>(AllyCount + EnemyCount);
        ordered.AddRange(allies);
        ordered.AddRange(enemies);

        var blocks = new Dictionary<int, int>();
        var blockList = JmaWhitelist.Blocks;
        if (blockList.Length == 0)
            return false;

        for (var i = 0; i < ordered.Count; i++)
        {
            var blockId = blockList[i % blockList.Length];
            if (!NtsJma.TrySynthesizeFrame(assetsRoot, blockId, seq: 0, frameIndex: 0, out _))
                return false;

            blocks[ordered[i]] = blockId;
        }

        allyIds = allies;
        enemyIds = enemies;
        npcIdToBlockId = blocks;
        return true;
    }

    private static HashSet<int> IndexMapSceneJpegs(string assetsRoot)
    {
        var set = new HashSet<int>();
        var picDir = NtsPaths.PicDirectory(assetsRoot);
        if (!Directory.Exists(picDir))
            return set;

        foreach (var path in Directory.EnumerateFiles(picDir))
        {
            var fileName = Path.GetFileName(path);
            if (!MapArchivePattern.IsMatch(fileName))
                continue;

            foreach (var entry in EnumerateArchiveEntryNames(path))
            {
                if (!entry.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var stem = entry.AsSpan(0, entry.Length - 4);
                if (int.TryParse(stem, out var sceneId))
                    set.Add(sceneId);
            }
        }

        return set;
    }

    private static IEnumerable<string> EnumerateArchiveEntryNames(string archivePath)
    {
        var data = File.ReadAllBytes(archivePath);
        if (data.Length < 2)
            yield break;

        var count = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0, 2));
        if (count == 0)
            yield break;

        const int dirEntrySize = 32;
        var dirEnd = 2 + dirEntrySize * count;
        if (dirEnd > data.Length)
            yield break;

        for (var i = 0; i < count; i++)
        {
            var p = 2 + i * dirEntrySize;
            var nameLen = data[p];
            if (nameLen is < 1 or > 23)
                continue;

            yield return Encoding.Latin1.GetString(data, p + 1, nameLen);
        }
    }

    private static void LogSelection(
        SceneRecord scene,
        IReadOnlyList<int> allies,
        IReadOnlyList<int> enemies,
        IReadOnlyDictionary<int, int> blocks)
    {
        var allyText = string.Join(",", allies);
        var enemyText = string.Join(",", enemies);
        var blockText = string.Join(
            ",",
            blocks.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}->{kv.Value:D4}"));

        Console.WriteLine(
            $"ContentResolver: Scene={scene.Name} AreaId={scene.AreaId} allies=[{allyText}] enemies=[{enemyText}] blocks=[{blockText}]");

        if (LastResolveWarning is not null)
            Console.WriteLine(LastResolveWarning);
    }
}
