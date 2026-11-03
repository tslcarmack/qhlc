namespace Qhlc.Combat.Public;

/// <summary>战斗单位（可变 HP/SP，供回合引擎就地结算）。</summary>
public sealed class BattleUnit
{
    public int Uid { get; init; }
    public string Name { get; init; } = "";
    /// <summary>0 = 我方，1 = 敌方。</summary>
    public int Team { get; init; }
    public int Slot { get; init; }
    public int MaxHp { get; set; }
    public int Hp { get; set; }
    public int MaxSp { get; set; }
    public int Sp { get; set; }
    public int Atk { get; init; }
    public int Def { get; init; }
    public int Intel { get; init; }
    public int Agi { get; init; }
    public int Element { get; init; }
    public bool Leader { get; init; }
    public IReadOnlyList<int> Skills { get; init; } = Array.Empty<int>();
    public bool Defending { get; set; }

    public bool IsAlive => Hp > 0;

    public static BattleUnit Create(
        int uid,
        string name,
        int team = 0,
        int slot = 0,
        int hp = 100,
        int atk = 20,
        int def = 10,
        int intel = 10,
        int agi = 10,
        int maxSp = 50,
        int sp = -1,
        bool leader = false,
        int element = 0,
        IReadOnlyList<int>? skills = null)
    {
        var resolvedSp = sp < 0 ? maxSp : sp;
        return new BattleUnit
        {
            Uid = uid,
            Name = name,
            Team = team,
            Slot = slot,
            MaxHp = hp,
            Hp = hp,
            MaxSp = maxSp,
            Sp = resolvedSp,
            Atk = atk,
            Def = def,
            Intel = intel,
            Agi = agi,
            Element = element,
            Leader = leader,
            Skills = skills ?? Array.Empty<int>(),
        };
    }

    internal BattleUnit CloneForBattle()
    {
        return new BattleUnit
        {
            Uid = Uid,
            Name = Name,
            Team = Team,
            Slot = Slot,
            MaxHp = MaxHp,
            Hp = Hp,
            MaxSp = MaxSp,
            Sp = Sp,
            Atk = Atk,
            Def = Def,
            Intel = Intel,
            Agi = Agi,
            Element = Element,
            Leader = Leader,
            Skills = Skills,
            Defending = false,
        };
    }
}
