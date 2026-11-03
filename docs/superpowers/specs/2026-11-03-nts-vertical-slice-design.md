# NTS 资源垂直切片设计规格（Godot 单机高还原）

| 字段 | 值 |
|------|-----|
| 状态 | 对话批准（2026-11-03）；规格已写入，待人工审阅 |
| 范围 | 本期垂直切片 only；后续养成/任务/联网另开规格 |
| 交付形态 | 可运行的 Godot 4.7 .NET 单机闭环 |
| 技术底座 | Godot 4.7 + C#；运行时读取 `assets/` 原版包 |
| 资源来源 | 《吞食天地 Online》客户端资源 + `assets/tsdocs` 格式规格 |
| 协作 | 单人 + AI；变更须走仓库治理（CHG） |

## 1. 背景与目标

### 1.1 背景

仓库 `qhlc` 已具备 AI 治理起步包与九模块空壳（core / player / combat / inventory / quest / dialogue / world / save / ui）。`assets/` 存放已逆向说明的 NTSOnline 客户端资源（约 2.2GB），`assets/tsdocs` 给出表、动画、地图、战斗参考实现的字节级规格。

完整复刻超出单次规格范围。本期只做一条端到端可玩切片，并为后续期次留下可复用的资源运行时。

### 1.2 目标

纯单机、高还原（数据与流程对齐 tsdocs；伤害公式使用已标明的参考实现），运行时 C# 解析原包，完成：

**登录壳 → ContentResolver 自动选可用图与单位 → 地图行走（碰撞）→ 小队回合战 → 回地图。**

### 1.3 非目标（本期不做）

- 真实联网、账号校验、服务端协议
- 离线烘焙资源管线（PNG/TileMap/JSON 导出作为主路径）
- 背包 / 装备加成进战 / 商店 / 任务 / 对话树 / 存档
- 完整 281 特效类覆盖、自定义字体 `.JPC`、`.sty`、未解的 `eve.Emg` / `Wem.MMG`
- 商城、星盘、时装等运营向子系统
- 宣称「原版伤害公式已还原」（原版在服务端，客户端无公式；见 tsdocs §05.4）

## 2. 决策摘要

| 决策 | 选择 |
|------|------|
| 产品形态 | 垂直切片（登录壳 → 走图 → 小队战） |
| 还原度 | 高：表结构、地图碰撞、回合流程对齐 tsdocs |
| 网络 | 纯单机；登录仅为 UI 壳 |
| 战斗编制 | 地图单人走动；战斗我方 N=3、敌方 M=2 |
| 资源加载 | 运行时 C# 解析 `.Dat` / `.BMg` / `.jmg` / `.jma` / `Ground.MMG` |
| 演示内容 | ContentResolver 按表序选第一组资源完整组合 |
| 架构 | Core 承载资源运行时；Ui / World / Player / Combat 承接表现与规则 |
| 伤害 | tsdocs 参考公式（`source: reference`） |
| 我方操作 | 全自动战斗（可加速）；保证验收可无人值守打完 |

## 3. 总体架构

### 3.1 验收故事

冷启动 → 登录壳点「进入」→ 自动锁定一组可用资源 → 地图走动且不穿墙 → 接触敌单位进战 → 小队回合分出胜负 → 返回同一地图。

### 3.2 模块职责

| 模块 | 本期职责 |
|------|----------|
| MOD-CORE | 启动、服务定位；NTS 资源运行时；`ContentResolver`；切片状态机编排入口 |
| MOD-UI | 登录壳、走图 HUD、战斗 HUD（优先 `menu/skins/default`） |
| MOD-WORLD | 场景组装、地面/碰撞、地图敌展示体、遇敌触发 |
| MOD-PLAYER | 主角输入与移动；持有当前小队引用 |
| MOD-COMBAT | 回合定序、行动、参考伤害、胜负 |
| Inventory / Quest / Dialogue / Save | 本期零功能（保留程序集，不进入切片依赖） |

### 3.3 依赖方向

```text
Ui ──────▶ Core
World ────▶ Core
Player ───▶ Core
Combat ───▶ Core, Player
```

禁止业务模块反向依赖。不在本期新建 `MOD-ASSET`：解析属跨模块基础设施，放入 Core；若后续体量失控再单开模块并走治理变更。

