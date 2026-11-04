# 角色面向对象重构设计规格

| 字段 | 值 |
|------|-----|
| 状态 | 对话批准（2026-11-04）；规格已写入，待人工审阅 |
| 范围 | 在 NTS 垂直切片之上引入 Character 领域模型、逻辑装备（Stub 目录）、拆分 WorldMap 上帝对象 |
| 前序规格 | `docs/superpowers/specs/2026-11-03-nts-vertical-slice-design.md` |
| 技术底座 | Godot 4.7 + C#；模块边界见 `governance/registry/modules.yaml` |
| 协作 | 单人 + AI；实现须走仓库治理（新 CHG） |

## 1. 背景与目标

### 1.1 现状问题

垂直切片以「解析器 + 静态工具 + 场景上帝对象」交付，可玩但领域模型缺失：

- 外观叠层隐式存在于 `NtsJma`（按块号），无衣/鞋/武器实体
- 走/站动画状态堆在 `WorldMapController`（约 700 行）
- 战斗 `BattleUnit` 与地图实体几乎不共享模型
- `PartyState` 仅为 NpcId 列表；`Player` 模块几乎只有 `PlayerMover`

### 1.2 目标

- **高内聚**：角色属性、装备、行为状态、进战投影集中在 `Character` 继承树
- **低耦合**：Godot 仅作 Presenter；NTS 解析留在 Core；装备目录可替换
- **一条数据源**：地图表现与战斗数值均来自同一 `Character` 实例（Combat 经 Mapper 投影为可变 `BattleUnit` 快照）
- **可验收换装**：Stub 装备目录下可 Equip，外观与/或战斗加成可见

### 1.3 非目标

- 真实 `ModelId → 身体块号` 全表还原（仍未解；见 tsdocs §05.1 / §08）
- 商店、掉落、存档持久化装备
- 把 `Character` 做成 Godot 脚本基类 / `CharacterBody2D` 继承树
- 重做战斗 AI、HUD 布局、联网

### 1.4 已定决策

| 决策 | 选择 |
|------|------|
| 范围 | 外观+动画行为 **与** 战斗模型统一，并拆 WorldMap（选项 C） |
| 建模 | **继承优先**：`Character` → `PlayerCharacter` / `NpcCharacter` |
| 装备 | **逻辑装备**（槽位 + 战斗加成）；数据用 **切片占位映射**（选项 C） |
| 架构风格 | 领域纯 C# + 薄 Godot 视图（方案 1） |

## 2. 类职责与继承

```text
Character (abstract)
├── Id, Name, TeamHint
├── Stats (Hp/Sp/Atk/Def/Int/Agi/Element)
├── EquipmentLoadout
├── Appearance          // Loadout → JMA part/武器块（只读投影）
├── BehaviorState       // Idle | Walk | BattleIdle | …
├── Facing (0–7)
├── virtual void TickBehavior(dt)
├── EffectiveStats() / AppearanceBlocks()   // 供战斗映射与动画读取
├── Equip(slot, itemId) / Unequip(slot)
│
├── PlayerCharacter     // 输入/寻路驱动
└── NpcCharacter        // 地图展示/遇敌；可覆写 Tick
```

配套（组合持有，非深继承）：

| 类型 | 职责 |
|------|------|
| `EquipmentItem` | Id、槽位、外观块增量、属性加成 |
| `IEquipCatalog` / `StubEquipCatalog` | 切片手工映射；日后换真表 |
| `ActorAnimator` | seq/frame/clock/粘滞朝向；调用 `NtsJma` |
| `Party` | 领袖 `PlayerCharacter` + 盟友 `Character` |
| `CharacterFactory` | `ResolvedContent` + Catalog → Party / 地图 NPC |

**边界**：`Character` 不是 Godot Node。`ActorView` 订阅脚位与合成帧。

## 3. 模块归属与依赖

```text
Inventory ──▶ Core
Player    ──▶ Core, Inventory
World     ──▶ Core, Player
Combat    ──▶ Core, Player
Ui/Game   ──▶ 编排与 Presenter
```

