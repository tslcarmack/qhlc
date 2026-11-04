using Qhlc.Inventory.Public;

namespace Qhlc.Core.Tests;

public class StubEquipCatalogTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Stub_has_default_loadout_and_swappable_weapon()
    {
        var cat = new StubEquipCatalog();
        Assert.NotNull(cat.DefaultWeaponForNpc(10001));
        Assert.True(cat.TryGet(StubEquipCatalog.TrialSwordId, out var sword));
        Assert.Equal(EquipmentSlot.Weapon, sword.Slot);
        Assert.True(sword.AtkBonus > 0);
    }
}
