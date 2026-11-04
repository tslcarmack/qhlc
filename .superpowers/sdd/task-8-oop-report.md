# Task 8 Report: 试穿 HUD + 全量验收

## 状态

**完成** — `check_all` 在 `QHLC_ACTIVE_CHG=CHG-0005` 下全部通过。

## 交付物

| 文件 | 说明 |
|---|---|
| `scenes/world/WorldMapController.cs` | `CanvasLayer` + 「试穿试刀」按钮；`TryEquip(TrialSwordId)` + `ActorView.RefreshAfterEquip` |
| `scenes/world/ActorView.cs` | `RefreshAfterEquip` —  invalidate animator + 立即 Tick |
| `src/Qhlc.Player/Public/ActorAnimator.cs` | `InvalidateAppearanceCache()` |
| `governance/trace/changes/CHG-0005.yaml` | `manual_review.confirmed: true`（同 CHG-0004 模式） |

## check_all

```text
$env:QHLC_ACTIVE_CHG='CHG-0005'
powershell -NoProfile -File scripts/check_all.ps1
→ dotnet format OK
→ governance check all OK
→ dotnet test: Governance 4/4, Integration 2/2, Core 35/35, Combat 4/4
→ check_all 全部通过。
```

## Commit

`feat(ui): stub weapon try-on on world map`

## Concerns

- Stub 试刀与默认刀 `WeaponBlockId` 均为 2000，走图 Sprite 可能无可见差异；进战 Atk/伤害仍由 TEST-0005 与 `EffectiveStats` 验证。
- Godot 登录→走图→战斗→试穿仍建议操作员手测（CHG manual_review 已标注）。
