using System;
using System.Collections.Generic;
using Godot;
using Qhlc.Core.Nts;
using Qhlc.Player.Public;
using Qhlc.World.Public;

namespace Qhlc.Game;

/// <summary>
/// Player-side click intent → A* → <see cref="PlayerMover"/> → updates <see cref="PlayerCharacter"/> foot/facing/behavior.
/// </summary>
public sealed class PlayerPathDriver
{
    private const float MoveSpeedPixelsPerSecond = 120f;

    private readonly List<(int X, int Y)> _pathWaypoints = new();
    private int _pathIndex;
    private int _lastGoalGx = int.MinValue;
    private int _lastGoalGy = int.MinValue;

    public void ClearPath()
    {
        _pathWaypoints.Clear();
        _pathIndex = 0;
        _lastGoalGx = int.MinValue;
        _lastGoalGy = int.MinValue;
    }

    public void TrySetTargetFromWorldPosition(WorldSession session, Vector2 world)
    {
        var tx = Math.Clamp((int)Math.Round(world.X), 0, Math.Max(0, session.Map.ImgW - 1));
        var ty = Math.Clamp((int)Math.Round(world.Y), 0, Math.Max(0, session.Map.ImgH - 1));
        TrySetTargetFromPixel(session, tx, ty);
    }

    public void TrySetTargetFromPixel(WorldSession session, int tx, int ty)
    {
        var (goalGx, goalGy) = NtsMap.PixelToCell(tx, ty);
        if (!GridAStar.TryFindNearestWalkable(session.Map, goalGx, goalGy, out goalGx, out goalGy))
            return;

        if (goalGx == _lastGoalGx && goalGy == _lastGoalGy && _pathWaypoints.Count > 0)
            return;

        var (startGx, startGy) = NtsMap.PixelToCell(session.PlayerX, session.PlayerY);
        if (!session.Map.IsCellWalkable(startGx, startGy)
            && !GridAStar.TryFindNearestWalkable(session.Map, startGx, startGy, out startGx, out startGy))
            return;

        if (!GridAStar.TryFindPath(session.Map, startGx, startGy, goalGx, goalGy, out var cells))
            return;

        _pathWaypoints.Clear();
        foreach (var (gx, gy) in cells)
        {
            var center = NtsMap.CellCenter(gx, gy);
            _pathWaypoints.Add(center);
        }

        _pathIndex = 0;
        while (_pathIndex < _pathWaypoints.Count
               && Dist2(session.PlayerX, session.PlayerY, _pathWaypoints[_pathIndex]) <= 4)
            _pathIndex++;

        _lastGoalGx = goalGx;
        _lastGoalGy = goalGy;
    }

    /// <returns>True while the player is actively walking a path.</returns>
    public bool TickMove(double delta, WorldSession session)
    {
        var player = session.Player;
        if (_pathIndex >= _pathWaypoints.Count)
        {
            player.Behavior = BehaviorState.Idle;
            return false;
        }

        var (tx, ty) = _pathWaypoints[_pathIndex];
        var dx = tx - session.PlayerX;
        var dy = ty - session.PlayerY;
        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
        {
            _pathIndex++;
            if (_pathIndex >= _pathWaypoints.Count)
            {
                ClearPath();
                player.Behavior = BehaviorState.Idle;
                return false;
            }

            (tx, ty) = _pathWaypoints[_pathIndex];
            dx = tx - session.PlayerX;
            dy = ty - session.PlayerY;
        }

        var dir = new Vector2(dx, dy);
        if (dir.LengthSquared() < 0.01f)
        {
            player.Behavior = BehaviorState.Idle;
            return false;
        }

        var aim = dir.Normalized();
        var step = Math.Max(1, (int)Math.Round(MoveSpeedPixelsPerSecond * (float)delta));
        var stepX = Math.Clamp((int)Math.Round(aim.X * step), -step, step);
        var stepY = Math.Clamp((int)Math.Round(aim.Y * step), -step, step);
        if (Math.Abs(stepX) > Math.Abs(dx))
            stepX = dx;
        if (Math.Abs(stepY) > Math.Abs(dy))
            stepY = dy;

        var beforeX = session.PlayerX;
        var beforeY = session.PlayerY;
        var (nextX, nextY) = PlayerMover.TryMove(session.Map, beforeX, beforeY, stepX, stepY);
        session.PlayerX = nextX;
        session.PlayerY = nextY;

        if (nextX == beforeX && nextY == beforeY)
        {
            ClearPath();
            player.Behavior = BehaviorState.Idle;
            return false;
        }

        var movedX = nextX - beforeX;
        var movedY = nextY - beforeY;
        if (movedX != 0 || movedY != 0)
        {
            var walkSeq = JmaFacing.WalkSeqForDirection(movedX, movedY);
            player.Facing = walkSeq - JmaFacing.WalkBase;
        }

        player.Behavior = BehaviorState.Walk;
        return true;
    }

    private static int Dist2(int x0, int y0, (int X, int Y) p)
    {
        var dx = x0 - p.X;
        var dy = y0 - p.Y;
        return dx * dx + dy * dy;
    }
}
