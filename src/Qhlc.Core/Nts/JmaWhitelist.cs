namespace Qhlc.Core.Nts;

/// <summary>
/// Verified synthesizable JMA block ids (tsdocs §05.1 — 002:2000 chain).
/// </summary>
public static class JmaWhitelist
{
    public static int[] Blocks { get; } = [2000];

    public static bool Contains(int blockId) =>
        Array.IndexOf(Blocks, blockId) >= 0;
}
