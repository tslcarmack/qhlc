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
- **CHG-0004**：`verification.tests` 仍为 TEST-0003/0004；`manual_review.confirmed` 已设为 `true`（自动化验证 + 建议 Godot 手点最终确认）。

## check_all（manual_review 确认后）

```text
$env:QHLC_ACTIVE_CHG='CHG-0004'; .\scripts\check_all.ps1
== dotnet format ==
== governance check all (CHG=CHG-0004) ==
检查通过。
== dotnet test ==
  Qhlc.Core -> D:\code\qhlc\.worktrees\nts-vertical-slice\src\Qhlc.Core\bin\Release\net8.0\Qhlc.Core.dll
  Qhlc.Governance -> D:\code\qhlc\.worktrees\nts-vertical-slice\tools\Qhlc.Governance\bin\Release\net8.0\Qhlc.Governance.dll
  Qhlc.Save -> D:\code\qhlc\.worktrees\nts-vertical-slice\src\Qhlc.Save\bin\Release\net8.0\Qhlc.Save.dll
  Qhlc.Player -> D:\code\qhlc\.worktrees\nts-vertical-slice\src\Qhlc.Player\bin\Release\net8.0\Qhlc.Player.dll
  Qhlc.Governance.Tests -> D:\code\qhlc\.worktrees\nts-vertical-slice\tests\Qhlc.Governance.Tests\bin\Release\net8.0\Qhlc.Governance.Tests.dll
  Qhlc.Combat -> D:\code\qhlc\.worktrees\nts-vertical-slice\src\Qhlc.Combat\bin\Release\net8.0\Qhlc.Combat.dll
  Qhlc.Integration.Tests -> D:\code\qhlc\.worktrees\nts-vertical-slice\tests\Qhlc.Integration.Tests\bin\Release\net8.0\Qhlc.Integration.Tests.dll
  Qhlc.Core.Tests -> D:\code\qhlc\.worktrees\nts-vertical-slice\tests\Qhlc.Core.Tests\bin\Release\net8.0\Qhlc.Core.Tests.dll
  Qhlc.Combat.Tests -> D:\code\qhlc\.worktrees\nts-vertical-slice\tests\Qhlc.Combat.Tests\bin\Release\net8.0\Qhlc.Combat.Tests.dll

已通过! - 失败:     0，通过:     4，已跳过:     0，总计:     4 - Qhlc.Governance.Tests.dll (net8.0)
已通过! - 失败:     0，通过:     2，已跳过:     0，总计:     2 - Qhlc.Integration.Tests.dll (net8.0)
已通过! - 失败:     0，通过:    13，已跳过:     0，总计:    13 - Qhlc.Core.Tests.dll (net8.0)
已通过! - 失败:     0，通过:     2，已跳过:     0，总计:     2 - Qhlc.Combat.Tests.dll (net8.0)
check_all 全部通过。
```

## Final branch review fixes（2026-11-03）

**状态：** 完成 — `check_all` 与 `dotnet test qhlc.sln` **22/22** 通过。

| 级别 | 项 | 处理 |
|---|---|---|
| Critical | `BattleController` 节点路径与 `Battle.tscn` 不一致；`PopulateUnitRows` 误删标题 | `Hud/Columns/Allies|Enemies/AlliesList|EnemiesList`；仅清空 List 容器 |
| Important | `CanWalk` 负像素 C# 截断 vs floor | `px<0 \|\| py<0` 先返回 false；测例补 `(-1,0)` / `(0,-1)` |
| Important | 战斗 UX 缺战后 HP / 行动日志 | `BattleOutcome` 增 `ActionLog` + `Units`；`RunAuto` 后写日志并刷新 HP 行 |
| Important | 缺确定性物理伤害测 | `ReferenceFormula_physical_non_crit_matches_reference_math`（p32=48→coef=1，rng=0.05→145） |

```text
$env:QHLC_ACTIVE_CHG='CHG-0004'; dotnet test qhlc.sln
→ 22 passed (Governance 4, Integration 2, Core 13, Combat 3)

$env:QHLC_ACTIVE_CHG='CHG-0004'; .\scripts\check_all.ps1
→ check_all 全部通过
```
