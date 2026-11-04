namespace Qhlc.Player.Public;

public sealed class Party
{
    public Party(PlayerCharacter leader, IReadOnlyList<Character> allies)
    {
        Leader = leader ?? throw new ArgumentNullException(nameof(leader));
        Allies = allies ?? throw new ArgumentNullException(nameof(allies));
    }

    public PlayerCharacter Leader { get; }
    public IReadOnlyList<Character> Allies { get; }

    public IEnumerable<Character> Members
    {
        get
        {
            yield return Leader;
            foreach (var ally in Allies)
                yield return ally;
        }
    }
}
