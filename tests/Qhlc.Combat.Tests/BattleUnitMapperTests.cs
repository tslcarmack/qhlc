using Qhlc.Combat.Public;
using Qhlc.Inventory.Public;
using Qhlc.Player.Public;

namespace Qhlc.Combat.Tests;

public class BattleUnitMapperTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Mapper_includes_weapon_atk_bonus()
    {
        var cat = new StubEquipCatalog();
        var hero = new PlayerCharacter(1, "H", 2000, new CharacterStats(100, 100, 50, 50, 20, 10, 10, 10, 0), cat);
        hero.TryEquip(StubEquipCatalog.DefaultSwordId, out _);
        var baseAtk = BattleUnitMapper.From(hero, team: 0, slot: 0, leader: true).Atk;
        hero.TryEquip(StubEquipCatalog.TrialSwordId, out _);
        var boosted = BattleUnitMapper.From(hero, 0, 0, true).Atk;
        Assert.True(boosted > baseAtk);
    }
}
