namespace Qhlc.Core.Nts;

public sealed class NtsTableFile
{
    private NtsTableFile(IReadOnlyList<byte[]> rows)
    {
        Rows = rows;
    }

    public IReadOnlyList<byte[]> Rows { get; }

    public static NtsTableFile Open(string path, int recordSize)
    {
        var blob = File.ReadAllBytes(path);
        var rows = NtsContainer.LoadRecords(blob, recordSize);
        return new NtsTableFile(rows);
    }

    /// <summary>Skips index 0 (format version card).</summary>
    public IEnumerable<byte[]> EnumerateDataRows()
    {
        for (var i = 1; i < Rows.Count; i++)
            yield return Rows[i];
    }
}
