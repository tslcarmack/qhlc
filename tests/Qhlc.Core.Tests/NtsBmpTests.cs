using System.Buffers.Binary;
using Qhlc.Core.Nts;

namespace Qhlc.Core.Tests;

public class NtsBmpTests
{
    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void Decode_minimal_2x1_24bit_bmp()
    {
        var bmp = MinimalBmp2x1();
        var raster = NtsBmp.Decode(bmp);

        Assert.Equal(2, raster.Width);
        Assert.Equal(1, raster.Height);
        Assert.Equal(8, raster.Rgba.Length);

        // Left pixel: red (255,0,0)
        Assert.Equal(255, raster.Rgba[0]);
        Assert.Equal(0, raster.Rgba[1]);
        Assert.Equal(0, raster.Rgba[2]);
        Assert.Equal(255, raster.Rgba[3]);

        // Right pixel: green (0,255,0) — not pure black, opaque
        Assert.Equal(0, raster.Rgba[4]);
        Assert.Equal(255, raster.Rgba[5]);
        Assert.Equal(0, raster.Rgba[6]);
        Assert.Equal(255, raster.Rgba[7]);
    }

    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void Decode_pure_black_pixels_are_transparent()
    {
        var bmp = MinimalBmp2x1(blackLeft: true);
        var raster = NtsBmp.Decode(bmp);
        Assert.Equal(0, raster.Rgba[3]);
        Assert.Equal(255, raster.Rgba[7]);
    }

    [Fact]
    [Trait("TraceId", "TEST-0003")]
    public void Decode_pure_green_key_for_map_objects()
    {
        var bmp = MinimalBmp2x1();
        var raster = NtsBmp.Decode(bmp, NtsBmpKey.PureGreen);
        // Left red stays opaque; right pure green is transparent.
        Assert.Equal(255, raster.Rgba[3]);
        Assert.Equal(0, raster.Rgba[7]);
    }

    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void Archive_reads_32_byte_directory_entry()
    {
        var dir = BuildMiniArchive("test.bmp", payload: [0x42, 0x4D, 0x00]);
        var path = Path.Combine(Path.GetTempPath(), $"nts-bmg-{Guid.NewGuid():N}.bmg");
        try
        {
            File.WriteAllBytes(path, dir);
            var arc = NtsArchive.Open(path);
            Assert.True(arc.TryReadEntry("test.bmp", out var payload));
            Assert.Equal(3, payload.Length);
            Assert.Equal(0x42, payload[0]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    [Trait("TraceId", "TEST-0004")]
    public void TrySynthesizeFrame_block2000_when_jma_present()
    {
        var root = NtsPaths.ResolveAssetsRoot();
        var jmaDir = NtsPaths.JmaDirectory(root);
        var probe = Path.Combine(jmaDir, "002.jma");
        if (!File.Exists(probe))
            return;

        var ok = NtsJma.TrySynthesizeFrame(root, 2000, seq: 0, frameIndex: 0, out var frame);
        Assert.True(ok);
        Assert.True(frame.Width > 0 && frame.Height > 0);

        var visible = 0;
        for (var i = 3; i < frame.Rgba.Length; i += 4)
        {
            if (frame.Rgba[i] != 0)
                visible++;
        }

        Assert.True(visible > 0);
    }

    [Fact]
    [Trait("TraceId", "TEST-0003")]
    public void TrySynthesizeFrame_seq_canvas_stable_across_frames()
    {
        var root = NtsPaths.ResolveAssetsRoot();
        var probe = Path.Combine(NtsPaths.JmaDirectory(root), "002.jma");
        if (!File.Exists(probe))
            return;

        Assert.True(NtsJma.TryGetSeqFrameCount(root, 2000, 0, out var count));
        Assert.True(count >= 2);
        Assert.True(NtsJma.TrySynthesizeFrame(root, 2000, 0, 0, out var f0, out var ox0, out var oy0));
        Assert.True(NtsJma.TrySynthesizeFrame(root, 2000, 0, 1, out var f1, out var ox1, out var oy1));
        Assert.Equal(f0.Width, f1.Width);
        Assert.Equal(f0.Height, f1.Height);
        Assert.Equal(ox0, ox1);
        Assert.Equal(oy0, oy1);
    }

    private static byte[] MinimalBmp2x1(bool blackLeft = false)
    {
        const int width = 2;
        const int height = 1;
        const int bpp = 24;
        const int pixelOffset = 54;
        const int stride = 8;
        var size = pixelOffset + stride * height;
        var buf = new byte[size];

        buf[0] = 0x42;
        buf[1] = 0x4D;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(2), (uint)size);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(10), (uint)pixelOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(22), height);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(26), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(28), bpp);

        if (blackLeft)
        {
            buf[pixelOffset] = 0;
            buf[pixelOffset + 1] = 0;
            buf[pixelOffset + 2] = 0;
        }
        else
        {
            buf[pixelOffset] = 0;
            buf[pixelOffset + 1] = 0;
            buf[pixelOffset + 2] = 255;
        }

        buf[pixelOffset + 3] = 0;
        buf[pixelOffset + 4] = 255;
        buf[pixelOffset + 5] = 0;
        return buf;
    }

    private static byte[] BuildMiniArchive(string entryName, byte[] payload)
    {
        var nameBytes = System.Text.Encoding.Latin1.GetBytes(entryName);
        if (nameBytes.Length is < 1 or > 23)
            throw new ArgumentException("Entry name length must be 1..23.");

        var dir = new byte[32];
        dir[0] = (byte)nameBytes.Length;
        nameBytes.CopyTo(dir, 1);
        var offset = (uint)(2 + 32);
        BinaryPrimitives.WriteUInt32LittleEndian(dir.AsSpan(24), offset);
        BinaryPrimitives.WriteUInt32LittleEndian(dir.AsSpan(28), (uint)payload.Length);

        var file = new byte[2 + 32 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(0), 1);
        dir.CopyTo(file, 2);
        payload.CopyTo(file, 2 + 32);
        return file;
    }
}
