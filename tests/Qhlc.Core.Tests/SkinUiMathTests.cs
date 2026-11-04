using Qhlc.Core.Public;

namespace Qhlc.Core.Tests;

public class SkinUiMathTests
{
    [Fact]
    [Trait("TraceId", "TEST-0007")]
    public void Vertical_strip_splits_equal_rows()
    {
        var (x, y, w, h) = SkinUiMath.VerticalStripFrame(
            textureWidth: 40, textureHeight: 90, frameCount: 3, frameIndex: 1);
        Assert.Equal(0, x);
        Assert.Equal(30, y);
        Assert.Equal(40, w);
        Assert.Equal(30, h);
    }

    [Fact]
    [Trait("TraceId", "TEST-0007")]
    public void Clamp_keeps_window_inside_parent()
    {
        var (x, y) = SkinUiMath.ClampPosition(
            desiredX: -50, desiredY: 900,
            windowW: 100, windowH: 80,
            parentW: 800, parentH: 600);
        Assert.Equal(0, x);
        Assert.Equal(520, y);
    }
}
