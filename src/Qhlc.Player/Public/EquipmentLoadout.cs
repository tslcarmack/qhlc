using Qhlc.Inventory.Public;

namespace Qhlc.Player.Public;

public sealed class EquipmentLoadout
{
    private readonly Dictionary<EquipmentSlot, int> _equipped = new();
    public IReadOnlyDictionary<EquipmentSlot, int> Equipped => _equipped;
    public void Set(EquipmentSlot slot, int itemId) => _equipped[slot] = itemId;
    public bool TryGet(EquipmentSlot slot, out int itemId) => _equipped.TryGetValue(slot, out itemId);
}
