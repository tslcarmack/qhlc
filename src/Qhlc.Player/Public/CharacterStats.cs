namespace Qhlc.Player.Public;

public readonly record struct CharacterStats(
    int MaxHp, int Hp, int MaxSp, int Sp, int Atk, int Def, int Intel, int Agi, int Element);
