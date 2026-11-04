using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;
using Qhlc.Core.Nts;
using Qhlc.Core.Public;
using Qhlc.Inventory.Public;
using Qhlc.Player.Public;
using Qhlc.World.Public;

namespace Qhlc.Game;

public partial class WorldMapController : Node2D
{
    private const float MoveSpeedPixelsPerSecond = 120f;
    private const int HitboxPixels = NtsMap.CellPixels;
    private const float AnimFps = 8f;
    /// <summary>002-family measured foot line above entity origin (tsdocs §05.1).</summary>
    private const int FootOffsetY = 29;

    private static readonly Regex MapArchivePattern = new(
        @"^map.*\.(jmg|bmg)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public string AssetsRoot { get; set; } = "";
    public ResolvedContent? ResolvedContent { get; set; }
    public WorldSession? RestoredSession { get; set; }

    private WorldSession? _session;

    public WorldSession? ExportSession() => _session;

    private Node2D? _sortedRoot;
    private Node2D? _playerRoot;
    private Sprite2D? _playerSprite;
    private Camera2D? _camera;
    private readonly List<Area2D> _enemyAreas = new();
    private readonly List<Node2D> _enemyRoots = new();
    private int _playerSpriteW;
    private int _playerSpriteH;
    private int _playerOriginX;
    private int _playerOriginY;
    private bool _battleTriggered;

    private int _playerBlockId;
    private int _animSeq;
    private int _animFrame;
    private int _animFrameCount = 1;
    private double _animClock;
    private int _walkFacingSeq = 4; // last walk slot 0–7
    private Vector2 _lastMoveDir = new(0, 1);
    private readonly List<(int X, int Y)> _pathWaypoints = new();
    private int _pathIndex;
    private int _lastGoalGx = int.MinValue;
    private int _lastGoalGy = int.MinValue;

    public override void _Ready()
    {
        _sortedRoot = GetNode<Node2D>("Sorted");
        _playerRoot = GetNode<Node2D>("Sorted/Player");
        _playerSprite = GetNode<Sprite2D>("Sorted/Player/Sprite");
        _camera = GetNode<Camera2D>("Camera2D");
        _camera.MakeCurrent();

        if (ResolvedContent is null || string.IsNullOrEmpty(AssetsRoot))
        {
            GD.PrintErr("WorldMap: missing ResolvedContent or AssetsRoot.");
            return;
        }

        try
        {
            BuildWorld(ResolvedContent);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WorldMap: build failed: {ex}");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_session is null || _battleTriggered)
            return;

        // Click to walk (LMB or RMB — hold repaths in _Process).
        if (@event is InputEventMouseButton { Pressed: true } mb
            && (mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right))
        {
            SetMoveTargetFromMouse();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_session is null || _playerSprite is null || _battleTriggered)
            return;

        // Hold mouse: repath only when the cursor enters another cell (SetMoveTarget early-outs).
        if (Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsMouseButtonPressed(MouseButton.Right))
            SetMoveTargetFromMouse();

        var moving = StepAlongPath(delta);

        if (moving)
            UpdateWalkAnimation(delta);
        else
            UpdateStandAnimation(delta);

        SyncPlayerVisual();

        if (!_session.TryEncounter())
            return;

        _battleTriggered = true;
        ClearPath();
        _session.MarkEncounterConsumed();
        GetParent<GameMain>()?.EnterBattle();
    }

    private void SetMoveTargetFromMouse()
    {
        if (_session is null)
            return;

        var world = GetGlobalMousePosition();
        var tx = Math.Clamp((int)Math.Round(world.X), 0, Math.Max(0, _session.Map.ImgW - 1));
        var ty = Math.Clamp((int)Math.Round(world.Y), 0, Math.Max(0, _session.Map.ImgH - 1));
        var (goalGx, goalGy) = NtsMap.PixelToCell(tx, ty);
        if (!GridAStar.TryFindNearestWalkable(_session.Map, goalGx, goalGy, out goalGx, out goalGy))
            return;

        if (goalGx == _lastGoalGx && goalGy == _lastGoalGy && _pathWaypoints.Count > 0)
            return;

        var (startGx, startGy) = NtsMap.PixelToCell(_session.PlayerX, _session.PlayerY);
        if (!_session.Map.IsCellWalkable(startGx, startGy)
            && !GridAStar.TryFindNearestWalkable(_session.Map, startGx, startGy, out startGx, out startGy))
            return;

        if (!GridAStar.TryFindPath(_session.Map, startGx, startGy, goalGx, goalGy, out var cells))
            return;

        _pathWaypoints.Clear();
        foreach (var (gx, gy) in cells)
        {
            var center = NtsMap.CellCenter(gx, gy);
            _pathWaypoints.Add(center);
        }

        // Skip the cell we're already standing in.
        _pathIndex = 0;
        while (_pathIndex < _pathWaypoints.Count
               && Dist2(_session.PlayerX, _session.PlayerY, _pathWaypoints[_pathIndex]) <= 4)
            _pathIndex++;

        _lastGoalGx = goalGx;
        _lastGoalGy = goalGy;
    }

    private bool StepAlongPath(double delta)
    {
        if (_session is null || _pathIndex >= _pathWaypoints.Count)
            return false;

        var (tx, ty) = _pathWaypoints[_pathIndex];
        var dx = tx - _session.PlayerX;
        var dy = ty - _session.PlayerY;
        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
        {
            _pathIndex++;
            if (_pathIndex >= _pathWaypoints.Count)
            {
                ClearPath();
                return false;
            }

            (tx, ty) = _pathWaypoints[_pathIndex];
            dx = tx - _session.PlayerX;
            dy = ty - _session.PlayerY;
        }

        var dir = new Vector2(dx, dy);
        if (dir.LengthSquared() < 0.01f)
            return false;

        var aim = dir.Normalized();
        var step = Math.Max(1, (int)Math.Round(MoveSpeedPixelsPerSecond * (float)delta));
        var stepX = Math.Clamp((int)Math.Round(aim.X * step), -step, step);
        var stepY = Math.Clamp((int)Math.Round(aim.Y * step), -step, step);
        if (Math.Abs(stepX) > Math.Abs(dx))
            stepX = dx;
        if (Math.Abs(stepY) > Math.Abs(dy))
            stepY = dy;

        var beforeX = _session.PlayerX;
        var beforeY = _session.PlayerY;
        var (nextX, nextY) = PlayerMover.TryMove(_session.Map, beforeX, beforeY, stepX, stepY);
        _session.PlayerX = nextX;
        _session.PlayerY = nextY;

        if (nextX == beforeX && nextY == beforeY)
        {
            ClearPath();
            return false;
        }

        // Face actual displacement (not raw waypoint vector) to avoid octant chatter.
        var movedX = nextX - beforeX;
        var movedY = nextY - beforeY;
        if (movedX != 0 || movedY != 0)
            _lastMoveDir = new Vector2(movedX, movedY).Normalized();

        return true;
    }

    private void ClearPath()
    {
        _pathWaypoints.Clear();
        _pathIndex = 0;
        _lastGoalGx = int.MinValue;
        _lastGoalGy = int.MinValue;
    }

    private static int Dist2(int x0, int y0, (int X, int Y) p)
    {
        var dx = x0 - p.X;
        var dy = y0 - p.Y;
        return dx * dx + dy * dy;
    }

    private void BuildWorld(ResolvedContent content)
    {
        var map = NtsMap.LoadFromGround(AssetsRoot, content.AreaId);
        var mapRoot = GetNode<Node2D>("MapTiles");
        foreach (var child in mapRoot.GetChildren())
            child.QueueFree();

        LoadMapTiles(map, mapRoot);
        ClearSortedActors(keepPlayer: true);
        LoadMapObjects(map);
        ConfigureCamera(map);

        if (RestoredSession is not null && RestoredSession.Content.AreaId == content.AreaId)
        {
            _session = RestoredSession;
            RestoredSession = null;
        }
        else
        {
            var spawn = PickPlayerSpawn(map);
            var enemyPositions = PickEnemyPositions(map, spawn, ContentResolver.EnemyCount);
            var catalog = new StubEquipCatalog();
            var party = CharacterFactory.CreateParty(content, catalog);
            var mapNpcs = CharacterFactory.CreateMapNpcs(content, catalog, enemyPositions);
            party.Leader.FootX = spawn.X;
            party.Leader.FootY = spawn.Y;
            _session = new WorldSession(content, map, party.Leader, party, mapNpcs);
            GD.Print($"WorldMap: AreaId={content.AreaId} size={map.ImgW}x{map.ImgH} objects={map.Objects.Count} spawn=({spawn.X},{spawn.Y}) enemies={enemyPositions.Count}");
        }

        if (content.AllyNpcIds.Count > 0
            && content.NpcIdToBlockId.TryGetValue(content.AllyNpcIds[0], out var blockId))
        {
            _playerBlockId = blockId;
        }

        _walkFacingSeq = JmaFacing.WalkSeqForDirection(_lastMoveDir.X, _lastMoveDir.Y);
        _animSeq = JmaFacing.StandSeqForWalkSeq(_walkFacingSeq);
        RefreshAnimFrameCount();
        RefreshPlayerFrame();
        SyncPlayerVisual();
        SpawnEnemies(content, _session.EnemyPositions);
    }

    private void ConfigureCamera(NtsMap map)
    {
        // 1:1 pixels — never scale map/actors. Camera scrolls with the player and
        // clamps so we don't show empty outside the map when near edges.
        _camera!.Zoom = Vector2.One;
        _camera.LimitEnabled = true;
        _camera.LimitLeft = 0;
        _camera.LimitTop = 0;
        _camera.LimitRight = Math.Max(map.ImgW, 1);
        _camera.LimitBottom = Math.Max(map.ImgH, 1);
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 10f;
    }

    private void UpdateWalkAnimation(double delta)
    {
        _walkFacingSeq = JmaFacing.StickWalkSeq(_walkFacingSeq, _lastMoveDir.X, _lastMoveDir.Y);
        AdvanceAnimation(_walkFacingSeq, delta);
    }

    private void UpdateStandAnimation(double delta)
    {
        // Keep last walk facing while idle; do not re-quantize from a stale dir each frame.
        var desiredSeq = JmaFacing.StandSeqForWalkSeq(_walkFacingSeq);
        AdvanceAnimation(desiredSeq, delta);
    }

    private void AdvanceAnimation(int desiredSeq, double delta)
    {
        if (desiredSeq != _animSeq)
        {
            var prevWalk = JmaFacing.IsWalkSeq(_animSeq);
            var nextWalk = JmaFacing.IsWalkSeq(desiredSeq);
            _animSeq = desiredSeq;
            RefreshAnimFrameCount();

            // Walk↔stand: restart cycle. Walk facing change: keep phase so the stride continues.
            if (prevWalk != nextWalk || _animFrameCount <= 1)
            {
                _animFrame = 0;
                _animClock = 0;
            }
            else
                _animFrame %= Math.Max(1, _animFrameCount);

            RefreshPlayerFrame();
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
            RefreshPlayerFrame();
        }
    }

    private void RefreshAnimFrameCount()
    {
        if (_playerBlockId <= 0
            || !NtsJma.TryGetSeqFrameCount(AssetsRoot, _playerBlockId, _animSeq, out var count)
            || count <= 0)
        {
            _animFrameCount = 1;
            return;
        }

        _animFrameCount = count;
    }

    private void RefreshPlayerFrame()
    {
        if (_playerSprite is null || _playerBlockId <= 0)
            return;

        if (!NtsJma.TrySynthesizeFrame(
                AssetsRoot,
                _playerBlockId,
                _animSeq,
                _animFrame,
                out var raster,
                out var originX,
                out var originY))
        {
            // Keep last good texture — do not snap to seq0 (causes direction flash).
            return;
        }

        _playerOriginX = originX;
        _playerOriginY = originY;
        ApplySpriteTexture(_playerSprite, RasterToTexture(raster), raster.Width, raster.Height, isPlayer: true);
    }

    private void LoadMapTiles(NtsMap map, Node2D parent)
    {
        foreach (var tile in map.Tiles)
        {
            if (!TryReadMapJpeg(AssetsRoot, tile.Scene, out var jpeg))
            {
                GD.Print($"WorldMap: missing map jpeg scene={tile.Scene}");
                continue;
            }

            Godot.Image image = new();
            if (image.LoadJpgFromBuffer(jpeg) != Error.Ok)
            {
                GD.Print($"WorldMap: jpeg decode failed scene={tile.Scene}");
                continue;
            }

            var sprite = new Sprite2D
            {
                Texture = ImageTexture.CreateFromImage(image),
                Position = new Vector2(tile.X, tile.Y),
                Centered = false,
            };
            parent.AddChild(sprite);
        }
    }

    private void ClearSortedActors(bool keepPlayer)
    {
        if (_sortedRoot is null)
            return;

        foreach (var child in _sortedRoot.GetChildren())
        {
            if (keepPlayer && ReferenceEquals(child, _playerRoot))
                continue;
            child.QueueFree();
        }

        _enemyAreas.Clear();
        _enemyRoots.Clear();
    }

    /// <summary>
    /// Meta 段2 overlays share Y-sort with actors so canopy/tree tops occlude correctly (§05.2).
    /// Sort key = object bottom edge (py + frameH); blit origin stays (px, py).
    /// </summary>
    private void LoadMapObjects(NtsMap map)
    {
        if (_sortedRoot is null)
            return;

        var gfx = new NtsMapObjectGfx(AssetsRoot);
        var loaded = 0;
        // Child order preserves meta array order for equal-Y ties (§03.6 pit 3).
        for (var i = 0; i < map.Objects.Count; i++)
        {
            var obj = map.Objects[i];
            if (!gfx.TryLoad(obj.Id, out var raster))
                continue;

            var root = new Node2D
            {
                Name = $"Obj_{obj.Id}_{i}",
                // Y-sort key = bottom edge of the overlay; blit stays at (px, py).
                Position = new Vector2(obj.X, obj.Y + raster.Height),
            };
            var sprite = new Sprite2D
            {
                Centered = false,
                Position = new Vector2(0, -raster.Height),
                Texture = RasterToTexture(raster),
            };
            root.AddChild(sprite);
            _sortedRoot.AddChild(root);
            loaded++;
        }

        // Player must stay among Sorted children for Y-sort vs overlays.
        if (_playerRoot is not null && _playerRoot.GetParent() == _sortedRoot)
            _sortedRoot.MoveChild(_playerRoot, _sortedRoot.GetChildCount() - 1);

        GD.Print($"WorldMap: loaded {loaded}/{map.Objects.Count} map objects");
    }

    private void SpawnEnemies(ResolvedContent content, IReadOnlyList<(int X, int Y)> positions)
    {
        foreach (var old in _enemyRoots)
            old.QueueFree();
        _enemyRoots.Clear();
        _enemyAreas.Clear();

        if (_sortedRoot is null)
            return;

        for (var i = 0; i < positions.Count; i++)
        {
            var (ex, ey) = positions[i];
            var enemyRoot = new Node2D { Name = $"Enemy_{i}" };

            Sprite2D sprite;
            int sw = HitboxPixels, sh = HitboxPixels;
            if (TryLoadActorSprite(content, isPlayer: false, out var tex, out var w, out var h, enemyIndex: i))
            {
                sprite = new Sprite2D { Centered = false };
                ApplySpriteTexture(sprite, tex, w, h, isPlayer: false);
                sw = w;
                sh = h;
            }
            else
            {
                sprite = new Sprite2D
                {
                    Texture = PlaceholderTexture(Colors.IndianRed),
                    Centered = false,
                };
            }

            // Root at foot for Y-sort; sprite hung above.
            enemyRoot.Position = new Vector2(ex, ey);
            sprite.Position = new Vector2(-sw * 0.5f, -sh);
            var area = new Area2D();
            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(HitboxPixels, HitboxPixels) },
                Position = new Vector2(0, -HitboxPixels * 0.5f),
            };
            area.AddChild(shape);
            enemyRoot.AddChild(sprite);
            enemyRoot.AddChild(area);
            _sortedRoot.AddChild(enemyRoot);
            _enemyRoots.Add(enemyRoot);
            _enemyAreas.Add(area);
        }
    }

