# Task 7: 拆 WorldMap — 对象自治 + 协调者瘦身

## Status
**Done** — build green; Godot 手测留 Task 8 / 用户。

## Commits
`refactor(world): WorldMap as coordinator; actors own behavior`

## Build
`dotnet build Qhlc.Game.csproj -c Debug` — **0 errors, 0 warnings**

## WorldMapController 行数
| | Lines |
|---|--:|
| Before (内嵌 anim/path) | **694** |
| After (协调者) | **419** |

## 门禁审阅（Controller）
| 禁止项 | 结果 |
|--------|------|
| `_animFrame` | 无 |
| `AdvanceAnimation` | 无 |
| `GridAStar.TryFindPath` / 寻路体 | 无（迁至 `PlayerPathDriver`） |
| `NtsJma.TrySynthesize*` 动画循环 | 无；仅敌人待机 **一行** `TrySynthesizeFrame`（静态占位精灵） |

## 新增/变更文件
- `scenes/world/ActorView.cs` — `ActorAnimator` + `Character.GetAppearanceBlocks()` 呈现
- `scenes/world/PlayerPathDriver.cs` — 点击→A*→`PlayerMover`→`FootX/Y`、`Facing`、`Behavior`
- `WorldMapController.cs` — session 工厂、地图层、挂接、`ForwardMoveTargetFromMouse`、遇敌→`GameMain.EnterBattle`
- `GameMain.cs` — `EquipCatalog` 单例传入 WorldMap
- `Qhlc.Game.csproj` — Compile Include 新文件

## Godot 手测（pending）
1. 登录→走图八向与 Y-sort 遮挡  
2. 按住鼠标连续走，无跳帧闪脸  
3. 遇敌进战→结束回图  

## Concerns
- 敌人精灵仍在 Controller 用单行 JMA 合成（非玩家动画循环）；若审阅要求零 JMA，可再抽 `EnemyStandView`。
- `ActorView` 与敌人贴图各有一份 `RasterToTexture` 小 helper（可后续合并，非本任务范围）。
