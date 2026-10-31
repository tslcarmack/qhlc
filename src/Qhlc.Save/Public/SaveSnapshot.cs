namespace Qhlc.Save.Public;

public sealed class SaveSnapshot
{
    public int SaveVersion { get; init; }

    public string PlayerId { get; init; } = string.Empty;

    public static SaveSnapshot Create(int saveVersion, string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            throw new ArgumentException("playerId 不可为空。", nameof(playerId));
        }

        if (saveVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(saveVersion), "saveVersion 必须 >= 1。");
        }

        return new SaveSnapshot { SaveVersion = saveVersion, PlayerId = playerId };
    }
}

public static class ModuleMarker
{
    public const string ModuleId = "MOD-SAVE";
}
