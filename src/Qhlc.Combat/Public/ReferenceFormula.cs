namespace Qhlc.Combat.Public;

/// <summary>参考伤害公式（非游戏真公式），对齐 tsdocs referenceFormula。</summary>
public static class ReferenceFormula
{
    public const string Source = "reference";

    public static double SkillCoef(int p32) => 0.6 + Math.Min(p32, 120) / 120.0;

    public static (int Damage, bool Crit) Physical(
        BattleUnit actor,
        BattleUnit target,
        int skillP32,
        Func<double> rng,
        double elementMul = 1)
    {
        var power = SkillCoef(skillP32);
        var baseDmg = actor.Atk * power * 1.5 - target.Def * 0.5;
        var crit = rng() < 0.05;
        var raw = Math.Max(1, baseDmg) * (crit ? 1.6 : 1) * (target.Defending ? 0.5 : 1) * elementMul;
        return (Math.Max(1, (int)Math.Round(raw)), crit);
    }

    public static (int Damage, bool Crit) Magical(
        BattleUnit actor,
        BattleUnit target,
        int skillP32,
        Func<double> rng,
        double elementMul = 1)
    {
        var power = SkillCoef(skillP32);
        var baseDmg = actor.Intel * power * 1.3 - target.Def * 0.4;
        var crit = rng() < 0.03;
        var raw = Math.Max(1, baseDmg) * (crit ? 1.6 : 1) * (target.Defending ? 0.5 : 1) * elementMul;
        return (Math.Max(1, (int)Math.Round(raw)), crit);
    }
}
