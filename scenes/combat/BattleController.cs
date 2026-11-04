using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Qhlc.Combat.Public;
using Qhlc.Core.Nts;
using Qhlc.Core.Public;
using Qhlc.Player.Public;
using Qhlc.World.Public;

namespace Qhlc.Game;

public partial class BattleController : Control
{
    private const double FinishDelaySeconds = 2.5;

    public string AssetsRoot { get; set; } = "";
    public ResolvedContent? ResolvedContent { get; set; }
    public WorldSession? Session { get; set; }

    private RichTextLabel? _log;
    private Label? _formulaLabel;
    private VBoxContainer? _alliesList;
    private VBoxContainer? _enemiesList;
    private Label? _outcomeLabel;
    private Godot.Timer? _finishTimer;

    public override void _Ready()
    {
        _log = GetNode<RichTextLabel>("Hud/Log");
        _formulaLabel = GetNode<Label>("Hud/FormulaSource");
        _alliesList = GetNode<VBoxContainer>("Hud/Columns/Allies/AlliesList");
        _enemiesList = GetNode<VBoxContainer>("Hud/Columns/Enemies/EnemiesList");
        _outcomeLabel = GetNode<Label>("Hud/Outcome");
        _finishTimer = GetNode<Godot.Timer>("FinishTimer");
        _finishTimer.Timeout += OnFinishTimeout;

        _formulaLabel.Text = $"formula source={ReferenceFormula.Source}";

        if (ResolvedContent is null || string.IsNullOrEmpty(AssetsRoot))
        {
            AppendLog("缺少 ResolvedContent 或 AssetsRoot。");
            ScheduleReturn(won: false);
            return;
        }

        TryApplyBattleSkin();

        if (Session is null)
        {
            AppendLog("缺少 WorldSession，无法从 Party 映射战斗单位。");
            ScheduleReturn(won: false);
            return;
        }

        IReadOnlyList<BattleUnit> allies;
        IReadOnlyList<BattleUnit> enemies;
        try
        {
            allies = BattleUnitMapper.FromParty(Session.Party);
            enemies = BuildEnemies(Session.MapNpcs);
            if (allies.Count == 0)
                throw new InvalidOperationException("我方无存活单位。");
            if (enemies.Count == 0)
                throw new InvalidOperationException("无敌方单位。");
        }
        catch (Exception ex)
        {
            AppendLog($"战斗单位映射失败：{ex.Message}");
            ScheduleReturn(won: false);
            return;
        }

        PopulateUnitRows(_alliesList!, allies);
        PopulateUnitRows(_enemiesList!, enemies);

        AppendLog($"开战：我方 {allies.Count} vs 敌方 {enemies.Count}（{ReferenceFormula.Source} 参考公式）");

        var battle = new Battle(allies, enemies);
        var outcome = battle.RunAuto(new Random(0));
        var won = outcome.WinnerTeam == 0;

        foreach (var line in outcome.ActionLog)
            AppendLog(line);

        var postAllies = outcome.Units.Where(u => u.Team == 0).OrderBy(u => u.Slot).ToList();
        var postEnemies = outcome.Units.Where(u => u.Team == 1).OrderBy(u => u.Slot).ToList();
        PopulateUnitRows(_alliesList!, postAllies);
        PopulateUnitRows(_enemiesList!, postEnemies);

        var winnerText = outcome.WinnerTeam switch
        {
            0 => "我方胜利",
            1 => "敌方胜利",
            _ => "平局（回合上限）",
        };
        _outcomeLabel!.Text = $"{winnerText} · {outcome.Rounds} 回合 · {outcome.EndReason}";
        AppendLog($"{winnerText} rounds={outcome.Rounds} reason={outcome.EndReason}");

        ScheduleReturn(won);
    }

    private static List<BattleUnit> BuildEnemies(IReadOnlyList<NpcCharacter> mapNpcs)
    {
        var enemies = new List<BattleUnit>(mapNpcs.Count);
        for (var slot = 0; slot < mapNpcs.Count; slot++)
        {
            var npc = mapNpcs[slot];
            if (npc.EffectiveStats().Hp <= 0)
                continue;
            enemies.Add(BattleUnitMapper.From(npc, team: 1, slot: enemies.Count, leader: false));
        }

        return enemies;
    }

    private static void PopulateUnitRows(VBoxContainer box, IReadOnlyList<BattleUnit> units)
    {
        foreach (var child in box.GetChildren())
            child.QueueFree();

        foreach (var u in units)
        {
            var row = new Label
            {
                Text = $"{u.Name}  HP {u.Hp}/{u.MaxHp}  SP {u.Sp}/{u.MaxSp}",
            };
            box.AddChild(row);
        }
    }

    private void AppendLog(string line)
    {
        if (_log is null)
            return;

        _log.Text += line + "\n";
    }

    private void ScheduleReturn(bool won)
    {
        _finishTimer!.WaitTime = FinishDelaySeconds;
        _finishTimer.Start();
        _finishTimer.SetMeta("won", won);
    }

    private void OnFinishTimeout()
    {
        var won = _finishTimer!.HasMeta("won") && (bool)_finishTimer.GetMeta("won");
        GetParent<GameMain>()?.ReturnToWorld(won);
    }

    private void TryApplyBattleSkin()
    {
        var skinDir = Path.Combine(AssetsRoot, "assets", "menu", "skins", "default");
        if (!Directory.Exists(skinDir))
            return;

        foreach (var name in new[] { "panelBattle.bmp", "panel_1.bmp", "panel_2.bmp" })
        {
            var path = Path.Combine(skinDir, name);
            if (!File.Exists(path))
                continue;

            var raster = NtsBmp.Decode(File.ReadAllBytes(path));
            var image = Godot.Image.CreateFromData(
                raster.Width,
                raster.Height,
                false,
                Godot.Image.Format.Rgba8,
                raster.Rgba);
            var tex = ImageTexture.CreateFromImage(image);
            var bg = GetNodeOrNull<TextureRect>("Hud/PanelBackground");
            if (bg is not null)
                bg.Texture = tex;
            break;
        }
    }
}