| 模块 | 放入 | 禁止放入 |
|------|------|----------|
| MOD-INVENTORY | `IEquipCatalog`、`EquipmentItem`、`EquipmentSlot`、`StubEquipCatalog` | JMA 合成、寻路 |
| MOD-PLAYER | `Character` 树、`Party`、`ActorAnimator`、`CharacterFactory` | 地图拼装、Godot 节点 |
| MOD-WORLD | `WorldSession`（player + mapNpcs + party）、遇敌 | 动画帧合成细节 |
| MOD-COMBAT | `Battle`；`BattleUnit` 快照；`BattleUnitMapper.From(Character)` | 装备目录、反向被 Player 依赖 |
| MOD-CORE | NTS 解析（`NtsJma` 等） | 角色业务对象 |

**依赖约束**：`Character` **不得**引用 `BattleUnit`（否则 Player→Combat 环依赖）。进战由 Combat 侧 Mapper 读取 `EffectiveStats()` 等公开投影。

治理：更新 `modules.yaml`（Inventory→Player；World→Player；Combat 保持→Player）。`PartyState` 裸 ID 列表退役。

## 4. 数据流

```text
ContentResolver.Resolve
  → CharacterFactory.FromResolved(content, catalog)
  → WorldSession(map, player, mapNpcs, party)
  → 走图 Tick（意图 → 移动 → Behavior → Animator → ActorView）
  → 遇敌：BattleUnitMapper.From(party + 触发敌) → Battle → 回同一 Session
```

换装（最小可玩）：走图 HUD「试穿」调用领袖 `Equip`；重算 Appearance 与 EffectiveStats；动画与进战自动生效。Stub 为 3 盟友 + 2 敌写死默认装，并提供 1～2 件可换武器/衣服（须带非零属性加成，以便验收）。

## 5. 拆分 WorldMapController

| 单元 | 职责 |
|------|------|
| `WorldMapController` | 生命周期、输入转发、遇敌切场景 |
| `MapTileLayer` | JPEG 底图 |
| `MapObjectLayer` | meta 段2 + Y-sort |
| `ActorView` | 单 Character → Sprite |
| `PlayerPathDriver` | 鼠标寻路 + `PlayerMover` |
| `ActorAnimator` | 在 Player 程序集 |
| `WorldSession` | 会话状态与遇敌门闩 |

### 5.1 错误处理

- 未知物品 Equip：拒绝并日志，保持原装
- JMA 合成失败：保留上一帧（禁止闪回 seq0）
- Stub 缺默认装：Factory 失败 → 启动态全屏错误
- 进战：Mapper 跳过 0HP 盟友；敌列表空不进战

## 6. 测试与验收

### 6.1 自动化

- `Character_equip_changes_appearance_and_stats`
- `BattleUnitMapper_includes_equip_bonuses`
- `ActorAnimator_walk_keeps_phase_on_facing_stick`
- `CharacterFactory_builds_party_from_resolved`
- `WorldSession_encounter_uses_character_hitbox`
- 治理：`architecture` / `contracts`；`scripts/check_all.ps1` + 新 CHG

### 6.2 手测

1. 登录→走图：ActorView 遮挡与八向正常
2. 试穿 stub 装备：EffectiveStats 可见变化；若物品含外观块增量则 Sprite 变化；进战伤害/面板体现加成
3. 战后回图，Session 位置保留
4. `WorldMapController` 明显瘦身

### 6.3 成功标准

领域可单测；地图与战斗共用 Character 源；替换 Stub 目录无需改 Character API。

## 7. 实现约束

- 新开 `CHG-xxxx`，scope 含 `player` / `inventory` / `world` / `combat` / `tests`（及必要的 `governance` 若改 modules.yaml）
- 规范性文档简体中文；代码标识符与契约字段英文
- 术语以 `governance/registry/terms.zh-CN.yaml` 为准；若新增「角色/装备槽」等词须登记
- 不宣称原版装备数值表已还原；Stub 须在代码/文档标明 `source: stub`

## 8. 风险

| 风险 | 缓解 |
|------|------|
| 占位映射与真表语义漂移 | Catalog 接口稳定；Stub 集中单文件 |
| 一次改动面过大 | 实现计划分任务：Catalog → Character → Factory/Session → 拆 WorldMap → 战斗投影 → HUD 试穿 |
| 继承滥用 | 行为差异优先虚方法钩子；装备与动画用组合成员，避免每职业一子类 |
