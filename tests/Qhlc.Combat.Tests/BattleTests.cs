using Qhlc.Combat.Public;

namespace Qhlc.Combat.Tests;

public class BattleTests
{
    [Trait("TraceId", "TEST-0004")]
    [Fact]
    public void ReferenceFormula_source_is_reference()
    {
        Assert.Equal("reference", ReferenceFormula.Source);
    }

    [Fact]
    public void AutoBattle_finishes_with_winner()
    {
        var allies = new[] { BattleUnit.Create(1, "A", team: 0, slot: 0, hp: 200, atk: 80, def: 10, intel: 10, agi: 50) };
        var enemies = new[] { BattleUnit.Create(2, "B", team: 1, slot: 0, hp: 50, atk: 5, def: 5, intel: 5, agi: 10) };
        var outcome = new Battle(allies, enemies).RunAuto(new Random(0));
        Assert.True(outcome.WinnerTeam is 0 or 1);
    }
}
