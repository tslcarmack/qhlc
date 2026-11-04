using System.Buffers.Binary;
using Qhlc.Core.Public;

namespace Qhlc.Core.Nts;

public enum NtsBmpKey
{
    /// <summary>Pure black → transparent (fx / additive-style assets).</summary>
    PureBlack = 0,
    /// <summary>Pure green #00FF00 → transparent (map object overlays, §03.5 §3.5).</summary>
    PureGreen = 1,
}

/// <summary>
/// Windows BMP → RGBA (tsdocs format/bmp.ts).
/// </summary>
public static class NtsBmp
{
    public static bool IsBmp(ReadOnlySpan<byte> data) =>
        data.Length > 54 && data[0] == 0x42 && data[1] == 0x4D;

    public static NtsRaster Decode(ReadOnlySpan<byte> bmp) =>
        Decode(bmp, NtsBmpKey.PureBlack);

    public static NtsRaster Decode(ReadOnlySpan<byte> bmp, NtsBmpKey key)
    {
        if (!IsBmp(bmp))
            throw new InvalidDataException("Not a BMP (missing BM header).");

        var pixelOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(10));
        var headerSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(14));
        var width = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(18));
        var rawHeight = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(22));
        var bpp = BinaryPrimitives.ReadUInt16LittleEndian(bmp.Slice(28));
        var compression = BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(30));
        if (compression != 0)
            throw new InvalidDataException($"BMP compression {compression} is not supported (BI_RGB only).");

        var topDown = rawHeight < 0;
        var height = Math.Abs(rawHeight);
        if (width <= 0 || height <= 0)
            throw new InvalidDataException($"BMP size invalid: {width}×{rawHeight}.");

        if (bpp is not (8 or 24 or 32))
            throw new InvalidDataException($"BMP bit depth {bpp} is not supported.");

        var stride = (bpp * width + 31) / 32 * 4;
        var need = pixelOffset + stride * height;
        if (bmp.Length < need)
            throw new InvalidDataException($"BMP truncated: need {need} bytes, have {bmp.Length}.");

        ReadOnlySpan<byte> palette = ReadOnlySpan<byte>.Empty;
        var palLen = 0;
        if (bpp == 8)
        {
            var palAt = 14 + headerSize;
            var clrUsed = BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(46));
            if (clrUsed == 0)
                clrUsed = 256;
            palLen = Math.Min((int)clrUsed, 256);
            if (palAt + palLen * 4 > bmp.Length)
                throw new InvalidDataException("BMP palette extends past file end.");
            palette = bmp.Slice(palAt, palLen * 4);
        }

        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var srcRow = topDown ? y : height - 1 - y;
            var src = pixelOffset + srcRow * stride;
            var dst = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                byte r, g, b;
                if (bpp == 8)
                {
                    var idx = bmp[src + x];
                    if (idx >= palLen)
                    {
                        r = g = b = 0;
                    }
                    else
                    {
                        var po = idx * 4;
                        b = palette[po];
                        g = palette[po + 1];
                        r = palette[po + 2];
                    }
                }
                else if (bpp == 24)
                {
                    b = bmp[src + x * 3];
                    g = bmp[src + x * 3 + 1];
                    r = bmp[src + x * 3 + 2];
                }
                else
                {
                    b = bmp[src + x * 4];
                    g = bmp[src + x * 4 + 1];
                    r = bmp[src + x * 4 + 2];
                    var av = bmp[src + x * 4 + 3];
                    if (av != 0 && av != 255)
                    {
                        var o = dst + x * 4;
                        rgba[o] = r;
                        rgba[o + 1] = g;
                        rgba[o + 2] = b;
                        rgba[o + 3] = av;
                        continue;
                    }
                }

                var o2 = dst + x * 4;
                rgba[o2] = r;
                rgba[o2 + 1] = g;
                rgba[o2 + 2] = b;
                rgba[o2 + 3] = IsKey(r, g, b, key) ? (byte)0 : (byte)255;
            }
        }

        return new NtsRaster(width, height, rgba);
    }

    /// <summary>
    /// Vertical strip first-frame slice (tsdocs §03.5 §5.3 autocorrelation).
    /// </summary>
    public static NtsRaster TakeFirstFrame(NtsRaster full)
    {
        var frameH = DetectFrameHeight(full);
        if (frameH >= full.Height)
            return full;

        var w = full.Width;
        var rgba = new byte[w * frameH * 4];
        Buffer.BlockCopy(full.Rgba, 0, rgba, 0, rgba.Length);
        return new NtsRaster(w, frameH, rgba);
    }

    /// <summary>
    /// Detect vertical strip frame height. Returns full height when not a strip.
    /// </summary>
    public static int DetectFrameHeight(NtsRaster raster)
    {
        var w = raster.Width;
        var h = raster.Height;
        if (h < 16 || w < 1)
            return h;

        var bestP = h;
        for (var frames = 2; frames <= 64; frames++)
        {
            if (h % frames != 0)
                continue;
            var p = h / frames;
            if (p < 8)
                continue;

            var sP = RowDiff(raster, p);
            var sHalf = RowDiff(raster, Math.Max(1, p / 2));
            if (sP <= 0.62 && sP <= 0.50 * sHalf && p < bestP)
                bestP = p;
        }

        return bestP;
    }

    private static double RowDiff(NtsRaster raster, int period)
    {
        var w = raster.Width;
        var h = raster.Height;
        if (period <= 0 || period >= h)
            return 1.0;

        var step = Math.Max(1, period / 8);
        long diffs = 0;
        long samples = 0;
        var rgba = raster.Rgba;
        for (var y = 0; y + period < h; y += step)
        {
            var a = y * w * 4;
            var b = (y + period) * w * 4;
            for (var x = 0; x < w; x++)
            {
                var o = x * 4;
                diffs += Math.Abs(rgba[a + o] - rgba[b + o])
                    + Math.Abs(rgba[a + o + 1] - rgba[b + o + 1])
                    + Math.Abs(rgba[a + o + 2] - rgba[b + o + 2]);
                samples += 3 * 255;
            }
        }

        return samples == 0 ? 1.0 : diffs / (double)samples;
    }

    private static bool IsKey(byte r, byte g, byte b, NtsBmpKey key) =>
        key switch
        {
            NtsBmpKey.PureGreen => r == 0 && g == 255 && b == 0,
            _ => r == 0 && g == 0 && b == 0,
        };
}
