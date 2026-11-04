using Qhlc.Player.Public;

namespace Qhlc.Combat.Public;

/// <summary>Projects live <see cref="Character"/> stats into immutable battle snapshots.</summary>
public static class BattleUnitMapper
{
    public static BattleUnit From(Character character, int team, int slot, bool leader)
    {
        ArgumentNullException.ThrowIfNull(character);

        var s = character.EffectiveStats();
        var uid = team * 1000 + slot + 1;
        return new BattleUnit
        {
            Uid = uid,
            Name = character.Name,
            Team = team,
            Slot = slot,
            MaxHp = s.MaxHp,
            Hp = s.Hp,
            MaxSp = s.MaxSp,
            Sp = s.Sp,
            Atk = s.Atk,
            Def = s.Def,
            Intel = s.Intel,
            Agi = s.Agi,
            Element = s.Element,
            Leader = leader,
            Skills = Array.Empty<int>(),
        };
    }

    public static List<BattleUnit> FromParty(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        var units = new List<BattleUnit>();
        var slot = 0;
        foreach (var member in party.Members)
        {
            if (member.EffectiveStats().Hp <= 0)
                continue;

            var leader = ReferenceEquals(member, party.Leader);
            units.Add(From(member, team: 0, slot, leader));
            slot++;
        }

        return units;
    }
}
