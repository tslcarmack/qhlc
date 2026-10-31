using System.Text.Json;
using Qhlc.Save.Public;
using Xunit;

namespace Qhlc.Integration.Tests;

public class CriticalPathTests
{
    [Trait("TraceId", "TEST-0001")]
    [Fact]
    public void SaveSnapshot_roundtrip_placeholder()
    {
        var snapshot = new SaveSnapshot { SaveVersion = 1, PlayerId = "player-1" };
        var json = JsonSerializer.Serialize(snapshot);
        var restored = JsonSerializer.Deserialize<SaveSnapshot>(json);
        Assert.NotNull(restored);
        Assert.Equal(1, restored.SaveVersion);
        Assert.Equal("player-1", restored.PlayerId);
    }
}
