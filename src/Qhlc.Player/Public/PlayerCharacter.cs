using Qhlc.Inventory.Public;

namespace Qhlc.Player.Public;

public sealed class PlayerCharacter : Character
{
    public PlayerCharacter(int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
        : base(id, name, bodyBlockId, baseStats, catalog) { }
}
