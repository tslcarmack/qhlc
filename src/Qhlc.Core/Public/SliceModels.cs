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

public sealed record ResolvedContent(
    int AreaId,
    string SceneName,
    IReadOnlyList<int> AllyNpcIds,
    IReadOnlyList<int> EnemyNpcIds,
    IReadOnlyDictionary<int, int> NpcIdToBlockId);

/// <summary>RGBA raster (row-major, top-down). Produced by BMP decode and JMA synthesis.</summary>
public sealed class NtsRaster
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public NtsRaster(int width, int height, byte[] rgba)
    {
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (rgba.Length != width * height * 4)
            throw new ArgumentException("RGBA length must be width×height×4.", nameof(rgba));

        Width = width;
        Height = height;
        Rgba = rgba;
    }
}
