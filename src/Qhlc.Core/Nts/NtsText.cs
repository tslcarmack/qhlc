using System.Text;

namespace Qhlc.Core.Nts;

public static class NtsText
{
    private static readonly Encoding Big5;

    static NtsText()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Big5 = Encoding.GetEncoding(
            950,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    public static string DecodeReversedBig5(ReadOnlySpan<byte> field)
    {
        if (field.IsEmpty)
            return string.Empty;

        var reversed = field.ToArray();
        Array.Reverse(reversed);

        var end = reversed.Length;
        while (end > 0 && reversed[end - 1] == 0)
            end--;

        if (end == 0)
            return string.Empty;

        return DecodeBig5Strict(reversed.AsSpan(0, end));
    }

    /// <summary>Npc.Dat name: byte +0 = Big5 byte length; bytes +1..+10 reversed Big5 slot.</summary>
    public static string DecodeNpcName(ReadOnlySpan<byte> rec97)
    {
        if (rec97.Length < 11)
            return string.Empty;

        var n = rec97[0];
        if (n == 0)
            return string.Empty;

        if (n > 10)
            n = 10;

        var nameBytes = rec97.Slice(11 - n, n).ToArray();
        Array.Reverse(nameBytes);
        return DecodeBig5Strict(nameBytes);
    }

    /// <summary>Skill.Dat name: +0 length; +1..+20 reversed Big5 slot.</summary>
    public static string DecodeSkillName(ReadOnlySpan<byte> rec86)
    {
        if (rec86.Length < 21)
            return string.Empty;

        var n = rec86[0];
        if (n == 0)
            return string.Empty;

        if (n > 20)
            n = 20;

        var slotEnd = 21;
        var nameBytes = rec86.Slice(slotEnd - n, n).ToArray();
        Array.Reverse(nameBytes);
        return DecodeBig5Strict(nameBytes);
    }

    /// <summary>Forward fixed Big5 field (Scene.Dat name +4..+23).</summary>
    public static string DecodeFixedBig5(ReadOnlySpan<byte> field)
    {
        if (field.IsEmpty)
            return string.Empty;

        var end = field.Length;
        while (end > 0 && field[end - 1] == 0)
            end--;

        if (end == 0)
            return string.Empty;

        return DecodeBig5Strict(field.Slice(0, end));
    }

    private static string DecodeBig5Strict(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return Big5.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return string.Empty;
        }
    }
}
