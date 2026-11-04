using Qhlc.Core.Public;
using Qhlc.Inventory.Public;

namespace Qhlc.Player.Public;

public static class CharacterFactory
{
    /// <summary>Slice fallback when no NpcTable stats resolver is supplied (source: stub).</summary>
    private static CharacterStats StubStats(int npcId) =>
        new(MaxHp: 2000, Hp: 2000, MaxSp: 500, Sp: 500, Atk: 100, Def: 50, Intel: 10, Agi: 10, Element: 0);

    public static Party CreateParty(
        ResolvedContent content,
        IEquipCatalog catalog,
        Func<int, CharacterStats?>? resolveStats = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(catalog);

        if (content.AllyNpcIds.Count == 0)
            throw new InvalidOperationException("ResolvedContent has no ally NpcIds.");

        resolveStats ??= _ => null;

        var leaderId = content.AllyNpcIds[0];
        var leader = BuildPlayer(leaderId, content, catalog, resolveStats);

        var allies = new List<Character>();
        for (var i = 1; i < content.AllyNpcIds.Count; i++)
        {
            var id = content.AllyNpcIds[i];
            allies.Add(BuildNpc(id, content, catalog, resolveStats));
        }

        return new Party(leader, allies);
    }

    public static List<NpcCharacter> CreateMapNpcs(
        ResolvedContent content,
        IEquipCatalog catalog,
        IReadOnlyList<(int X, int Y)> positions,
        Func<int, CharacterStats?>? resolveStats = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(positions);

        resolveStats ??= _ => null;

        var list = new List<NpcCharacter>(positions.Count);
        for (var i = 0; i < positions.Count; i++)
        {
            var enemyId = i < content.EnemyNpcIds.Count
                ? content.EnemyNpcIds[i]
                : content.EnemyNpcIds[^1];
            var npc = BuildNpc(enemyId, content, catalog, resolveStats);
            var (x, y) = positions[i];
            npc.FootX = x;
            npc.FootY = y;
            list.Add(npc);
        }

        return list;
    }

    private static PlayerCharacter BuildPlayer(
        int npcId,
        ResolvedContent content,
        IEquipCatalog catalog,
        Func<int, CharacterStats?> resolveStats)
    {
        var blockId = ResolveBlockId(content, npcId);
        var stats = resolveStats(npcId) ?? StubStats(npcId);
        var hero = new PlayerCharacter(npcId, $"Npc-{npcId}", blockId, stats, catalog);
        EquipDefaultWeapon(hero, catalog, npcId);
        return hero;
    }

    private static NpcCharacter BuildNpc(
        int npcId,
        ResolvedContent content,
        IEquipCatalog catalog,
        Func<int, CharacterStats?> resolveStats)
    {
        var blockId = ResolveBlockId(content, npcId);
        var stats = resolveStats(npcId) ?? StubStats(npcId);
        var npc = new NpcCharacter(npcId, $"Npc-{npcId}", blockId, stats, catalog);
        EquipDefaultWeapon(npc, catalog, npcId);
        return npc;
    }

    private static int ResolveBlockId(ResolvedContent content, int npcId) =>
        content.NpcIdToBlockId.TryGetValue(npcId, out var blockId) ? blockId : 2000;

    private static void EquipDefaultWeapon(Character character, IEquipCatalog catalog, int npcId)
    {
        var weapon = catalog.DefaultWeaponForNpc(npcId);
        if (weapon is not null)
            character.TryEquip(weapon.Id, out _);
    }
}
