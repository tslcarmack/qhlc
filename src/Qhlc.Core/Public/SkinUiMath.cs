namespace Qhlc.Core.Public;

/// <summary>Skin UI pure helpers (vertical btn strips + window clamp).</summary>
public static class SkinUiMath
{
    public static (int X, int Y, int W, int H) VerticalStripFrame(
        int textureWidth, int textureHeight, int frameCount, int frameIndex)
    {
        if (textureWidth <= 0 || textureHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(textureWidth));
        if (frameCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
        var h = textureHeight / frameCount;
        return (0, h * frameIndex, textureWidth, h);
    }

    public static (float X, float Y) ClampPosition(
        float desiredX, float desiredY,
        float windowW, float windowH,
        float parentW, float parentH)
    {
        var maxX = Math.Max(0, parentW - windowW);
        var maxY = Math.Max(0, parentH - windowH);
        return (Math.Clamp(desiredX, 0, maxX), Math.Clamp(desiredY, 0, maxY));
    }
}
