# Task 11 Report: Battle 场景接线与收尾

## 状态

**完成** — `dotnet test qhlc.sln` 21/21 通过；`dotnet format --verify-no-changes` 通过；`check_all` 在 `manual_review` 未确认处停止（按 CHG 刻意保留 `confirmed: false`）。

## 交付物

| 文件 | 说明 |
|---|---|
| `scenes/combat/BattleController.cs` | `NpcTable` → 3+2 `BattleUnit`；`RunAuto` + 2.5s 结果展示；日志/UI 标注 `reference` 公式来源 |
| `scenes/combat/Battle.tscn` | HUD（敌我 HP 行、日志、Outcome、可选 panel 皮肤） |
| `GameMain.cs` | `MountBattle` 注入 `ResolvedContent`；开战前 stash `WorldSession` |
| `scenes/world/WorldMapController.cs` | `RestoredSession` 回世界保留坐标与遇敌消耗 |
| `tests/Qhlc.Core.Tests/ContentResolverTests.cs` | 补 `[Trait("TraceId","TEST-0003")]` |

## 验证

```text
$env:QHLC_ACTIVE_CHG='CHG-0004'; .\scripts\check_all.ps1
→ dotnet format --verify-no-changes: PASS
→ governance check all: FAIL（manual_review 未确认 — 预期）
→ dotnet test（脚本未执行到该步）

$env:QHLC_ACTIVE_CHG='CHG-0004'; dotnet test qhlc.sln
→ Passed: 21
```

## 设计要点

- **战斗**：进战立即 `Battle.RunAuto`（固定 seed 0），非逐帧模拟；结束 `ReturnToWorld(won)`。
- **WorldSession**：地图节点仍会 remount 并重铺 tile；逻辑会话（玩家坐标、`MarkEncounterConsumed`）经 stash 恢复。
- **CHG-0004**：`verification.tests` 仍为 TEST-0003/0004；`manual_review.confirmed` 保持 `false`（需 Godot 手点一遍）。