### 3.4 场景流

```text
Main → Login (Ui) → WorldMap (World + Player + Ui HUD)
                 ↘ Battle (Combat + Ui) ⇄ 结束回 WorldMap
```

建议场景路径：

- `scenes/core/Main.tscn` — 状态机入口
- `scenes/ui/Login.tscn`
- `scenes/world/WorldMap.tscn`
- `scenes/combat/Battle.tscn`

### 3.5 状态机

```text
Boot → ResolvingContent → Login → WorldRoam ⇄ Battle → (胜/负) → WorldRoam
                              └─ Quit
```

| 状态 | 表现 | 输入 |
|------|------|------|
| Boot / ResolvingContent | 简短加载提示 | 无 |
| Login | 登录壳；主按钮「进入游戏」 | 确认 / 退出 |
| WorldRoam | 地图 + 主角 + HUD | WASD / 方向键移动 |
| Battle | 战斗场景；结束后回原坐标 | 加速 / 跳过动画（可选） |

## 4. 资源运行时与 ContentResolver

### 4.1 资源根

默认项目相对路径 `assets/`（仓库根下现有包）。缺 `data/Item.Dat` 或 `data/Ground.MMG` 任一 → 启动错误，不静默空跑。

### 4.2 Core 内组件

| 组件 | 职责 | 对照 |
|------|------|------|
| `NtsContainer` / `NtsScramble` / `NtsText` | 容器判定、解扰、反序 Big5 | tsdocs §03.1–03.2；移植 tsotool `core/*` |
| `NtsTableCatalog` | 表记录长登记；本期至少 `Scene` / `Npc` / `Skill` | §03.3 |
| `NtsBmgArchive` | `.BMg` / `.jmg` 按条目名取字节 | §03.5 |
| `NtsJma` | `.jma` / `.jxa` 与 7 层合成 | §03.4 / §05.1 |
| `NtsGround` | `Ground.MMG` → `.map`，`canWalk(px,py)` | §03.6 / §05.2 |
| `NtsImageBridge` | BMP/JPEG → Godot `Image` / `ImageTexture` | 黑底软抠按 §03.5 |

纯逻辑部分避免依赖 Godot 节点类型，便于 `tests/` 单测；仅 `NtsImageBridge` 与场景侧碰引擎 API。

### 4.3 已知缺口：ModelId → 块号

`Npc.Dat` 的 `ModelId` 到 jma 四位块号的对照表不在 `data/` 内（tsdocs §05.1 / §08）。ContentResolver **不得**用 ModelId 盲查 jma。

外观策略：维护「已验证可合成」的块号白名单（起步采用 tsdocs 实证链路，如 `002:2000`）。Npc 按选取顺序轮转绑定白名单块号（`index % whitelist.Length`）；启动日志打印每条 NpcId → 块号映射。

### 4.4 ContentResolver 选组规则

确定性、可复现：

1. 枚举 `Scene.Dat`（跳过版本卡），按表内顺序。
2. 用 `AreaId` 在 `Ground.MMG` 定位 `.map`；`tiles[]` 所需 `<scene>.jpg` 须在 `Map*.JMg` 全命中；可走格数 ≥ 50。
3. 物件缺图：跳过绘制，不否决整图。
4. 从 `Npc.Dat` 按序挑选我方 N=3（`Faction` 可入队）与敌方 M=2，且每人能绑定白名单块号完成 jma 合成。
5. 第一组同时满足「地图可铺 + 我方 3 + 敌方 2」即锁定；日志输出 Scene 名、AreaId、NpcId 列表、块号映射。
6. 零候选 → 启动错误，提示资源不完整。

### 4.5 缓存与错误

- 进程内缓存：表行一次；纹理键 `(archive, entry)`；jma 帧键 `(block, seq, frame)`。不做磁盘烘焙。
- 单条目失败：日志并跳过候选；仅零候选致命。
- 游玩中缺贴图：色块占位，不崩溃。

## 5. 走图与遇敌

### 5.1 走图

