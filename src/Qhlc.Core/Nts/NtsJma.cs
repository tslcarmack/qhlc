using System.Buffers.Binary;
using System.Text;
using Qhlc.Core.Public;

namespace Qhlc.Core.Nts;

/// <summary>
/// JMA/JXA animation synthesis (tsdocs format/jma.ts, jma_anim.ts). Whitelist blocks only.
/// </summary>
public static class NtsJma
{
    private const int HeaderSize = 14;
    private const int DirEntrySize = 28;
    private const int FrameRecordSize = 40;
    private const int PaletteSize = 1024;
    private const int IdxRecordSize = 80;
    private const int SeqSlots = 79;

    /// <summary>
    /// Default action stack: parts 0–5 + weapon (tsdocs §03.4 §5.3 verified composite).
    /// Parts 6–8 (<c>b</c>/<c>k</c>/<c>bh</c>/<c>bc</c>) are optional appearance replacements
    /// with static slot-0 frames only — never auto-stack them into walk/stand synthesis.
    /// </summary>
    private static readonly (string Suffix, int PartIndex)[] BodyLayers =
    [
        ("", 0),
        ("h", 1),
        ("c", 2),
        ("e", 3),
        ("s", 4),
        ("a", 5),
    ];

    private static readonly Dictionary<string, JmaSet> SetCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<(int BlockId, int Seq), SeqCanvas> SeqCanvasCache = new();

    /// <summary>
    /// Synthesize one composited frame for a whitelist block (e.g. 002:2000, parts 0–5 + weapon).
    /// Canvas is the union of the whole sequence (§05.1) so frame size/origin stay stable.
    /// </summary>
    public static bool TrySynthesizeFrame(
        string assetsRoot,
        int blockId,
        int seq,
        int frameIndex,
        out NtsRaster frame) =>
        TrySynthesizeFrame(assetsRoot, blockId, seq, frameIndex, out frame, out _, out _);

    /// <summary>
    /// Same as <see cref="TrySynthesizeFrame(string,int,int,int,out NtsRaster)"/>, also returns
    /// where the entity origin sits inside the raster (for foot/anchor placement).
    /// </summary>
    public static bool TrySynthesizeFrame(
        string assetsRoot,
        int blockId,
        int seq,
        int frameIndex,
        out NtsRaster frame,
        out int originX,
        out int originY)
    {
        frame = null!;
        originX = originY = 0;
        if (!JmaWhitelist.Contains(blockId))
            return false;

        var jmaDir = NtsPaths.JmaDirectory(assetsRoot);
        if (!Directory.Exists(jmaDir))
            return false;

        var family = blockId / 1000;
        if (family < 1 || family > 999)
            return false;

        var familyPrefix = family.ToString("D3");
        if (!TryGetOrBuildSeqCanvas(jmaDir, familyPrefix, blockId, seq, out var canvas))
            return false;

        var layers = new List<LayerDraw>();
        foreach (var (suffix, partIndex) in BodyLayers)
        {
            var blockNum = blockId + partIndex * 100;
            TryAddLayer(jmaDir, familyPrefix, suffix, blockNum, seq, frameIndex, layers);
        }

        TryAddLayer(jmaDir, familyPrefix, "w", blockId, seq, frameIndex, layers);
        if (layers.Count == 0)
            return false;

        if (!TryComposite(layers, canvas, out var raster))
            return false;

        if (!RasterHasVisiblePixel(raster))
            return false;

        frame = raster;
        originX = -canvas.MinX;
        originY = -canvas.MinY;
        return true;
    }

    /// <summary>
    /// Frame count for a whitelist block at the given sequence slot (max across body/weapon layers).
    /// </summary>
    public static bool TryGetSeqFrameCount(string assetsRoot, int blockId, int seq, out int frameCount)
    {
        frameCount = 0;
        if (!JmaWhitelist.Contains(blockId))
            return false;

        var jmaDir = NtsPaths.JmaDirectory(assetsRoot);
        if (!Directory.Exists(jmaDir))
            return false;

        var family = blockId / 1000;
        if (family < 1 || family > 999)
            return false;

        var familyPrefix = family.ToString("D3");
        var max = 0;

        foreach (var (suffix, partIndex) in BodyLayers)
        {
            var blockNum = blockId + partIndex * 100;
            if (TryGetLayerFrameCount(jmaDir, familyPrefix, suffix, blockNum, seq, out var n))
                max = Math.Max(max, n);
        }

        if (TryGetLayerFrameCount(jmaDir, familyPrefix, "w", blockId, seq, out var wn))
            max = Math.Max(max, wn);

        if (max <= 0)
            return false;

        frameCount = max;
        return true;
    }

