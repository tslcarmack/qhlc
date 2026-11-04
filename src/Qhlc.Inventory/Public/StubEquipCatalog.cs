namespace Qhlc.Inventory.Public;

/// <summary>切片占位目录（source: stub）。</summary>
public sealed class StubEquipCatalog : IEquipCatalog
{
    public const int TrialSwordId = 9001;
    public const int DefaultSwordId = 9000;

    private readonly Dictionary<int, EquipmentItem> _items;

    public StubEquipCatalog()
    {
        var list = new[]
        {
            new EquipmentItem(DefaultSwordId, "Stub Blade", EquipmentSlot.Weapon, WeaponBlockId: 2000, AtkBonus: 5, DefBonus: 0, HpBonus: 0),
            new EquipmentItem(TrialSwordId, "Trial Fang", EquipmentSlot.Weapon, WeaponBlockId: 2000, AtkBonus: 25, DefBonus: 0, HpBonus: 0),
            new EquipmentItem(9100, "Stub Coat", EquipmentSlot.Body, WeaponBlockId: 0, AtkBonus: 0, DefBonus: 8, HpBonus: 20),
        };
        _items = list.ToDictionary(i => i.Id);
        All = list;
    }

    public IReadOnlyList<EquipmentItem> All { get; }

    public bool TryGet(int itemId, out EquipmentItem item) => _items.TryGetValue(itemId, out item!);

    public EquipmentItem? DefaultWeaponForNpc(int npcId) =>
        _items.TryGetValue(DefaultSwordId, out var w) ? w : null;
}
