using System;
using System.Linq;
using Godot;
using Qhlc.Core.Nts;
using Qhlc.Core.Public;
using Qhlc.Inventory.Public;
using Qhlc.World.Public;

namespace Qhlc.Game;

public enum SliceGameState
{
    Boot,
    Resolving,
    Login,
    WorldRoam,
    Battle,
}

public partial class GameMain : Node2D
{
    private const string LoginScenePath = "res://scenes/ui/Login.tscn";
    private const string WorldScenePath = "res://scenes/world/WorldMap.tscn";
    private const string BattleScenePath = "res://scenes/combat/Battle.tscn";

    private Node? _stateRoot;
    private SliceGameState _state = SliceGameState.Boot;

    public string AssetsRoot { get; private set; } = "";
    public ResolvedContent? ResolvedContent { get; private set; }
    public IEquipCatalog EquipCatalog { get; } = new StubEquipCatalog();

    private WorldSession? _stashedWorldSession;

    public override void _Ready() => ChangeState(SliceGameState.Boot);

    public void EnterBattle()
    {
        if (FindStateNode<WorldMapController>() is { } worldMap)
            _stashedWorldSession = worldMap.ExportSession();
        ChangeState(SliceGameState.Battle);
    }

    public void ReturnToWorld(bool won)
    {
        GD.Print($"GameMain: battle ended won={won}");
        ChangeState(SliceGameState.WorldRoam);
    }

    public void ChangeState(SliceGameState next)
    {
        _state = next;
        ClearStateRoot();

        switch (next)
        {
            case SliceGameState.Boot:
                RunBoot();
                break;
            case SliceGameState.Resolving:
                RunResolving();
                break;
            case SliceGameState.Login:
                MountLogin();
                break;
            case SliceGameState.WorldRoam:
                MountWorld();
                break;
            case SliceGameState.Battle:
                MountBattle();
                break;
        }
    }

    private void RunBoot()
    {
        AssetsRoot = NtsPaths.ResolveAssetsRoot();
        if (!TryValidateBootAssets(AssetsRoot, out var message))
        {
            ShowFullscreenMessage(message);
            return;
        }

        ChangeState(SliceGameState.Resolving);
    }

    private void RunResolving()
    {
        ShowFullscreenMessage("正在解析资源…");

        try
        {
            ResolvedContent = ContentResolver.Resolve(AssetsRoot);
            PrintResolvedContent(ResolvedContent);
        }
        catch (Exception ex)
        {
            ShowFullscreenMessage($"资源解析失败：{ex.Message}");
            return;
        }

        ChangeState(SliceGameState.Login);
    }

    private void MountLogin()
    {
        var packed = GD.Load<PackedScene>(LoginScenePath);
        var login = packed.Instantiate<LoginController>();
        login.AssetsRoot = AssetsRoot;
        login.EnterGame += () => ChangeState(SliceGameState.WorldRoam);
        AttachStateRoot(login);
    }

    private void MountWorld()
    {
        if (ResolvedContent is null)
        {
            ShowFullscreenMessage("资源尚未解析，无法进入地图。");
            return;
        }

        var packed = GD.Load<PackedScene>(WorldScenePath);
        var world = packed.Instantiate<WorldMapController>();
        world.AssetsRoot = AssetsRoot;
        world.ResolvedContent = ResolvedContent;
        world.EquipCatalog = EquipCatalog;
        world.RestoredSession = _stashedWorldSession;
        _stashedWorldSession = null;
        AttachStateRoot(world);
    }

    private void MountBattle()
    {
        if (ResolvedContent is null)
        {
            ShowFullscreenMessage("资源尚未解析，无法进入战斗。");
            return;
        }

        var packed = GD.Load<PackedScene>(BattleScenePath);
        var battle = packed.Instantiate<BattleController>();
        battle.AssetsRoot = AssetsRoot;
        battle.ResolvedContent = ResolvedContent;
        battle.Session = _stashedWorldSession;
        AttachStateRoot(battle);
    }

    private void ShowFullscreenMessage(string text)
    {
        var layer = new CanvasLayer();
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(label);
        AttachStateRoot(layer);
    }

    private static bool TryValidateBootAssets(string assetsRoot, out string message)
    {
        var dataDir = NtsPaths.DataDirectory(assetsRoot);
        var itemDat = System.IO.Path.Combine(dataDir, "Item.Dat");
        var groundMmg = System.IO.Path.Combine(dataDir, "Ground.MMG");

        if (!System.IO.File.Exists(itemDat) || !System.IO.File.Exists(groundMmg))
        {
            message = "缺少必要数据文件：assets/data/Item.Dat 或 Ground.MMG。";
            return false;
        }

        message = "";
        return true;
    }

    private void PrintResolvedContent(ResolvedContent content)
    {
        var allies = string.Join(",", content.AllyNpcIds);
        var enemies = string.Join(",", content.EnemyNpcIds);
        var blocks = string.Join(
            ",",
            content.NpcIdToBlockId.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}->{kv.Value:D4}"));

        GD.Print(
            $"ContentResolver: Scene={content.SceneName} AreaId={content.AreaId} allies=[{allies}] enemies=[{enemies}] blocks=[{blocks}]");

        if (ContentResolver.LastResolveWarning is { } warning)
            GD.Print(warning);
    }

    private T? FindStateNode<T>() where T : class
    {
        if (_stateRoot is T direct)
            return direct;
        if (_stateRoot is null)
            return null;
        foreach (var child in _stateRoot.GetChildren())
        {
            if (child is T match)
                return match;
        }

        return null;
    }

    private void AttachStateRoot(Node node)
    {
        // Control under Node2D does not participate in GUI layout: full-rect anchors stay
        // 0×0, so Login/Battle backgrounds never appear. Host UI on a CanvasLayer instead.
        if (node is Control control)
        {
            control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var layer = new CanvasLayer { Name = "StateUiLayer" };
            layer.AddChild(control);
            _stateRoot = layer;
            AddChild(layer);
            return;
        }

        _stateRoot = node;
        AddChild(node);
    }

    private void ClearStateRoot()
    {
        if (_stateRoot is null)
            return;

        // Immediate remove — QueueFree leaves Login Control covering the viewport for a frame
        // and can leave a grey half-screen under a camera that peeks past the map edge.
        RemoveChild(_stateRoot);
        _stateRoot.Free();
        _stateRoot = null;
    }
}
