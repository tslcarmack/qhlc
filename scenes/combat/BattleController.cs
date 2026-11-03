using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Qhlc.Combat.Public;
using Qhlc.Core.Nts;
using Qhlc.Core.Nts.Tables;
using Qhlc.Core.Public;

namespace Qhlc.Game;

public partial class BattleController : Control
{
    private const double FinishDelaySeconds = 2.5;

    public string AssetsRoot { get; set; } = "";
    public ResolvedContent? ResolvedContent { get; set; }

    private RichTextLabel? _log;
    private Label? _formulaLabel;
    private VBoxContainer? _alliesBox;
    private VBoxContainer? _enemiesBox;
    private Label? _outcomeLabel;
    private Godot.Timer? _finishTimer;

    public override void _Ready()
    {
        _log = GetNode<RichTextLabel>("Hud/Log");
        _formulaLabel = GetNode<Label>("Hud/FormulaSource");
        _alliesBox = GetNode<VBoxContainer>("Hud/Allies");
        _enemiesBox = GetNode<VBoxContainer>("Hud/Enemies");
        _outcomeLabel = GetNode<Label>("Hud/Outcome");
        _finishTimer = GetNode<Timer>("FinishTimer");
        _finishTimer.Timeout += OnFinishTimeout;

        _formulaLabel.Text = $"formula source={ReferenceFormula.Source}";

        if (ResolvedContent is null || string.IsNullOrEmpty(AssetsRoot))
        {
            AppendLog("缺少 ResolvedContent 或 AssetsRoot。");
            ScheduleReturn(won: false);
            return;
        }

        TryApplyBattleSkin();

        IReadOnlyList<BattleUnit> allies;
        IReadOnlyList<BattleUnit> enemies;
        try
        {
            var npcById = NpcTable.Load(AssetsRoot).ToDictionary(n => n.Id);
            allies = BuildParty(ResolvedContent.AllyNpcIds, team: 0, npcById);
            enemies = BuildParty(ResolvedContent.EnemyNpcIds, team: 1, npcById);
        }
        catch (Exception ex)
        {
            AppendLog($"Npc 加载失败：{ex.Message}");
            ScheduleReturn(won: false);
            return;
        }

        PopulateUnitRows(_alliesBox!, allies);
        PopulateUnitRows(_enemiesBox!, enemies);

        AppendLog($"开战：我方 {allies.Count} vs 敌方 {enemies.Count}（{ReferenceFormula.Source} 参考公式）");

        var battle = new Battle(allies, enemies);
        var outcome = battle.RunAuto(new Random(0));
        var won = outcome.WinnerTeam == 0;

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

    private static List<BattleUnit> BuildParty(
        IReadOnlyList<int> npcIds,
        int team,
        IReadOnlyDictionary<int, NpcRecord> npcById)
    {
        var party = new List<BattleUnit>(npcIds.Count);
        for (var slot = 0; slot < npcIds.Count; slot++)
        {
            var id = npcIds[slot];
            if (!npcById.TryGetValue(id, out var npc))
                throw new InvalidOperationException($"Npc.Dat 缺少 id={id}");

            var uid = team * 1000 + slot + 1;
            party.Add(
                BattleUnit.Create(
                    uid,
                    npc.Name,
                    team: team,
                    slot: slot,
                    hp: npc.Hp,
                    atk: npc.Atk,
                    def: npc.Def,
                    intel: npc.Int,
                    agi: npc.Agi,
                    maxSp: npc.Sp,
                    sp: npc.Sp,
                    leader: team == 0 && slot == 0,
                    element: 0,
                    skills: Array.Empty<int>()));
        }

        return party;
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
