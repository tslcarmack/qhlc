namespace Qhlc.Inventory.Public;

/// <summary>装配物定义。Stub 数值非原版表（source: stub）。</summary>
public sealed record EquipmentItem(
    int Id,
    string Name,
    EquipmentSlot Slot,
    int WeaponBlockId,
    int AtkBonus,
    int DefBonus,
    int HpBonus);
