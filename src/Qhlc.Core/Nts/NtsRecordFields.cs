namespace Qhlc.Core.Nts;

internal static class NtsRecordFields
{
    public static uint ReadPlainU8(ReadOnlySpan<byte> rec, int offset) => rec[offset];

    public static uint ReadPlainU16(ReadOnlySpan<byte> rec, int offset) =>
        NtsBuf.ReadU16Le(rec, offset);

    public static uint ReadScrambled(ReadOnlySpan<byte> rec, int offset, int width, uint key, int bias)
    {
        uint raw = width switch
        {
            1 => rec[offset],
            2 => NtsBuf.ReadU16Le(rec, offset),
            4 => NtsBuf.ReadU32Le(rec, offset),
            _ => throw new ArgumentOutOfRangeException(nameof(width)),
        };
        return NtsScramble.Unscramble(raw, width, key, bias);
    }

    // Scene.Dat (+0 AreaId)
    public const uint SceneAreaKey = 0xEA6C;
    public const int SceneAreaBias = 9;

    // Npc.Dat combat fields (descramble.json)
    public const uint NpcU8Key = 0xC8;
    public const uint NpcU16Key = 0x5209;
    public const uint NpcU32Key = 0x0BAEB716;
    public const int NpcBias = 1;

    // Skill.Dat (descramble.json)
    public const uint SkillU8Key = 0xFD;
    public const uint SkillU16Key = 0x6EA0;
    public const int SkillBias = 4;
}
