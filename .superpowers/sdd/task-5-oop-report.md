# Task 5 Report: CharacterFactory、Party、WorldSession

## 状态

**完成** — `CharacterFactoryTests` 2/2；`Qhlc.Game.csproj` 0 错误。

## 交付物

| 文件 | 说明 |
|---|---|
| `src/Qhlc.Player/Public/Party.cs` | `Leader` + `Allies` + `Members` |
| `src/Qhlc.Player/Public/CharacterFactory.cs` | `CreateParty` / `CreateMapNpcs`；可选 `Func<int, CharacterStats?>`；stub 属性回退 |
| `src/Qhlc.World/Public/WorldSession.cs` | `Player` / `Party` / `MapNpcs`；`TryEncounter()` 脚位 AABB |
| `tests/Qhlc.Core.Tests/CharacterFactoryTests.cs` | TEST-0005 |
| `scenes/world/WorldMapController.cs` | 最小接线：Factory + 新 Session 构造 |
| 删除 `PartyState.cs` | 无引用 |

## 验证

```text
dotnet test tests/Qhlc.Core.Tests --filter CharacterFactoryTests → 2 passed
dotnet build Qhlc.Game.csproj → 0 errors
```

## 设计要点

- `PlayerX/Y`、`EnemyPositions` 为 Character 脚位镜像，兼容 Task 7 前 Controller。
- 遇敌：玩家 AABB 以脚位居中 20×20，与地图 NPC 脚位矩形相交（与旧逻辑一致）。
- Stats：无 resolver 时用 stub HP/Atk（注释标明）；Godot 路径可注入 NpcTable 映射。

## 遗留 / 风险

- `WorldSession` 无独立单元测试（遇敌逻辑未单测）。
- `WorldMapController` 仍用 `_playerBlockId` 而非 `session.Player` 外观（Task 7）。
- `check_all.ps1` 留 Task 8。
