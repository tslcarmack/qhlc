using System.Reflection;
using Qhlc.Core.Nts;

namespace Qhlc.Core.Tests;

public class NtsJmaLayerTests
{
    [Fact]
    [Trait("TraceId", "TEST-0006")]
    public void Default_body_layers_exclude_optional_replacements()
    {
        var field = typeof(NtsJma).GetField(
            "BodyLayers",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);

        var layers = (Array)field!.GetValue(null)!;
        var suffixes = new List<string>();
        foreach (var layer in layers)
        {
            var suffix = (string)layer.GetType().GetField("Item1")!.GetValue(layer)!;
            suffixes.Add(suffix);
        }

        Assert.Equal(["", "h", "c", "e", "s", "a"], suffixes);
        Assert.DoesNotContain("b", suffixes);
        Assert.DoesNotContain("k", suffixes);
        Assert.DoesNotContain("bh", suffixes);
        Assert.DoesNotContain("bc", suffixes);
    }

    [Fact]
    [Trait("TraceId", "TEST-0006")]
    public void Walk_up_seq0_synthesizes_without_requiring_002b()
    {
        var root = NtsPaths.ResolveAssetsRoot();
        var jmaDir = NtsPaths.JmaDirectory(root);
        if (!File.Exists(Path.Combine(jmaDir, "002.jma")))
            return;

        // Optional replacement file may exist; default stack must still compose without it.
        Assert.True(NtsJma.TrySynthesizeFrame(root, 2000, seq: 0, frameIndex: 0, out var frame));
        Assert.True(frame.Width > 0 && frame.Height > 0);

        Assert.True(NtsJma.TryGetSeqFrameCount(root, 2000, 0, out var count));
        Assert.True(count >= 2);
        Assert.True(NtsJma.TrySynthesizeFrame(root, 2000, 0, 1, out var frame1));
        Assert.Equal(frame.Width, frame1.Width);
        Assert.Equal(frame.Height, frame1.Height);
    }
}
