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
    private const float MoveSpeedPixelsPerSecond = 96f;
    private const int HitboxPixels = NtsMap.CellPixels;

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
            GD.PrintErr($"WorldMap: build failed: {ex.Message}");
        }
    }

    public override void _Process(double delta)
    {
        if (_session is null || _playerSprite is null || _battleTriggered)
            return;

        var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (input.LengthSquared() < 0.01f)
            return;

        var step = (int)Math.Round(MoveSpeedPixelsPerSecond * (float)delta);
        if (step < 1)
            step = 1;

        var dx = (int)Math.Round(input.X * step);
        var dy = (int)Math.Round(input.Y * step);
        if (dx == 0 && dy == 0)
            return;

        var (nextX, nextY) = PlayerMover.TryMove(_session.Map, _session.PlayerX, _session.PlayerY, dx, dy);
        _session.PlayerX = nextX;
        _session.PlayerY = nextY;
        SyncPlayerVisual();

        var playerRect = PlayerHitbox(nextX, nextY);
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
        LoadMapTiles(map, mapRoot);

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
        }

        if (!TryLoadActorSprite(content, isPlayer: true, out var playerTex, out var pw, out var ph))
            GD.Print("WorldMap: player sprite synthesis failed; using placeholder.");
        else
            ApplySpriteTexture(_playerSprite!, playerTex, pw, ph, isPlayer: true);

        SyncPlayerVisual();
        SpawnEnemies(content, _session.EnemyPositions);
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
        _enemyAreas.Clear();
        for (var i = 0; i < positions.Count; i++)
        {
            var (ex, ey) = positions[i];
            var enemyRoot = new Node2D { Position = new Vector2(ex, ey) };

            Sprite2D sprite;
            if (TryLoadActorSprite(content, isPlayer: false, out var tex, out var w, out var h, enemyIndex: i))
            {
                sprite = new Sprite2D { Centered = false };
                ApplySpriteTexture(sprite, tex, w, h, isPlayer: false);
            }
            else
            {
                sprite = new Sprite2D
                {
                    Texture = PlaceholderTexture(Colors.IndianRed),
                    Centered = false,
                };
            }

            var area = new Area2D();
            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(HitboxPixels, HitboxPixels) },
                Position = new Vector2(HitboxPixels * 0.5f, HitboxPixels * 0.5f),
            };
            area.AddChild(shape);
            enemyRoot.AddChild(sprite);
            enemyRoot.AddChild(area);
            _enemiesRoot!.AddChild(enemyRoot);
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

        _playerSprite.Position = FootToSpritePosition(_session.PlayerX, _session.PlayerY, _playerSpriteW, _playerSpriteH);
        _camera!.Position = _playerSprite.Position + new Vector2(_playerSpriteW * 0.5f, _playerSpriteH * 0.5f);
    }

    private static Vector2 FootToSpritePosition(int footX, int footY, int spriteW, int spriteH)
    {
        if (spriteW <= 0 || spriteH <= 0)
            return new Vector2(footX, footY);

        return new Vector2(footX - spriteW * 0.5f, footY - spriteH + HitboxPixels * 0.5f);
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
        foreach (var cell in EnumerateWalkableCells(map))
            return CellFootTopLeft(cell.Gx, cell.Gy);

        return (0, 0);
    }

    private static List<(int X, int Y)> PickEnemyPositions(NtsMap map, (int X, int Y) playerFoot, int count)
    {
        var result = new List<(int X, int Y)>(count);
        var candidates = new List<(int Gx, int Gy)>();
        foreach (var cell in EnumerateWalkableCells(map))
        {
            var foot = CellFootTopLeft(cell.Gx, cell.Gy);
            if (Manhattan(foot, playerFoot) < 8 * HitboxPixels)
                continue;
            candidates.Add(cell);
        }

        var stride = Math.Max(1, candidates.Count / (count + 1));
        for (var i = 0; i < count && i * stride < candidates.Count; i++)
        {
            var c = candidates[i * stride];
            result.Add(CellFootTopLeft(c.Gx, c.Gy));
        }

        while (result.Count < count && result.Count < candidates.Count)
            result.Add(CellFootTopLeft(candidates[result.Count].Gx, candidates[result.Count].Gy));

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

    private static (int X, int Y) CellFootTopLeft(int gx, int gy) =>
        (gx * NtsMap.CellPixels, gy * NtsMap.CellPixels);

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
