# Task 8 Report: Player / World 纯逻辑会话

## 状态

**完成** — `PlayerMoverTests` 2/2 通过；`Qhlc.World` 编译通过。

## 交付物

| 文件 | 说明 |
|---|---|
| `src/Qhlc.Player/Public/PartyState.cs` | 小队 Ally/Enemy NpcId 列表 |
| `src/Qhlc.Player/Public/PlayerMover.cs` | 脚底像素 + 轴分离滑墙 `TryMove` |
| `src/Qhlc.World/Public/WorldSession.cs` | `ResolvedContent` + `NtsMap` + 玩家/敌人坐标；`TryEncounter` / `MarkEncounterConsumed` |
| `tests/Qhlc.Core.Tests/PlayerMoverTests.cs` | `TEST-0004`：阻挡不变、对角只滑可走轴 |
| `tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj` | 引用 `Qhlc.Player` |

## 验证

```text
dotnet test tests/Qhlc.Core.Tests --filter PlayerMoverTests
→ 2 passed
```

## 设计要点

- **移动**：先 X 后 Y，每步 `NtsMap.CanWalk(footPx, footPy)`；不可走则保留该轴（spec §5.1）。
- **遇敌**：玩家 AABB 与 20×20 敌人占位相交且未 `MarkEncounterConsumed` 时 `TryEncounter` 为 true；开战后标记 consumed 防立刻重入。

## Commit

`feat(player,world): movement and encounter session logic`

## 变更单

`CHG-0004`（scope 已含 player、world、tests）
