using Qhlc.Inventory.Public;

namespace Qhlc.Player.Public;

public abstract class Character
{
    private readonly IEquipCatalog _catalog;
    protected Character(
        int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
    {
        Id = id;
        Name = name;
        BodyBlockId = bodyBlockId;
        BaseStats = baseStats;
        _catalog = catalog;
        Loadout = new EquipmentLoadout();
    }

    public int Id { get; }
    public string Name { get; }
    public int BodyBlockId { get; }
    public CharacterStats BaseStats { get; protected set; }
    public EquipmentLoadout Loadout { get; }
    public int FootX { get; set; }
    public int FootY { get; set; }
    public int Facing { get; set; } = 4;
    public BehaviorState Behavior { get; set; } = BehaviorState.Idle;

    public bool TryEquip(int itemId, out string error)
    {
        error = "";
        if (!_catalog.TryGet(itemId, out var item))
        {
            error = $"unknown item {itemId}";
            return false;
        }
        Loadout.Set(item.Slot, item.Id);
        return true;
    }

    public CharacterStats EffectiveStats()
    {
        var s = BaseStats;
        var atk = s.Atk;
        var def = s.Def;
        var maxHp = s.MaxHp;
        foreach (var id in Loadout.Equipped.Values)
        {
            if (!_catalog.TryGet(id, out var it))
                continue;
            atk += it.AtkBonus;
            def += it.DefBonus;
            maxHp += it.HpBonus;
        }
        var hp = Math.Min(s.Hp + (maxHp - s.MaxHp), maxHp);
        return s with { Atk = atk, Def = def, MaxHp = maxHp, Hp = hp };
    }

    public AppearanceBlocks GetAppearanceBlocks()
    {
        var weapon = BodyBlockId;
        if (Loadout.TryGet(EquipmentSlot.Weapon, out var wid)
            && _catalog.TryGet(wid, out var w) && w.WeaponBlockId > 0)
            weapon = w.WeaponBlockId;
        return new AppearanceBlocks(BodyBlockId, weapon);
    }

    public virtual void TickBehavior(double dt) { }
}
