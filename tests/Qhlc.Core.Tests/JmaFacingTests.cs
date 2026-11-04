using Qhlc.Core.Nts;

namespace Qhlc.Core.Tests;

public class JmaFacingTests
{
    [Theory]
    [Trait("TraceId", "TEST-0003")]
    [InlineData(0f, -1f, 0)]   // 上
    [InlineData(-1f, -1f, 1)]  // 左上
    [InlineData(-1f, 0f, 2)]   // 左
    [InlineData(-1f, 1f, 3)]   // 左下
    [InlineData(0f, 1f, 4)]    // 下
    [InlineData(1f, 1f, 5)]    // 右下
    [InlineData(1f, 0f, 6)]    // 右
    [InlineData(1f, -1f, 7)]   // 右上
    public void WalkSeqForDirection_matches_03_4_offsets(float dx, float dy, int expected)
    {
        Assert.Equal(expected, JmaFacing.WalkSeqForDirection(dx, dy));
    }

    [Fact]
    [Trait("TraceId", "TEST-0003")]
    public void StandSeq_is_walk_plus_eight()
    {
        for (var walk = 0; walk < 8; walk++)
            Assert.Equal(8 + walk, JmaFacing.StandSeqForWalkSeq(walk));
    }

    [Fact]
    [Trait("TraceId", "TEST-0003")]
    public void StickWalkSeq_keeps_facing_inside_cone()
    {
        // Facing right (6); slight up-right still inside keepDot → stay 6.
        Assert.Equal(6, JmaFacing.StickWalkSeq(6, 1f, -0.2f));
        // Strong up → leave cone → switch to 0.
        Assert.Equal(0, JmaFacing.StickWalkSeq(6, 0f, -1f));
    }
}
