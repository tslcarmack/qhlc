namespace Qhlc.Core.Nts;

/// <summary>
/// .Dat container load and TDataEnCode record-level XOR (tsotool container.ts + decode.ts).
/// </summary>
public static class NtsContainer
{
    public const int FirstEncryptedRecord = 1;

    public static void XorPayload(
        Span<byte> payload,
        int recordSize,
        ReadOnlySpan<byte> key1,
        ReadOnlySpan<byte> key2,
        int startRecord = FirstEncryptedRecord)
    {
        if (recordSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(recordSize));
        if (key1.Length != recordSize || key2.Length != recordSize)
            throw new ArgumentException("Key length must equal record size.");
        if (payload.Length % recordSize != 0)
            throw new ArgumentException("Payload length must be a multiple of record size.");

        var count = payload.Length / recordSize;
        for (var k = Math.Max(startRecord, 0); k < count; k++)
        {
            var key = (k & 1) == 1 ? key2 : key1;
            var baseOffset = k * recordSize;
            for (var j = 0; j < recordSize; j++)
                payload[baseOffset + j] ^= key[j];
        }
    }

    public static byte[][] SplitRecords(ReadOnlySpan<byte> blob, int recordSize)
    {
        if (recordSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(recordSize));
        if (blob.Length % recordSize != 0)
            throw new InvalidDataException(
                $"File {blob.Length} bytes is not an integer multiple of record size {recordSize}.");

        var count = blob.Length / recordSize;
        var rows = new byte[count][];
        for (var i = 0; i < count; i++)
        {
            rows[i] = blob.Slice(i * recordSize, recordSize).ToArray();
        }

        return rows;
    }

    public static byte[][] LoadRecords(ReadOnlySpan<byte> blob, int recordSize)
    {
        if (TryLoadHeadered(blob, recordSize, out var headered))
            return headered;

        if (blob.Length % recordSize != 0)
            throw new InvalidDataException(
                $"File {blob.Length} bytes is not an integer multiple of record size {recordSize}.");

        var total = blob.Length / recordSize;
        if (total >= 3 && LooksLikeEncryptedBlob(blob, recordSize))
        {
            var payloadCount = total - 2;
            var payload = blob.Slice(0, payloadCount * recordSize).ToArray();
            var key1 = blob.Slice(payloadCount * recordSize, recordSize);
            var key2 = blob.Slice((payloadCount + 1) * recordSize, recordSize);
            XorPayload(payload, recordSize, key1, key2);
            return SplitRecords(payload, recordSize);
        }

        return SplitRecords(blob, recordSize);
    }

    private static bool TryLoadHeadered(ReadOnlySpan<byte> blob, int recordSize, out byte[][] rows)
    {
        rows = Array.Empty<byte[]>();
        if (blob.Length < 8)
            return false;

        var declared = (int)NtsBuf.ReadU32Le(blob, 0);
        if (declared != recordSize)
            return false;

        var payloadCount = (int)NtsBuf.ReadU32Le(blob, 4) + 1;
        var need = 8 + (payloadCount + 2) * recordSize;
        if (need != blob.Length)
            return false;

        var payload = blob.Slice(8, payloadCount * recordSize).ToArray();
        var key1 = blob.Slice(8 + payloadCount * recordSize, recordSize);
        var key2 = blob.Slice(8 + (payloadCount + 1) * recordSize, recordSize);
        XorPayload(payload, recordSize, key1, key2);
        rows = SplitRecords(payload, recordSize);
        return true;
    }

    /// <summary>Encrypted blob: row 0 is a zero version card; last two rows are keys.</summary>
    private static bool LooksLikeEncryptedBlob(ReadOnlySpan<byte> blob, int recordSize)
    {
        var total = blob.Length / recordSize;
        if (total < 3)
            return false;

        var row0 = blob.Slice(0, recordSize);
        if (!IsSparseZeroRow(row0))
            return false;

        var key1 = blob.Slice((total - 2) * recordSize, recordSize);
        var key2 = blob.Slice((total - 1) * recordSize, recordSize);
        return KeyEntropy(key1) >= 64 && KeyEntropy(key2) >= 64;
    }

    private static bool IsSparseZeroRow(ReadOnlySpan<byte> row)
    {
        var nonZero = 0;
        foreach (var b in row)
        {
            if (b != 0 && ++nonZero > 2)
                return false;
        }

        return true;
    }

    private static int KeyEntropy(ReadOnlySpan<byte> key)
    {
        Span<byte> seen = stackalloc byte[256];
        var distinct = 0;
        foreach (var b in key)
        {
            if (seen[b] == 0)
            {
                seen[b] = 1;
                distinct++;
            }
        }

        return distinct;
    }
}
