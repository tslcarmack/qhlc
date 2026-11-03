namespace Qhlc.Combat.Public;

public enum BattleActionKind
{
    Attack,
    Skill,
    Defend,
}

public sealed record BattleAction(
    int Actor,
    BattleActionKind Kind = BattleActionKind.Attack,
    int? SkillId = null,
    IReadOnlyList<int>? Targets = null);
