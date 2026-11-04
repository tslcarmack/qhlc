namespace Qhlc.Inventory.Public;

public interface IEquipCatalog
{
    IReadOnlyList<EquipmentItem> All { get; }
    bool TryGet(int itemId, out EquipmentItem item);
    EquipmentItem? DefaultWeaponForNpc(int npcId);
}