    private static bool TryGetLayerFrameCount(
        string jmaDir,
        string familyPrefix,
        string suffix,
        int blockNum,
        int seq,
        out int framesInSeq)
    {
        framesInSeq = 0;
        var path = Path.Combine(jmaDir, $"{familyPrefix}{suffix}.jma");
        if (!File.Exists(path))
            return false;

        if (!TryLoadSet(path, out var set))
            return false;

        var blockName = $"{blockNum}.jmp";
        if (!set.TryResolveBlockIndex(blockName, out var blockIndex))
            return false;

        return set.TryGetSeqLength(blockIndex, seq, out framesInSeq) && framesInSeq > 0;
    }

    private static void TryAddLayer(
        string jmaDir,
        string familyPrefix,
        string suffix,
        int blockNum,
        int seq,
        int frameIndex,
        List<LayerDraw> layers)
    {
        var fileName = $"{familyPrefix}{suffix}.jma";
        var path = Path.Combine(jmaDir, fileName);
        if (!File.Exists(path))
            return;

        if (!TryLoadSet(path, out var set))
            return;

        var blockName = $"{blockNum}.jmp";
        if (!set.TryResolveBlockIndex(blockName, out var blockIndex))
            return;

        if (!set.TryGetFrame(blockIndex, seq, frameIndex, out var jmaFrame))
            return;

        if (jmaFrame.BitmapWidth <= 0 || jmaFrame.BitmapHeight <= 0)
            return;

        var ox = jmaFrame.DrawOffsetX;
        var oy = jmaFrame.DrawOffsetY;
        var rgba = jmaFrame.ToRgba();
        layers.Add(new LayerDraw(ox, oy, jmaFrame.BitmapWidth, jmaFrame.BitmapHeight, rgba));
    }

    private readonly record struct SeqCanvas(int MinX, int MinY, int Width, int Height);

    private static bool TryGetOrBuildSeqCanvas(
        string jmaDir,
        string familyPrefix,
        int blockId,
        int seq,
        out SeqCanvas canvas)
    {
        var key = (blockId, seq);
        lock (SeqCanvasCache)
        {
            if (SeqCanvasCache.TryGetValue(key, out canvas))
                return true;
        }

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxR = int.MinValue;
        var maxB = int.MinValue;
        var any = false;

        void ExpandLayer(string suffix, int blockNum)
        {
            var path = Path.Combine(jmaDir, $"{familyPrefix}{suffix}.jma");
            if (!File.Exists(path) || !TryLoadSet(path, out var set))
                return;
            var blockName = $"{blockNum}.jmp";
            if (!set.TryResolveBlockIndex(blockName, out var blockIndex))
                return;
            if (!set.TryGetSeqLength(blockIndex, seq, out var n) || n <= 0)
                return;

            for (var fi = 0; fi < n; fi++)
            {
                if (!set.TryGetFrame(blockIndex, seq, fi, out var fr))
                    continue;
                if (fr.BitmapWidth <= 0 || fr.BitmapHeight <= 0)
                    continue;
                any = true;
                minX = Math.Min(minX, fr.DrawOffsetX);
                minY = Math.Min(minY, fr.DrawOffsetY);
                maxR = Math.Max(maxR, fr.DrawOffsetX + fr.BitmapWidth);
                maxB = Math.Max(maxB, fr.DrawOffsetY + fr.BitmapHeight);
            }
        }

        foreach (var (suffix, partIndex) in BodyLayers)
            ExpandLayer(suffix, blockId + partIndex * 100);
        ExpandLayer("w", blockId);

        if (!any)
        {
            canvas = default;
            return false;
        }

        canvas = new SeqCanvas(minX, minY, Math.Max(1, maxR - minX), Math.Max(1, maxB - minY));
        lock (SeqCanvasCache)
            SeqCanvasCache[key] = canvas;
        return true;
    }

    private static bool TryComposite(IReadOnlyList<LayerDraw> layers, SeqCanvas canvas, out NtsRaster raster)
    {
        raster = null!;
        var outW = canvas.Width;
        var outH = canvas.Height;
        var buf = new byte[outW * outH * 4];

        foreach (var l in layers)
            Blit(buf, outW, outH, l.Rgba, l.Width, l.Height, l.Ox - canvas.MinX, l.Oy - canvas.MinY);

        raster = new NtsRaster(outW, outH, buf);
        return true;
    }

