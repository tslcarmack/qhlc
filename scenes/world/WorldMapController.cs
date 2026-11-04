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
    private const int HitboxPixels = NtsMap.CellPixels;

    private static readonly Regex MapArchivePattern = new(
        @"^map.*\.(jmg|bmg)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public string AssetsRoot { get; set; } = "";
    public ResolvedContent? ResolvedContent { get; set; }
    public WorldSession? RestoredSession { get; set; }
    public IEquipCatalog? EquipCatalog { get; set; }

    private WorldSession? _session;

    public WorldSession? ExportSession() => _session;

    private Node2D? _sortedRoot;
    private Node2D? _playerRoot;
    private Sprite2D? _playerSprite;
    private Camera2D? _camera;
    private readonly List<Area2D> _enemyAreas = new();
    private readonly List<Node2D> _enemyRoots = new();
    private bool _battleTriggered;

    private ActorView? _playerView;
    private readonly PlayerPathDriver _pathDriver = new();

    public override void _Ready()
    {
        _sortedRoot = GetNode<Node2D>("Sorted");
        _playerRoot = GetNode<Node2D>("Sorted/Player");
        _playerSprite = GetNode<Sprite2D>("Sorted/Player/Sprite");
        _camera = GetNode<Camera2D>("Camera2D");
        _camera.MakeCurrent();
        _playerView = new ActorView(_playerRoot, _playerSprite);
        _playerView.BindAssetsRoot(AssetsRoot);

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

        if (@event is InputEventMouseButton { Pressed: true } mb
            && (mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right))
        {
            ForwardMoveTargetFromMouse();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_session is null || _playerView is null || _battleTriggered)
            return;

        if (Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsMouseButtonPressed(MouseButton.Right))
            ForwardMoveTargetFromMouse();

        _pathDriver.TickMove(delta, _session);
        _playerView.Tick(delta, _session.Player);
        _playerView.SyncCamera(_camera!, _session.Player);

        if (!_session.TryEncounter())
            return;

        _battleTriggered = true;
        _pathDriver.ClearPath();
        _session.MarkEncounterConsumed();
        GetParent<GameMain>()?.EnterBattle();
    }

    private void ForwardMoveTargetFromMouse()
    {
        if (_session is null)
            return;

        _pathDriver.TrySetTargetFromWorldPosition(_session, GetGlobalMousePosition());
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
            var catalog = EquipCatalog ?? new StubEquipCatalog();
            var party = CharacterFactory.CreateParty(content, catalog);
            var mapNpcs = CharacterFactory.CreateMapNpcs(content, catalog, enemyPositions);
            party.Leader.FootX = spawn.X;
            party.Leader.FootY = spawn.Y;
            _session = new WorldSession(content, map, party.Leader, party, mapNpcs);
            GD.Print($"WorldMap: AreaId={content.AreaId} size={map.ImgW}x{map.ImgH} objects={map.Objects.Count} spawn=({spawn.X},{spawn.Y}) enemies={enemyPositions.Count}");
        }

        _pathDriver.ClearPath();
        _playerView!.Prime(_session.Player);
        SpawnEnemies(content, _session.EnemyPositions);
    }

    private void ConfigureCamera(NtsMap map)
    {
        _camera!.Zoom = Vector2.One;
        _camera.LimitEnabled = true;
        _camera.LimitLeft = 0;
        _camera.LimitTop = 0;
        _camera.LimitRight = Math.Max(map.ImgW, 1);
        _camera.LimitBottom = Math.Max(map.ImgH, 1);
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 10f;
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

    private void LoadMapObjects(NtsMap map)
    {
        if (_sortedRoot is null)
            return;

        var gfx = new NtsMapObjectGfx(AssetsRoot);
        var loaded = 0;
        for (var i = 0; i < map.Objects.Count; i++)
        {
            var obj = map.Objects[i];
            if (!gfx.TryLoad(obj.Id, out var raster))
                continue;

            var root = new Node2D
            {
                Name = $"Obj_{obj.Id}_{i}",
                Position = new Vector2(obj.X, obj.Y + raster.Height),
            };
            var sprite = new Sprite2D
            {
                Centered = false,
                Position = new Vector2(0, -raster.Height),
                Texture = EnemyRasterToTexture(raster),
            };
            root.AddChild(sprite);
            _sortedRoot.AddChild(root);
            loaded++;
        }

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
            if (TryLoadEnemyStandSprite(content, i, out var tex, out var w, out var h))
            {
                sprite = new Sprite2D { Centered = false, Texture = tex };
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

    private bool TryLoadEnemyStandSprite(
        ResolvedContent content,
        int enemyIndex,
        out ImageTexture texture,
        out int width,
        out int height)
    {
        texture = null!;
        width = height = 0;

        if (enemyIndex >= content.EnemyNpcIds.Count)
            return false;

        var npcId = content.EnemyNpcIds[enemyIndex];
        if (!content.NpcIdToBlockId.TryGetValue(npcId, out var blockId))
            return false;

        if (!NtsJma.TrySynthesizeFrame(AssetsRoot, blockId, seq: 0, frameIndex: 0, out var raster))
            return false;

        texture = EnemyRasterToTexture(raster);
        width = raster.Width;
        height = raster.Height;
        return true;
    }

    private static ImageTexture EnemyRasterToTexture(NtsRaster raster)
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
