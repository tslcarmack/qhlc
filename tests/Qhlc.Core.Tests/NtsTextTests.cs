namespace Qhlc.Core.Tests;

public class NtsTextTests
{
    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void DecodeReversedBig5_ZhangJiao_name_slot()
    {
        // 「張角」Big5 = B1 69 A8 A4；槽宽 10，右对齐，前导 0x00
        var slot = new byte[] { 0, 0, 0, 0, 0, 0, 0xA4, 0xA8, 0x69, 0xB1 };
        Assert.Equal("張角", Qhlc.Core.Nts.NtsText.DecodeReversedBig5(slot));
    }
}
