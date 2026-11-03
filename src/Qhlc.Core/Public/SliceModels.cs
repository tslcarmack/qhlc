namespace Qhlc.Core.Public;

public readonly record struct SceneRecord(int AreaId, byte Seq, byte MapSet, string Name);

public readonly record struct NpcRecord(
    int Id,
    string Name,
    int ModelId,
    int Level,
    int Hp,
    int Sp,
    int Atk,
    int Def,
    int Int,
    int Agi,
    int Faction);

public readonly record struct SkillRecord(
    int Id,
    string Name,
    int Kind,
    int Mp,
    int P32,
    int TargetParam);
