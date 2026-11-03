namespace Qhlc.Core.Tests;

public class NtsScrambleTests
{
    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void Unscramble_u16_roundtrip_inverse()
    {
        const uint key = 0xECEA;
        const int bias = 5;
        uint raw = 0x1234;
        var plain = Qhlc.Core.Nts.NtsScramble.Unscramble(raw, 2, key, bias);
        var back = Qhlc.Core.Nts.NtsScramble.Scramble(plain, 2, key, bias);
        Assert.Equal(raw, back);
    }
}