    private bool TryLoadActorSprite(
        ResolvedContent content,
        bool isPlayer,
        out ImageTexture texture,
        out int width,
        out int height,
        int enemyIndex = 0)
    {
        texture = null!;
        width = height = 0;

        int npcId;
        if (isPlayer)
        {
            if (content.AllyNpcIds.Count == 0)
                return false;
            npcId = content.AllyNpcIds[0];
        }
        else
        {
            if (enemyIndex >= content.EnemyNpcIds.Count)
                return false;
            npcId = content.EnemyNpcIds[enemyIndex];
        }

        if (!content.NpcIdToBlockId.TryGetValue(npcId, out var blockId))
            return false;

        if (!NtsJma.TrySynthesizeFrame(AssetsRoot, blockId, seq: 0, frameIndex: 0, out var raster))
            return false;

        texture = RasterToTexture(raster);
        width = raster.Width;
        height = raster.Height;
        return true;
    }

    private void SyncPlayerVisual()
    {
        if (_session is null || _playerRoot is null || _playerSprite is null)
            return;

        // Root Y = foot → participates in Sorted Y-sort with map objects.
        _playerRoot.Position = new Vector2(_session.PlayerX, _session.PlayerY);
        // Entity origin is FootOffsetY below the foot (tsdocs §05.1); raster origin is that point.
        if (_playerSpriteW > 0 && _playerSpriteH > 0)
        {
            _playerSprite.Position = new Vector2(
                -_playerOriginX,
                FootOffsetY - _playerOriginY);
        }

        // Camera follows near the torso, not sprite top-left.
        _camera!.Position = new Vector2(_session.PlayerX, _session.PlayerY - FootOffsetY);
    }

