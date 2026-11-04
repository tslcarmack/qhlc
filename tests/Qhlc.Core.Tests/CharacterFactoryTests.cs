using Qhlc.Core.Public;
using Qhlc.Inventory.Public;
using Qhlc.Player.Public;

public class CharacterFactoryTests
{
    private static ResolvedContent TestContent() =>
        new(
            10851,
            "test",
            new[] { 10001, 10002, 10003 },
            new[] { 10206, 10209 },
            new Dictionary<int, int>
            {
                [10001] = 2000,
                [10002] = 2000,
                [10003] = 2000,
                [10206] = 2000,
                [10209] = 2000,
            });

    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Factory_builds_three_allies()
    {
        var party = CharacterFactory.CreateParty(TestContent(), new StubEquipCatalog());
        Assert.Equal(3, party.Members.Count());
        Assert.Equal(10001, party.Leader.Id);
    }

    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Factory_places_map_npcs_at_positions()
    {
        var content = TestContent();
        var positions = new List<(int X, int Y)> { (100, 200), (300, 400) };
        var npcs = CharacterFactory.CreateMapNpcs(content, new StubEquipCatalog(), positions);
        Assert.Equal(2, npcs.Count);
        Assert.Equal(10206, npcs[0].Id);
        Assert.Equal(100, npcs[0].FootX);
        Assert.Equal(200, npcs[0].FootY);
        Assert.Equal(10209, npcs[1].Id);
    }
}
