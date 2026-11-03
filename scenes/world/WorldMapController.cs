using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;
using Qhlc.Core.Nts;
using Qhlc.Core.Public;
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

    private Sprite2D? _playerSprite;
    private Camera2D? _camera;
    private Node2D? _enemiesRoot;
    private readonly List<Area2D> _enemyAreas = new();
    private int _playerSpriteW;
    private int _playerSpriteH;
    private bool _battleTriggered;

    private int _playerBlockId;
    private int _animSeq;
    private int _animFrame;
    private int _animFrameCount = 1;
    private double _animClock;
    private Vector2 _lastMoveDir = new(0, 1);

    public override void _Ready()
    {
        _playerSprite = GetNode<Sprite2D>("Player");
        _camera = GetNode<Camera2D>("Camera2D");
        _enemiesRoot = GetNode<Node2D>("Enemies");
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

    public override void _Process(double delta)
    {
        if (_session is null || _playerSprite is null || _battleTriggered)
            return;

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (input.LengthSquared() < 0.01f)
            input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        var moving = input.LengthSquared() >= 0.01f;
        if (moving)
        {
            _lastMoveDir = input.Normalized();
            var step = Math.Max(1, (int)Math.Round(MoveSpeedPixelsPerSecond * (float)delta));
            var dx = (int)Math.Round(_lastMoveDir.X * step);
            var dy = (int)Math.Round(_lastMoveDir.Y * step);
            if (dx != 0 || dy != 0)
            {
                var (nextX, nextY) = PlayerMover.TryMove(
                    _session.Map,
                    _session.PlayerX,
                    _session.PlayerY,
                    dx,
                    dy);
                _session.PlayerX = nextX;
                _session.PlayerY = nextY;
            }

            UpdateWalkAnimation(delta);
        }
        else if (_animFrame != 0)
        {
            // Idle: hold first frame of current facing seq
            _animFrame = 0;
            _animClock = 0;
            RefreshPlayerFrame();
        }

        SyncPlayerVisual();

        var playerRect = PlayerHitbox(_session.PlayerX, _session.PlayerY);
        if (!_session.TryEncounter(playerRect))
            return;

        _battleTriggered = true;
        _session.MarkEncounterConsumed();
        GetParent<GameMain>()?.EnterBattle();
    }

    private void BuildWorld(ResolvedContent content)
    {
        var map = NtsMap.LoadFromGround(AssetsRoot, content.AreaId);
        var mapRoot = GetNode<Node2D>("MapTiles");
        foreach (var child in mapRoot.GetChildren())
            child.QueueFree();

        LoadMapTiles(map, mapRoot);
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
            _session = new WorldSession(content, map, spawn.X, spawn.Y, enemyPositions);
            GD.Print($"WorldMap: AreaId={content.AreaId} size={map.ImgW}x{map.ImgH} spawn=({spawn.X},{spawn.Y}) enemies={enemyPositions.Count}");
        }

        if (content.AllyNpcIds.Count > 0
            && content.NpcIdToBlockId.TryGetValue(content.AllyNpcIds[0], out var blockId))
        {
            _playerBlockId = blockId;
        }

        _animSeq = SeqForDirection(_lastMoveDir);
        RefreshAnimFrameCount();
        RefreshPlayerFrame();
        SyncPlayerVisual();
        SpawnEnemies(content, _session.EnemyPositions);
    }

    private void ConfigureCamera(NtsMap map)
    {
        _camera!.LimitEnabled = true;
        _camera.LimitLeft = 0;
        _camera.LimitTop = 0;
        _camera.LimitRight = Math.Max(map.ImgW, 1);
        _camera.LimitBottom = Math.Max(map.ImgH, 1);
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 8f;

        // If the map is smaller than the viewport, zoom out so the whole map fits
        // instead of letterboxing half the screen with clear color.
        var vp = GetViewport().GetVisibleRect().Size;
        if (vp.X > 1 && vp.Y > 1 && map.ImgW > 0 && map.ImgH > 0)
        {
            var zx = vp.X / map.ImgW;
            var zy = vp.Y / map.ImgH;
            var fit = Math.Min(zx, zy);
            if (fit < 1f)
                _camera.Zoom = new Vector2(fit, fit);
            else
                _camera.Zoom = Vector2.One;
        }
    }

    private void UpdateWalkAnimation(double delta)
    {
        var desiredSeq = SeqForDirection(_lastMoveDir);
        if (desiredSeq != _animSeq)
        {
            _animSeq = desiredSeq;
            _animFrame = 0;
            _animClock = 0;
            RefreshAnimFrameCount();
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
                out var raster))
        {
            // Fallback to seq 0 frame 0 if this facing has no frames
            if (_animSeq != 0
                && NtsJma.TrySynthesizeFrame(AssetsRoot, _playerBlockId, 0, 0, out raster))
            {
                _animSeq = 0;
                _animFrame = 0;
                _animFrameCount = 1;
            }
            else
                return;
        }

        ApplySpriteTexture(_playerSprite, RasterToTexture(raster), raster.Width, raster.Height, isPlayer: true);
    }

    /// <summary>
    /// Walk sequences for 002 family are even/odd pairs (tsdocs §05.1). Map 4-way facing to even slots.
    /// </summary>
    private static int SeqForDirection(Vector2 dir)
    {
        if (Math.Abs(dir.X) > Math.Abs(dir.Y))
            return dir.X < 0 ? 2 : 4; // left / right
        return dir.Y < 0 ? 6 : 0; // up / down
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

    private void SpawnEnemies(ResolvedContent content, IReadOnlyList<(int X, int Y)> positions)
    {
        foreach (var child in _enemiesRoot!.GetChildren())
            child.QueueFree();
        _enemyAreas.Clear();

        for (var i = 0; i < positions.Count; i++)
        {
            var (ex, ey) = positions[i];
            var enemyRoot = new Node2D();

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

            enemyRoot.Position = FootToSpritePosition(ex, ey, sw, sh);
            var area = new Area2D();
            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(HitboxPixels, HitboxPixels) },
                Position = new Vector2(sw * 0.5f, sh - HitboxPixels * 0.5f),
            };
            area.AddChild(shape);
            enemyRoot.AddChild(sprite);
            enemyRoot.AddChild(area);
            _enemiesRoot.AddChild(enemyRoot);
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
        if (_session is null || _playerSprite is null)
            return;

        _playerSprite.Position = FootToSpritePosition(
            _session.PlayerX,
            _session.PlayerY,
            _playerSpriteW,
            _playerSpriteH);
        // Camera follows foot (world position), not sprite top-left — keeps character centered
        _camera!.Position = new Vector2(_session.PlayerX, _session.PlayerY - FootOffsetY);
    }

    private static Vector2 FootToSpritePosition(int footX, int footY, int spriteW, int spriteH)
    {
        if (spriteW <= 0 || spriteH <= 0)
            return new Vector2(footX, footY);

        // Entity origin sits FootOffsetY above the foot; raster is composed around that origin.
        // Place sprite so its bottom-center lines up with the foot pixel.
        return new Vector2(footX - spriteW * 0.5f, footY - spriteH);
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