    private static (int X, int Y, int W, int H) PlayerHitbox(int footX, int footY) =>
        (footX - HitboxPixels / 2, footY - HitboxPixels / 2, HitboxPixels, HitboxPixels);

    private void ApplySpriteTexture(Sprite2D sprite, ImageTexture texture, int width, int height, bool isPlayer)
    {
        sprite.Texture = texture;
        sprite.Centered = false;
        if (isPlayer)
        {
            _playerSpriteW = width;
            _playerSpriteH = height;
        }
    }

    private static ImageTexture RasterToTexture(NtsRaster raster)
    {
        var image = Godot.Image.CreateFromData(
            raster.Width,
            raster.Height,
            false,
            Godot.Image.Format.Rgba8,
            raster.Rgba);
        return ImageTexture.CreateFromImage(image);
    }

    private static ImageTexture PlaceholderTexture(Color color)
    {
        var image = Godot.Image.CreateEmpty(HitboxPixels, HitboxPixels, false, Godot.Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

    private static (int X, int Y) PickPlayerSpawn(NtsMap map)
    {
        // Prefer a walkable cell near the map center so the camera is not glued to a corner
        // (corner spawns make half the viewport show empty clear-color).
        var cells = new List<(int Gx, int Gy)>();
        foreach (var cell in EnumerateWalkableCells(map))
            cells.Add(cell);

        if (cells.Count == 0)
            return (Math.Max(0, map.ImgW / 2), Math.Max(0, map.ImgH / 2));

        var cx = (map.Gw - 1) / 2.0;
        var cy = (map.Gh - 1) / 2.0;
        var best = cells[0];
        var bestDist = double.MaxValue;
        foreach (var c in cells)
        {
            var d = (c.Gx - cx) * (c.Gx - cx) + (c.Gy - cy) * (c.Gy - cy);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        return CellFoot(best.Gx, best.Gy);
    }

    private static List<(int X, int Y)> PickEnemyPositions(NtsMap map, (int X, int Y) playerFoot, int count)
    {
        var result = new List<(int X, int Y)>(count);
        var candidates = new List<(int Gx, int Gy)>();
        foreach (var cell in EnumerateWalkableCells(map))
        {
            var foot = CellFoot(cell.Gx, cell.Gy);
            if (Manhattan(foot, playerFoot) < 12 * HitboxPixels)
                continue;
            candidates.Add(cell);
        }

        if (candidates.Count == 0)
            return result;

        var stride = Math.Max(1, candidates.Count / (count + 1));
        for (var i = 0; i < count && i * stride < candidates.Count; i++)
        {
            var c = candidates[i * stride];
            result.Add(CellFoot(c.Gx, c.Gy));
        }

        while (result.Count < count && result.Count < candidates.Count)
            result.Add(CellFoot(candidates[result.Count].Gx, candidates[result.Count].Gy));

        return result;
    }

    private static IEnumerable<(int Gx, int Gy)> EnumerateWalkableCells(NtsMap map)
    {
        for (var gx = 0; gx < map.Gw - 1; gx++)
        {
            for (var gy = 0; gy < map.Gh - 1; gy++)
            {
                if (map.Grid[gx * map.Gh + gy] == 0)
                    yield return (gx, gy);
            }
        }
    }

    private static (int X, int Y) CellFoot(int gx, int gy) =>
        (gx * NtsMap.CellPixels + NtsMap.CellPixels / 2, gy * NtsMap.CellPixels + NtsMap.CellPixels / 2);

    private static int Manhattan((int X, int Y) a, (int X, int Y) b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static bool TryReadMapJpeg(string assetsRoot, int sceneId, out byte[] jpeg)
    {
        jpeg = Array.Empty<byte>();
        var picDir = NtsPaths.PicDirectory(assetsRoot);
        if (!Directory.Exists(picDir))
            return false;

        var entryName = $"{sceneId}.jpg";
        foreach (var path in Directory.EnumerateFiles(picDir))
        {
            if (!MapArchivePattern.IsMatch(Path.GetFileName(path)))
                continue;

            var archive = NtsArchive.Open(path);
            if (archive.TryReadEntry(entryName, out jpeg))
                return true;
        }

        return false;
    }
}
