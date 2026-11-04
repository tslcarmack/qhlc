namespace Qhlc.Core.Nts;

/// <summary>
/// JMA action-slot facing (tsdocs §03.4 §5.1).
/// Walk = slots 0–7, Stand = 8–15; offset 0 = 上 … 7 = 右上.
/// </summary>
public static class JmaFacing
{
    public const int WalkBase = 0;
    public const int StandBase = 8;

    /// <summary>
    /// Direction offsets within an 8-way action group (screen +Y down).
    /// </summary>
    public static readonly (float X, float Y)[] OffsetVectors =
    [
        (0f, -1f),  // 0 上
        (-1f, -1f), // 1 左上
        (-1f, 0f),  // 2 左
        (-1f, 1f),  // 3 左下
        (0f, 1f),   // 4 下
        (1f, 1f),   // 5 右下
        (1f, 0f),   // 6 右
        (1f, -1f),  // 7 右上
    ];

    /// <summary>Walk sequence slot 0–7 for a movement vector.</summary>
    public static int WalkSeqForDirection(float dirX, float dirY)
    {
        if (dirX * dirX + dirY * dirY < 1e-8f)
            return 4; // default 下

        var len = MathF.Sqrt(dirX * dirX + dirY * dirY);
        dirX /= len;
        dirY /= len;

        var best = 4;
        var bestDot = float.NegativeInfinity;
        for (var i = 0; i < OffsetVectors.Length; i++)
        {
            var (x, y) = OffsetVectors[i];
            var vl = MathF.Sqrt(x * x + y * y);
            var dot = dirX * (x / vl) + dirY * (y / vl);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }

        return WalkBase + best;
    }

    /// <summary>
    /// Keep the current walk facing until the movement vector clearly leaves its cone.
    /// Prevents octant chatter from resetting the walk cycle mid-stride.
    /// </summary>
    public static int StickWalkSeq(int currentWalkSeq, float dirX, float dirY, float keepDot = 0.88f)
    {
        var best = WalkSeqForDirection(dirX, dirY);
        var cur = Math.Clamp(currentWalkSeq - WalkBase, 0, 7) + WalkBase;
        if (best == cur || dirX * dirX + dirY * dirY < 1e-8f)
            return cur;

        var (cx, cy) = OffsetVectors[cur - WalkBase];
        var cl = MathF.Sqrt(cx * cx + cy * cy);
        var len = MathF.Sqrt(dirX * dirX + dirY * dirY);
        var dot = (dirX / len) * (cx / cl) + (dirY / len) * (cy / cl);
        return dot >= keepDot ? cur : best;
    }

    /// <summary>Stand sequence slot 8–15 for the same facing as a walk slot 0–7.</summary>
    public static int StandSeqForWalkSeq(int walkSeq)
    {
        var offset = Math.Clamp(walkSeq - WalkBase, 0, 7);
        return StandBase + offset;
    }

    public static bool IsWalkSeq(int seq) => seq is >= WalkBase and < StandBase;
}
