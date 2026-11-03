namespace Qhlc.Combat.Public;

public sealed record BattleOutcome(int? WinnerTeam, int Rounds, string? EndReason);

/// <summary>精简回合引擎：普攻/技能伤害，无道具/逃跑/捕捉。</summary>
public sealed class Battle
{
    public const int MeleeSkillId = 10000;
    public const int DefaultMeleeP32 = 1;

    private readonly List<BattleUnit> _units;
    private readonly int _maxRounds;
    private int _round;
    private bool _finished;
    private int? _winner;
    private string? _endReason;

    public Battle(IReadOnlyList<BattleUnit> allies, IReadOnlyList<BattleUnit> enemies)
        : this(CloneParties(allies, enemies), maxRounds: 30)
    {
    }

    private Battle(List<BattleUnit> units, int maxRounds)
    {
        _units = units;
        _maxRounds = maxRounds;
    }

    public static Battle FromParties(IReadOnlyList<BattleUnit> allies, IReadOnlyList<BattleUnit> enemies) =>
        new(allies, enemies);

    private static List<BattleUnit> CloneParties(
        IReadOnlyList<BattleUnit> allies,
        IReadOnlyList<BattleUnit> enemies)
    {
        var units = new List<BattleUnit>(allies.Count + enemies.Count);
        foreach (var u in allies)
            units.Add(u.CloneForBattle());
        foreach (var u in enemies)
            units.Add(u.CloneForBattle());
        return units;
    }

    public BattleOutcome RunAuto(Random rng)
    {
        Start();
        while (!_finished && _round < _maxRounds)
            RunRound(rng);

        return new BattleOutcome(_winner, _round, _endReason);
    }

    private void Start()
    {
        foreach (var u in _units)
        {
            u.Hp = Math.Min(u.Hp, u.MaxHp);
            u.Sp = Math.Min(u.Sp, u.MaxSp);
            u.Defending = false;
        }

        _round = 0;
        _finished = false;
        _winner = null;
        _endReason = null;
        CheckEnd(afterTick: false);
    }

    private void RunRound(Random rng)
    {
        if (_finished)
            return;

        BeginRound();
        foreach (var uid in Order())
        {
            if (_finished)
                break;
            var actor = ByUid(uid);
            if (actor is null || !actor.IsAlive)
                continue;
            Act(PlanFor(actor), rng);
        }

        EndRound();
    }

    private void BeginRound()
    {
        if (_finished)
            return;
        _round++;
        foreach (var u in _units)
            u.Defending = false;
    }

    private void EndRound()
    {
        if (_finished)
            return;
        CheckEnd(afterTick: true);
    }

    private IReadOnlyList<int> Order()
    {
        return _units
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Agi)
            .ThenBy(u => u.Team)
            .ThenBy(u => u.Slot)
            .Select(u => u.Uid)
            .ToList();
    }

    private BattleUnit? ByUid(int uid) => _units.FirstOrDefault(u => u.Uid == uid);

    private List<BattleUnit> AliveOf(int team) =>
        _units.Where(u => u.Team == team && u.IsAlive).ToList();

    private List<BattleUnit> EnemiesOf(BattleUnit u) => AliveOf(u.Team == 0 ? 1 : 0);

    private BattleAction PlanFor(BattleUnit u)
    {
        var foes = EnemiesOf(u);
        if (foes.Count == 0)
            return new BattleAction(u.Uid, BattleActionKind.Attack);

        var target = foes.OrderBy(f => f.Def).ThenBy(f => f.Hp).First();
        return new BattleAction(u.Uid, BattleActionKind.Attack, Targets: new[] { target.Uid });
    }

    private void Act(BattleAction action, Random rng)
    {
        var actor = ByUid(action.Actor);
        if (actor is null || !actor.IsAlive)
            return;

        switch (action.Kind)
        {
            case BattleActionKind.Defend:
                actor.Defending = true;
                return;
            case BattleActionKind.Attack:
                DoMelee(actor, action, rng);
                return;
            case BattleActionKind.Skill:
                if (action.SkillId is null)
                    DoMelee(actor, action, rng);
                return;
        }
    }

    private void DoMelee(BattleUnit actor, BattleAction action, Random rng)
    {
        var foes = EnemiesOf(actor);
        if (foes.Count == 0)
            return;

        var targetUid = action.Targets?.FirstOrDefault();
        BattleUnit? target = targetUid is > 0 ? ByUid(targetUid.Value) : null;
        target ??= foes.OrderBy(f => f.Def).ThenBy(f => f.Hp).First();
        if (!target.IsAlive || target.Team == actor.Team)
            return;

        var rngNext = () => rng.NextDouble();
        var (damage, _) = ReferenceFormula.Physical(
            actor,
            target,
            DefaultMeleeP32,
            rngNext);

        ApplyDamage(target, damage);
        CheckEnd(afterTick: false);
    }

    private static void ApplyDamage(BattleUnit target, int amount)
    {
        target.Hp = Math.Max(0, target.Hp - amount);
    }

    private void CheckEnd(bool afterTick)
    {
        if (_finished)
            return;

        foreach (var team in new[] { 0, 1 })
        {
            var leader = _units.FirstOrDefault(u => u.Team == team && u.Leader);
            if (leader is not null && !leader.IsAlive)
            {
                Finish($"leader-died:{team}", team == 0 ? 1 : 0);
                return;
            }
        }

        foreach (var team in new[] { 0, 1 })
        {
            if (AliveOf(team).Count == 0)
            {
                Finish($"wipe:{team}", team == 0 ? 1 : 0);
                return;
            }
        }

        if (afterTick && _round >= _maxRounds)
            Finish("round-limit", null);
    }

    private void Finish(string reason, int? winner)
    {
        _finished = true;
        _endReason = reason;
        _winner = winner;
    }
}
