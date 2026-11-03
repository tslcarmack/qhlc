namespace Qhlc.Player.Public;

public sealed record PartyState(
    IReadOnlyList<int> AllyNpcIds,
    IReadOnlyList<int> EnemyNpcIds);
