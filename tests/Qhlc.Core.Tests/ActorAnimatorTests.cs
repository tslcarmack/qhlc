using Qhlc.Inventory.Public;
using Qhlc.Player.Public;

public class ActorAnimatorTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Stick_facing_keeps_walk_frame_phase()
    {
        var anim = new ActorAnimator();
        var cat = new StubEquipCatalog();
        var c = new PlayerCharacter(1, "t", 2000, new CharacterStats(10, 10, 10, 10, 1, 1, 1, 1, 0), cat);
        c.Behavior = BehaviorState.Walk;
        c.Facing = 6;
        anim.NotifyFacing(6);
        anim.ForceFrameForTest(2);
        anim.NotifyFacing(6);
        Assert.Equal(2, anim.FrameIndex);
    }
}
