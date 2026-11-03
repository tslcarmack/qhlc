namespace Qhlc.Core.Nts;

public static class NtsScramble
{
    public static uint Unscramble(uint raw, int width, uint key, int bias)
    {
        uint mask = width switch
        {
            1 => 0xffu,
            2 => 0xffffu,
            4 => 0xffffffffu,
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };
        uint mod = width switch
        {
            1 => 0x100u,
            2 => 0x10000u,
            4 => 0u,
            _ => 0,
        };
        uint x = (raw ^ (key & mask)) & mask;
        if (width == 4)
        {
            ulong y = (x + (ulong)(0x100000000L - bias)) & 0xffffffffUL;
            return (uint)y;
        }

        return (uint)((x + (mod - (uint)bias)) % mod) & mask;
    }

    public static uint Scramble(uint value, int width, uint key, int bias)
    {
        uint mask = width switch
        {
            1 => 0xffu,
            2 => 0xffffu,
            4 => 0xffffffffu,
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };
        long mod = width switch
        {
            1 => 0x100,
            2 => 0x10000,
            4 => 0x100000000L,
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };

        long v = ((long)(value & mask) + bias) % mod;
        if (v < 0)
            v += mod;

        return (uint)(((ulong)v ^ (key & mask)) & mask);
    }
}
