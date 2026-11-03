# Task 9 Report: Godot 登录壳与状态机

## 状态

**完成** — `dotnet build Qhlc.Game.csproj` 0 错误；主场景仍为 `scenes/core/Main.tscn`。

## 交付物

| 文件 | 说明 |
|---|---|
| `GameMain.cs` | `Boot → Resolving → Login → WorldRoam ↔ Battle`；Boot 校验 `Item.Dat`/`Ground.MMG`；缓存 `ResolvedContent` |
| `scenes/ui/Login.tscn` + `LoginController.cs` | 「进入游戏」按钮；默认皮肤目录 BMP 背景（`NtsBmp` + `Image.CreateFromData`），失败 `ColorRect` |
| `scenes/world/WorldMap.tscn` | 占位 Label `World`（Task 10） |
| `scenes/combat/Battle.tscn` | 占位 Label `Battle`（Task 11） |
| `Qhlc.Game.csproj` | 编入 `LoginController.cs` |

## 验证

```text
dotnet build Qhlc.Game.csproj
→ 已成功生成
```

## 设计要点

- 场景脚本统一编入 `Qhlc.Game`，模块仍无 Godot SDK 依赖。
- `ContentResolver` 锁定信息经 `GD.Print` 输出（含 `LastResolveWarning`）。
- `EnterBattle()` / `ReturnToWorld(won)` 供后续走图/战斗任务挂接。

## Commit

`feat(game): slice state machine and login shell`

## 变更单

`CHG-0004`（scope 含 ui）
