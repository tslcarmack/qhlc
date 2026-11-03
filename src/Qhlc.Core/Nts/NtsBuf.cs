using System.Buffers.Binary;

namespace Qhlc.Core.Nts;

public static class NtsBuf
{
    public static ushort ReadU16Le(ReadOnlySpan<byte> s, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(offset));

    public static uint ReadU32Le(ReadOnlySpan<byte> s, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset));
}