    private static void Blit(
        byte[] dst,
        int dstW,
        int dstH,
        byte[] src,
        int srcW,
        int srcH,
        int dx,
        int dy)
    {
        for (var y = 0; y < srcH; y++)
        {
            var py = dy + y;
            if (py < 0 || py >= dstH)
                continue;
            for (var x = 0; x < srcW; x++)
            {
                var px = dx + x;
                if (px < 0 || px >= dstW)
                    continue;
                var si = (y * srcW + x) * 4;
                if (src[si + 3] == 0)
                    continue;
                var di = (py * dstW + px) * 4;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
        }
    }

    private static bool RasterHasVisiblePixel(NtsRaster raster)
    {
        for (var i = 3; i < raster.Rgba.Length; i += 4)
        {
            if (raster.Rgba[i] != 0)
                return true;
        }

        return false;
    }

    private static bool TryLoadSet(string jmaPath, out JmaSet set)
    {
        lock (SetCache)
        {
            if (SetCache.TryGetValue(jmaPath, out set!))
                return true;
        }

        try
        {
            var jxaPath = ResolveJxaPath(jmaPath);
            if (jxaPath is null)
            {
                set = null!;
                return false;
            }

            set = new JmaSet(jmaPath, jxaPath);
            lock (SetCache)
                SetCache[jmaPath] = set;
            return true;
        }
        catch
        {
            set = null!;
            return false;
        }
    }

    private static string? ResolveJxaPath(string jmaPath)
    {
        var upper = Path.ChangeExtension(jmaPath, ".Jxa");
        if (File.Exists(upper))
            return upper;
        var lower = Path.ChangeExtension(jmaPath, ".jxa");
        return File.Exists(lower) ? lower : null;
    }

    private readonly record struct LayerDraw(int Ox, int Oy, int Width, int Height, byte[] Rgba);

    private sealed class JmaSet
    {
        private readonly byte[] _jma;
        private readonly byte[] _jxa;
        private readonly int _blockCount;
        private readonly List<(string Name, int Offset, int Size)> _entries;

        public JmaSet(string jmaPath, string jxaPath)
        {
            _jma = File.ReadAllBytes(jmaPath);
            _jxa = File.ReadAllBytes(jxaPath);
            if (_jma.Length < HeaderSize || Encoding.Latin1.GetString(_jma, 0, 2) != "ja")
                throw new InvalidDataException("Invalid JMA magic.");
            if (_jxa.Length < HeaderSize || Encoding.Latin1.GetString(_jxa, 0, 2) != "jx")
                throw new InvalidDataException("Invalid JXA magic.");

            _blockCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(10));
            var dataStart = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(6));
            if (dataStart != HeaderSize + DirEntrySize * _blockCount)
                throw new InvalidDataException("JMA directory size mismatch.");

            var jxaCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jxa.AsSpan(10));
            if (jxaCount != _blockCount)
                throw new InvalidDataException("JMA/JXA block count mismatch.");

