using Qhlc.Core.Nts;
using Qhlc.Core.Public;

namespace Qhlc.Player.Public;

/// <summary>
/// JMA sequence/frame clock and sticky walk facing (extracted from world map controller).
/// </summary>
public sealed class ActorAnimator
{
    private const float AnimFps = 8f;

    private int _animSeq;
    private int _animFrame;
    private int _animFrameCount = 1;
    private double _animClock;
    private int _walkFacingSeq = 4;

    private string? _assetsRoot;
    private int _bodyBlockId;

    private NtsRaster? _cachedRaster;
    private int _cachedOriginX;
    private int _cachedOriginY;

    public int FrameIndex => _animFrame;

    public void ForceFrameForTest(int frame) => _animFrame = frame;

    /// <summary>Clears synthesized frame cache after equip / appearance change.</summary>
    public void InvalidateAppearanceCache()
    {
        _cachedRaster = null;
        _bodyBlockId = 0;
        _animFrame = 0;
        _animClock = 0;
    }

    /// <summary>
    /// Updates sticky walk facing from a character facing slot 0–7 (screen +Y down).
    /// Does not advance the frame clock; call <see cref="Tick"/> for playback.
    /// </summary>
    public void NotifyFacing(int facingSlot)
    {
        var slot = Math.Clamp(facingSlot, 0, 7);
        var (dirX, dirY) = JmaFacing.OffsetVectors[slot];
        _walkFacingSeq = JmaFacing.StickWalkSeq(_walkFacingSeq, dirX, dirY);
    }

    public void Tick(double dt, Character c)
    {
        if (c.Behavior == BehaviorState.Walk)
            TickWalk(dt);
        else
            TickIdle(dt);
    }

    public bool TryGetFrame(
        string assetsRoot,
        Character c,
        out NtsRaster raster,
        out int originX,
        out int originY)
    {
        var appearance = c.GetAppearanceBlocks();
        _assetsRoot = assetsRoot;
        _bodyBlockId = appearance.BodyBlockId;
        var blockId = _bodyBlockId;
        if (blockId <= 0
            || !NtsJma.TrySynthesizeFrame(
                assetsRoot,
                blockId,
                _animSeq,
                _animFrame,
                out var synthesized,
                out var ox,
                out var oy))
        {
            if (_cachedRaster is not null)
            {
                raster = _cachedRaster;
                originX = _cachedOriginX;
                originY = _cachedOriginY;
                return false;
            }

            raster = null!;
            originX = originY = 0;
            return false;
        }

        _cachedRaster = synthesized;
        _cachedOriginX = ox;
        _cachedOriginY = oy;
        raster = synthesized;
        originX = ox;
        originY = oy;
        return true;
    }

    private void TickWalk(double dt) => AdvanceAnimation(_walkFacingSeq, dt);

    private void TickIdle(double dt)
    {
        var desiredSeq = JmaFacing.StandSeqForWalkSeq(_walkFacingSeq);
        AdvanceAnimation(desiredSeq, dt);
    }

    private void AdvanceAnimation(int desiredSeq, double delta)
    {
        if (desiredSeq != _animSeq)
        {
            var prevWalk = JmaFacing.IsWalkSeq(_animSeq);
            var nextWalk = JmaFacing.IsWalkSeq(desiredSeq);
            _animSeq = desiredSeq;
            RefreshAnimFrameCount();

            if (prevWalk != nextWalk || _animFrameCount <= 1)
            {
                _animFrame = 0;
                _animClock = 0;
            }
            else
                _animFrame %= Math.Max(1, _animFrameCount);

            return;
        }

        if (_animFrameCount <= 1)
            return;

        _animClock += delta;
        var frameDur = 1.0 / AnimFps;
        while (_animClock >= frameDur)
        {
            _animClock -= frameDur;
            _animFrame = (_animFrame + 1) % _animFrameCount;
        }
    }

    private void RefreshAnimFrameCount()
    {
        if (_bodyBlockId <= 0
            || string.IsNullOrEmpty(_assetsRoot)
            || !NtsJma.TryGetSeqFrameCount(_assetsRoot, _bodyBlockId, _animSeq, out var count)
            || count <= 0)
        {
            _animFrameCount = 1;
            return;
        }

        _animFrameCount = count;
    }
}
