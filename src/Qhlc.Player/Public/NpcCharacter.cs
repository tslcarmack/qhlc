using Qhlc.Inventory.Public;

namespace Qhlc.Player.Public;

public sealed class NpcCharacter : Character
{
    public NpcCharacter(int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
        : base(id, name, bodyBlockId, baseStats, catalog) { }
}