            _entries = new List<(string, int, int)>(_blockCount);
            for (var i = 0; i < _blockCount; i++)
            {
                var o = HeaderSize + DirEntrySize * i;
                var nameLen = _jma[o];
                var name = Encoding.Latin1.GetString(_jma, o + 1, Math.Min((int)nameLen, 19));
                var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(o + 20));
                var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(o + 24));
                _entries.Add((name, offset, size));
            }
        }

        public bool TryResolveBlockIndex(string blockName, out int index)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Name.Equals(blockName, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public bool TryGetSeqLength(int blockIndex, int seq, out int framesInSeq)
        {
            framesInSeq = 0;
            if (blockIndex < 0 || blockIndex >= _blockCount)
                return false;

            var recOff = HeaderSize + IdxRecordSize * blockIndex;
            if (recOff + IdxRecordSize > _jxa.Length)
                return false;

            var rec = _jxa.AsSpan(recOff, IdxRecordSize);
            var seqCount = rec[79];
            if (seq < 0 || seq >= seqCount)
                return false;

            framesInSeq = rec[seq];
            return framesInSeq > 0;
        }

        public bool TryGetFrame(int blockIndex, int seq, int frameIndexInSeq, out JmaFrame frame)
        {
            frame = null!;
            if (blockIndex < 0 || blockIndex >= _blockCount)
                return false;

            var recOff = HeaderSize + IdxRecordSize * blockIndex;
            if (recOff + IdxRecordSize > _jxa.Length)
                return false;

            var rec = _jxa.AsSpan(recOff, IdxRecordSize);
            var seqCount = rec[79];
            if (seq < 0 || seq >= seqCount)
                return false;

            var framesInSeq = rec[seq];
            if (framesInSeq <= 0 || frameIndexInSeq < 0 || frameIndexInSeq >= framesInSeq)
                return false;

            var absFrame = 0;
            for (var i = 0; i < seq; i++)
                absFrame += rec[i];

            absFrame += frameIndexInSeq;

            var (_, blockOffset, blockSize) = _entries[blockIndex];
            if (blockOffset + blockSize > _jma.Length)
                return false;
            if (Encoding.Latin1.GetString(_jma, blockOffset, 2) != "jm")
                return false;

            var nframes = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(blockOffset + 10));
            if (absFrame >= nframes)
                return false;

            var paletteOff = blockOffset + HeaderSize + FrameRecordSize * nframes;
            var palette = _jma.AsSpan(paletteOff, PaletteSize);
            var dropRed = BuildRedOutlineDropSet(palette);

            var fo = blockOffset + HeaderSize + FrameRecordSize * absFrame;
            var dataSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(fo + 20));
            var dataOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(fo + 24));
            var bitmapW = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(fo + 28));
            var bitmapH = (int)BinaryPrimitives.ReadUInt32LittleEndian(_jma.AsSpan(fo + 32));
            var canvasW = BinaryPrimitives.ReadUInt16LittleEndian(_jma.AsSpan(fo + 12));
            var canvasH = BinaryPrimitives.ReadUInt16LittleEndian(_jma.AsSpan(fo + 14));
            var anchorX = BinaryPrimitives.ReadUInt16LittleEndian(_jma.AsSpan(fo + 16));
            var anchorY = BinaryPrimitives.ReadUInt16LittleEndian(_jma.AsSpan(fo + 18));

            var pxOff = blockOffset + dataOffset;
            if (pxOff + dataSize > _jma.Length)
                return false;

            var pixels = _jma.AsSpan(pxOff, dataSize).ToArray();
            frame = new JmaFrame(
                canvasW,
                canvasH,
                anchorX,
                anchorY,
                bitmapW,
                bitmapH,
                dataSize,
                pixels,
                palette.ToArray(),
                dropRed);
            return true;
        }

        private static HashSet<int> BuildRedOutlineDropSet(ReadOnlySpan<byte> palette)
        {
            var drop = new HashSet<int>();
            for (var i = 1; i < 256; i++)
            {
                var o = i * 4;
                var b = palette[o];
                var g = palette[o + 1];
                var r = palette[o + 2];
                if (r >= 240 && g <= 32 && b <= 32)
                    drop.Add(i);
            }

            return drop;
        }
    }

    private sealed class JmaFrame
    {
        private readonly byte[] _palette;
        private readonly byte[] _pixels;
        private readonly HashSet<int> _drop;

        public JmaFrame(
            int canvasW,
            int canvasH,
            int anchorX,
            int anchorY,
            int bitmapWidth,
            int bitmapHeight,
            int dataSize,
            byte[] pixels,
            byte[] palette,
            HashSet<int> drop)
        {
            CanvasW = canvasW;
            CanvasH = canvasH;
            AnchorX = anchorX;
            AnchorY = anchorY;
            BitmapWidth = bitmapWidth;
            BitmapHeight = bitmapHeight;
            _pixels = pixels;
            _palette = palette;
            _drop = drop;
            Stride = bitmapHeight > 0 && dataSize % bitmapHeight == 0
                ? dataSize / bitmapHeight
                : RowStride(bitmapWidth);
        }

        public int CanvasW { get; }
        public int CanvasH { get; }
        public int AnchorX { get; }
        public int AnchorY { get; }
        public int BitmapWidth { get; }
        public int BitmapHeight { get; }
        public int Stride { get; }

        public int DrawOffsetX => AnchorX - (CanvasW >> 1);
        public int DrawOffsetY => AnchorY - CanvasH;

        public byte[] ToRgba()
        {
            var w = BitmapWidth;
            var h = BitmapHeight;
            var outBuf = new byte[w * h * 4];
            var alpha = new byte[256];
            for (var i = 0; i < 256; i++)
                alpha[i] = i == 0 || _drop.Contains(i) ? (byte)0 : (byte)255;

            var o = 0;
            for (var y = 0; y < h; y++)
            {
                var row = y * Stride;
                for (var x = 0; x < w; x++)
                {
                    var v = _pixels[row + x];
                    var p = v * 4;
                    var r = _palette[p + 2];
                    var g = _palette[p + 1];
                    var b = _palette[p];
                    outBuf[o] = r;
                    outBuf[o + 1] = g;
                    outBuf[o + 2] = b;
                    outBuf[o + 3] = alpha[v];
                    o += 4;
                }
            }

            return outBuf;
        }

        private static int RowStride(int width) => (width + 3) & ~3;
    }
}
