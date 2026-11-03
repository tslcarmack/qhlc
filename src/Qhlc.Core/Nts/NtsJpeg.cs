namespace Qhlc.Core.Nts;

/// <summary>
/// JPEG payloads from archives are returned as compressed bytes; decoding happens in Godot (Image.LoadJpgFromBuffer).
/// </summary>
public static class NtsJpeg
{
    public static bool IsJpeg(ReadOnlySpan<byte> data) =>
        data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
}
