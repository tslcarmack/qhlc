namespace Qhlc.Save.Public;

public sealed class SaveSnapshot
{
    public int SaveVersion { get; init; }

    public string PlayerId { get; init; } = string.Empty;
}

public static class ModuleMarker
{
    public const string ModuleId = "MOD-SAVE";
}