- 坐标系与 `.map` 像素一致；格距 20px；`canWalk` 使用列优先下标，越界阻挡（§05.2）。
- 移动：尝试位移后用脚底点查询；不可走则取消该分量（可滑墙）。
- 相机跟随主角。
- 动画默认 8fps；四向素材不足时允许单 seq / 镜像降级。
- 物件层按数组原序绘于地面之上；角色整体绘于物件之上（不做精细 Y 排序）。

### 5.2 遇敌

- 在可走格刷 M=2 个敌方展示体；本期静止。
- 主角与敌碰撞 → 进入 Battle；遇敌组 = Resolver 预锁定的整组敌方 M 人。
- 开战后该遭遇移除或冷却，避免立刻重入。
- 胜利：移除对应展示体。失败：主角回开战坐标并短暂无敌。

### 5.3 登录壳

用 `menu/skins/default` 下 `panel*`、`btn_*`、`form_NewRole_*`、`Title*` 等按文件名拼一帧可点界面；缺图则色块 + 按钮。不还原账号协议。

## 6. 战斗

### 6.1 编制与数据

- 我方 3、敌方 2；左右简单站位，不还原军阵几何。
- 单位字段按 §05.4 从 `Npc.Dat` 取进战所需项；技能从 `Skill.Dat` 取（无可用技则普攻）。

### 6.2 回合流程

对齐 §05.4 六步：定序（agi 降序）→ 选行动 → 前置校验 → 选目标 → 结算 → 回合收尾。回合上限 30。判结算顺序：先队长 → 再团灭 → 再回合上限。

### 6.3 伤害与 AI

- 使用 tsdocs 参考公式（`source: reference`），规格与 UI/日志不得宣称原版公式。
- 敌我均全自动：合法目标 + 第一个可施放技能或普攻。
- 提供加速 / 跳过动画控件（非门禁）。

### 6.4 表现

- 单位：jma 站立帧。
- 技能：允许闪白 + 飘字；可选接一条 `L*.bmp` 演示特效，非验收门禁。
- HUD：HP/SP、回合序、行动日志；皮肤取 `BattleDot*`、`bar_*`、`panel*` 等。

## 7. 错误处理

| 情况 | 行为 |
|------|------|
| `assets/` 缺失或关键表失败 | 错误场景 + 中文路径说明 |
| ContentResolver 零候选 | 错误场景 + 「无完整 Scene/Npc/jma 组合」 |
| 单贴图 / 单物件缺失 | 日志 + 跳过或色块 |
| 战斗中未处理异常 | 日志；强制结束战斗回地图并提示 |

## 8. 测试与验收

### 8.1 自动化

- 容器整除、解扰往返、反序 Big5 样例。
- `canWalk` 与 §05.2 规则一致（有 `assets` 用实文件；否则嵌入夹具字节）。
- 参考伤害：固定输入 → 与参考实现一致的期望值。
- `scripts/check_all.ps1` 必须通过；不改 governance policies / workflows，除非另开含 `governance` scope 的 CHG。

### 8.2 人工验收清单

1. 启动后登录壳可见，点进入。
2. 进入有底图、不穿墙的地图；日志含锁定的 Scene / Npc / 块号。
3. 接触敌人进入战斗；多单位按敏捷出手；分出胜负并回地图。
4. `scripts/check_all.ps1` 通过。

### 8.3 成功标准

不联网、不烘焙，用运行时原包数据跑通「登录壳 → 自动选图行走 → 小队参考公式战斗」闭环。

## 9. 后续期次（本规格不实施）

| 期次 | 方向 |
|------|------|
| P2+ | 装备进战、更完整特效、手动战斗 UI |
| P3 | 背包 / 商店 |
| P4 | 对话 / 任务 / 存档 / 选角 |
| 可选 | 若 Core 资源代码膨胀，拆 `MOD-ASSET` |

## 10. 实现约束

- 修改 `src/` 或 `tests/` 前须有已登记 CHG，且用户确认 scope。
- 规范性文档简体中文；代码标识符与契约字段英文。
- 术语以 `governance/registry/terms.zh-CN.yaml` 为准；若新增「资源运行时 / ContentResolver」等术语，在实现 CHG 中同步登记。
- 移植优先对照 `assets/tsdocs` 与附录 tsotool 的 `core/` + 所需 `format/`，行为与规格不一致时以 tsdocs 带实证标记的结论为准。
