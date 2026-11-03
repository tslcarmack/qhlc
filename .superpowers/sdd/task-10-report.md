# Task 10 Report: WorldMap 走图与遇敌表现

## 状态

**完成** — `dotnet build Qhlc.Game.csproj` 0 错误。

## 交付物

| 文件 | 说明 |
|---|---|
| `scenes/world/WorldMapController.cs` | 铺图、主角 JMA 帧、WASD 移动、`WorldSession` 遇敌 → `EnterBattle` |
| `scenes/world/WorldMap.tscn` | `Node2D` + `MapTiles` / `Player` / `Enemies` / `Camera2D` |
| `GameMain.cs` | `MountWorld()` 注入 `AssetsRoot` + `ResolvedContent` |
| `Qhlc.Game.csproj` | 编入 `WorldMapController.cs` |

## 验证

```text
dotnet build Qhlc.Game.csproj
→ 已成功生成
```

## 设计要点

- **铺图**：`NtsMap.LoadFromGround` + `Map*.JMg` 内 `{scene}.jpg` → `Image.LoadJpgFromBuffer` → `Sprite2D` 于 `(X,Y)`。
- **移动**：`PlayerMover.TryMove` + 脚底像素坐标；`Camera2D` 跟随精灵中心。
- **遇敌**：可走格放置 `ContentResolver.EnemyCount` 敌人（JMA 帧 + `Area2D` 20×20）；`WorldSession.TryEncounter` 后 `MarkEncounterConsumed` 并切 Battle。
- **局限**：返回地图会重建会话（持久化留 Task 11）。

## Commit

`feat(world): map render, walk collision, encounter trigger`
