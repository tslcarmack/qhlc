namespace Qhlc.Core.Nts;

public readonly record struct NtsMapTile(int Scene, int X, int Y);

/// <summary>
/// Ground.MMG / .map collision canvas (tsdocs format/ground.ts).
/// Grid is column-major: <c>grid[x * gh + y]</c>, 0 = walkable.
/// </summary>
public sealed class NtsMap
{
    public const int CellPixels = 20;
    private const int DirRecordSize = 29;

    public int ImgW { get; }
    public int ImgH { get; }
    public int Gw { get; }
    public int Gh { get; }
    public byte[] Grid { get; }
    public IReadOnlyList<NtsMapTile> Tiles { get; }

    public NtsMap(
        int imgW,
        int imgH,
        int gw,
        int gh,
        byte[] grid,
        IReadOnlyList<NtsMapTile> tiles)
    {
        if (gw < 1 || gh < 1)
            throw new ArgumentOutOfRangeException(nameof(gw), "Grid dimensions must be positive.");
        if (grid.Length != gw * gh)
            throw new ArgumentException($"Grid length {grid.Length} != {gw}×{gh}.", nameof(grid));

        ImgW = imgW;
        ImgH = imgH;
        Gw = gw;
        Gh = gh;
        Grid = grid;
        Tiles = tiles;
    }

    public static NtsMap LoadFromGround(string assetsRoot, int areaId)
    {
        var path = Path.Combine(NtsPaths.DataDirectory(assetsRoot), "Ground.MMG");
        if (!File.Exists(path))
            throw new FileNotFoundException("Ground.MMG not found.", path);

        var buf = File.ReadAllBytes(path);
        var targetName = $"{areaId}.map";
        var entry = FindMapEntry(buf, targetName)
            ?? throw new InvalidOperationException($"Ground.MMG: map '{targetName}' not found.");

        return ParseMapData(buf, entry.Offset, entry.Size);
    }

    public bool CanWalk(int px, int py)
    {
        if (px < 0 || py < 0)
            return false;

        int gx = px / CellPixels, gy = py / CellPixels;
        return IsCellWalkable(gx, gy);
    }

    public bool IsCellWalkable(int gx, int gy)
    {
        if (gx < 0 || gy < 0 || gx >= Gw - 1 || gy >= Gh - 1)
            return false;
        return Grid[gx * Gh + gy] == 0;
    }

    public static (int Gx, int Gy) PixelToCell(int px, int py) =>
        (px / CellPixels, py / CellPixels);

    public static (int X, int Y) CellCenter(int gx, int gy) =>
        (gx * CellPixels + CellPixels / 2, gy * CellPixels + CellPixels / 2);

    private readonly record struct GroundDirEntry(string Name, int Offset, int Size);

    private static GroundDirEntry? FindMapEntry(ReadOnlySpan<byte> buf, string mapName)
    {
        var dirStart = FindDirStart(buf);
        for (var p = dirStart; p + DirRecordSize <= buf.Length; p += DirRecordSize)
        {
            var nameLen = buf[p];
            if (nameLen is < 5 or > 20)
                break;

            var nameEnd = p + 1 + nameLen;
            if (nameEnd > buf.Length)
                break;

            var name = System.Text.Encoding.Latin1.GetString(buf.Slice(p + 1, nameLen));
            if (!name.EndsWith(".map", StringComparison.Ordinal))
                break;

            var offset = (int)NtsBuf.ReadU32Le(buf, p + 12 + nameLen);
            var size = (int)NtsBuf.ReadU32Le(buf, p + 16 + nameLen);

            if (string.Equals(name, mapName, StringComparison.OrdinalIgnoreCase))
                return new GroundDirEntry(name, offset, size);
        }

        return null;
    }

    private static int FindDirStart(ReadOnlySpan<byte> buf)
    {
        var marker = ".map"u8;
        var i = buf.IndexOf(marker);
        if (i < 0)
            throw new InvalidDataException("Ground.MMG: .map directory not found.");
        return i - 6;
    }

    private static NtsMap ParseMapData(ReadOnlySpan<byte> buf, int offset, int size)
    {
        if (offset + 9 > buf.Length)
            throw new InvalidDataException("Ground.MMG: map header out of range.");

        var imgW = (int)NtsBuf.ReadU32Le(buf, offset);
        var imgH = (int)NtsBuf.ReadU32Le(buf, offset + 4);
        var count = buf[offset + 8];
        if (count > 64)
            throw new InvalidDataException($"Ground.MMG: invalid tile count {count}.");

        var tiles = new List<NtsMapTile>(count);
        for (var i = 0; i < count; i++)
        {
            var q = offset + 9 + i * 6;
            if (q + 6 > buf.Length)
                throw new InvalidDataException("Ground.MMG: tile table out of range.");

            tiles.Add(new NtsMapTile(
                NtsBuf.ReadU16Le(buf, q),
                NtsBuf.ReadU16Le(buf, q + 2),
                NtsBuf.ReadU16Le(buf, q + 4)));
        }

        var gridHeaderAt = offset + 9 + count * 6;
        if (gridHeaderAt + 4 > buf.Length)
            throw new InvalidDataException("Ground.MMG: grid header out of range.");

        var gw = NtsBuf.ReadU16Le(buf, gridHeaderAt);
        var gh = NtsBuf.ReadU16Le(buf, gridHeaderAt + 2);
        if (gw < 1 || gh < 1 || gw > 400 || gh > 400)
            throw new InvalidDataException($"Ground.MMG: invalid grid size {gw}×{gh}.");

        var gridOffset = gridHeaderAt + 4;
        var gridBytes = gw * gh;
        if (gridOffset + gridBytes > buf.Length)
            throw new InvalidDataException("Ground.MMG: grid data out of range.");

        var metaOffset = gridOffset + gridBytes;
        var metaLen = size - (metaOffset - offset);
        if (metaLen < 0 || metaOffset + metaLen > buf.Length)
            throw new InvalidDataException("Ground.MMG: metadata bounds invalid.");

        var grid = buf.Slice(gridOffset, gridBytes).ToArray();
        return new NtsMap(imgW, imgH, gw, gh, grid, tiles);
    }
}
