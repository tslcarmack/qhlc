using System.Buffers.Binary;
using System.Text;

namespace Qhlc.Core.Nts;

/// <summary>
/// BMg / JMg container (32-byte directory entries; tsdocs sprites.ts).
/// </summary>
public sealed class NtsArchive
{
    private const int DirEntrySize = 32;

    private readonly byte[] _data;
    private readonly Dictionary<string, (int Offset, int Size)> _entries;

    private NtsArchive(byte[] data, Dictionary<string, (int Offset, int Size)> entries)
    {
        _data = data;
        _entries = entries;
    }

    public static NtsArchive Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Archive not found.", path);

        var data = File.ReadAllBytes(path);
        if (data.Length < 2)
            throw new InvalidDataException("Archive too small.");

        var count = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0, 2));
        if (count == 0)
            throw new InvalidDataException("Archive entry count is zero.");

        var dirEnd = 2 + DirEntrySize * count;
        if (dirEnd > data.Length)
            throw new InvalidDataException("Archive directory extends past file end.");

        var entries = new Dictionary<string, (int Offset, int Size)>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var p = 2 + i * DirEntrySize;
            var nameLen = data[p];
            if (nameLen is < 1 or > 23)
                continue;

            var name = Encoding.Latin1.GetString(data, p + 1, nameLen);
            var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p + 24));
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p + 28));
            if (size < 0 || offset < 0 || offset + size > data.Length)
                continue;

            entries.TryAdd(name, (offset, size));
        }

        return new NtsArchive(data, entries);
    }

    public bool TryReadEntry(string entryName, out byte[] payload)
    {
        payload = Array.Empty<byte>();
        if (string.IsNullOrEmpty(entryName))
            return false;

        if (!_entries.TryGetValue(entryName, out var e))
            return false;

        payload = new byte[e.Size];
        Buffer.BlockCopy(_data, e.Offset, payload, 0, e.Size);
        return true;
    }

    public bool ContainsEntry(string entryName) =>
        !string.IsNullOrEmpty(entryName) && _entries.ContainsKey(entryName);
}
