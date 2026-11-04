using Qhlc.Inventory.Public;
using Qhlc.Player.Public;

public class CharacterEquipTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Equip_trial_sword_raises_effective_atk()
    {
        var cat = new StubEquipCatalog();
        var hero = new PlayerCharacter(
            id: 10001,
            name: "Hero",
            bodyBlockId: 2000,
            baseStats: new CharacterStats(100, 100, 50, 50, 20, 10, 10, 10, 0),
            catalog: cat);
        hero.TryEquip(StubEquipCatalog.DefaultSwordId, out _);
        var before = hero.EffectiveStats().Atk;
        Assert.True(hero.TryEquip(StubEquipCatalog.TrialSwordId, out _));
        Assert.True(hero.EffectiveStats().Atk > before);
        Assert.Equal(2000, hero.GetAppearanceBlocks().BodyBlockId);
    }
}
