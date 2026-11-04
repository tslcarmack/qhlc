# Character OOP 重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 引入 `Character` 继承树与逻辑装备（Stub 目录），地图/战斗共用同一人物源，并拆分 `WorldMapController` 上帝对象。

**Architecture:** 领域对象在 `Qhlc.Inventory` / `Qhlc.Player`（纯 C#）；`Combat` 经 `BattleUnitMapper` 投影；Godot 仅 `ActorView` / 路径驱动 / 瘦 `WorldMapController`。装备目录可整文件替换。

**Tech Stack:** Godot 4.7.2 .NET、C# / .NET 8、xUnit、`Qhlc.Governance` 门禁。

**Spec:** `docs/superpowers/specs/2026-11-04-character-oop-refactor-design.md`

## Global Constraints

- 无已确认 scope 的 `CHG-*.yaml` 不得改 `src/` / `tests/`。
- 改 `modules.yaml` 须 CHG `scope` 含 `governance` 且用户确认。
- 规范性文档简体中文；代码标识符与契约字段英文。
- 术语以 `governance/registry/terms.zh-CN.yaml` 为准（勿用「角色」指代玩家；人物用 `Character`/`人物`）。
- Stub 装备须标明 `source: stub`；禁止宣称原版装备表已还原。
- `Character` 不得引用 `BattleUnit`（禁止 Player→Combat 依赖）。
- 完成前跑 `scripts/check_all.ps1`（设置 `QHLC_ACTIVE_CHG`）。
- Core 不引用 Godot；`NtsRaster` → 纹理转换仅在 `scenes/`。

## File Structure（实现后）

| 路径 | 职责 |
|------|------|
| `governance/trace/requirements/REQ-0004.yaml` | 人物 OOP / 装备 / WorldMap 拆分需求 |
| `governance/trace/changes/CHG-0005.yaml` | 本期变更单 |
| `governance/registry/terms.zh-CN.yaml` | 增补人物/装配物/装配槽 |
| `governance/registry/modules.yaml` | Inventory←仅 Core；Player←Inventory；World←Player |
| `src/Qhlc.Inventory/Qhlc.Inventory.csproj` | **去掉**对 Player 的引用 |
| `src/Qhlc.Inventory/Public/EquipmentSlot.cs` | 槽位枚举 |
| `src/Qhlc.Inventory/Public/EquipmentItem.cs` | 装配物定义 |
| `src/Qhlc.Inventory/Public/IEquipCatalog.cs` | 目录接口 |
| `src/Qhlc.Inventory/Public/StubEquipCatalog.cs` | 切片占位目录 |
| `src/Qhlc.Player/Qhlc.Player.csproj` | 增加 Inventory 引用 |
| `src/Qhlc.Player/Public/CharacterStats.cs` | 属性结构 |
| `src/Qhlc.Player/Public/AppearanceBlocks.cs` | JMA 块投影 |
| `src/Qhlc.Player/Public/EquipmentLoadout.cs` | 已装备槽 |
| `src/Qhlc.Player/Public/BehaviorState.cs` | Idle/Walk/… |
| `src/Qhlc.Player/Public/Character.cs` | 抽象基类 |
| `src/Qhlc.Player/Public/PlayerCharacter.cs` | 玩家 |
| `src/Qhlc.Player/Public/NpcCharacter.cs` | NPC |
| `src/Qhlc.Player/Public/Party.cs` | 小队 |
| `src/Qhlc.Player/Public/CharacterFactory.cs` | 从 ResolvedContent 组装 |
| `src/Qhlc.Player/Public/ActorAnimator.cs` | 动画相位（自 WorldMap 迁出） |
| `src/Qhlc.Player/Public/PartyState.cs` | 标记 obsolete 或删除（改用 Party） |
| `src/Qhlc.World/Qhlc.World.csproj` | 增加 Player 引用 |
| `src/Qhlc.World/Public/WorldSession.cs` | 持有 Character / Party |
| `src/Qhlc.Combat/Public/BattleUnitMapper.cs` | Character → BattleUnit |
| `scenes/world/ActorView.cs` | Godot 视图 |
| `scenes/world/PlayerPathDriver.cs` | 鼠标寻路 |
| `scenes/world/WorldMapController.cs` | 瘦编排 |
| `scenes/combat/BattleController.cs` | 使用 Mapper |
| `GameMain.cs` | 注入 catalog / factory |
| `Qhlc.Game.csproj` | 编译新 scene 脚本 |
| `tests/Qhlc.Inventory.Tests/` 或挂在 Core.Tests / 新项目 | 目录与装备测 |
| `tests/Qhlc.Player.Tests/` 或扩展现有 | Character / Animator |
| `tests/Qhlc.Combat.Tests/` | Mapper |
| `tests/Qhlc.Core.Tests/` 或 World 测 | Session 遇敌 |

---

### Task 1: 登记 REQ/CHG/术语与模块依赖翻转

**Files:**
- Create: `governance/trace/requirements/REQ-0004.yaml`
- Create: `governance/trace/changes/CHG-0005.yaml`
- Modify: `governance/registry/terms.zh-CN.yaml`
- Modify: `governance/registry/modules.yaml`
- Modify: `src/Qhlc.Inventory/Qhlc.Inventory.csproj`
- Modify: `src/Qhlc.Player/Qhlc.Player.csproj`
- Modify: `src/Qhlc.World/Qhlc.World.csproj`

**Interfaces:**
- Produces: 用户确认的 CHG scope：`inventory`, `player`, `world`, `combat`, `tests`, `governance`
- Produces: 程序集依赖 `Inventory → Core`；`Player → Core, Inventory`；`World → Core, Player`

- [ ] **Step 1: 写 REQ-0004**

```yaml
id: REQ-0004
title: 人物领域模型与装配物 Stub 闭环
module: MOD-PLAYER
status: active
priority: high
description_zh: >
  引入 Character 继承树与逻辑装配（Stub 目录），地图与战斗共用人物源，
  拆分 WorldMapController；装配物数据标明 source stub。
acceptance:
  - Character 可 Equip 并改变 EffectiveStats
  - BattleUnitMapper 投影含加成
  - 走图 ActorView 使用 Character 外观
  - WorldMapController 不再内嵌动画状态机
tests:
  - TEST-0005
```

- [ ] **Step 2: 写 CHG-0005**

```yaml
id: CHG-0005
title: 实现人物 OOP 重构与 Stub 装配目录
requirements:
  - REQ-0004
scope:
  - inventory
  - player
  - world
  - combat
  - tests
  - governance
impact:
  modules:
    - MOD-INVENTORY
    - MOD-PLAYER
    - MOD-WORLD
    - MOD-COMBAT
    - MOD-CORE
  apis: []
  data_migration: false
  manual_review:
    - description: Godot 手测走图换装与进战加成
      confirmed: false
acceptance:
  - 符合 REQ-0004
verification:
  tests:
    - TEST-0005
  checks:
    - architecture
    - contracts
    - doc-language
documentation:
  - docs/superpowers/specs/2026-11-04-character-oop-refactor-design.md
  - docs/superpowers/plans/2026-11-04-character-oop-refactor.md
recovery:
  rollback: code_only
  notes: ""
approvals:
  implementer: self
  reviewer: optional
  merged_by: self
```

- [ ] **Step 3: 增补术语（避免与 player 的「角色」冲突）**

在 `terms.zh-CN.yaml` 追加：

```yaml
  - concept: character
    name_zh: 人物
    identifier_en: Character
    forbidden_aliases:
      - 玩家

  - concept: equipment_item
    name_zh: 装配物
    identifier_en: EquipmentItem
    forbidden_aliases: []

  - concept: equipment_slot
    name_zh: 装配槽
    identifier_en: EquipmentSlot
    forbidden_aliases: []
```

- [ ] **Step 4: 翻转 modules.yaml 与 csproj 依赖**

`modules.yaml`：
- `MOD-INVENTORY.depends_on`: 仅 `[MOD-CORE]`
- `MOD-PLAYER.depends_on`: `[MOD-CORE, MOD-INVENTORY]`
- `MOD-WORLD.depends_on`: `[MOD-CORE, MOD-PLAYER]`

`Qhlc.Inventory.csproj`：删除对 `Qhlc.Player` 的 `ProjectReference`。  
`Qhlc.Player.csproj`：增加 `..\Qhlc.Inventory\Qhlc.Inventory.csproj`。  
`Qhlc.World.csproj`：增加 `..\Qhlc.Player\Qhlc.Player.csproj`。

- [ ] **Step 5: 请用户确认 CHG-0005 scope（含 governance）**

对话中明确列出 scope 并等待确认后再改 `src/`。

- [ ] **Step 6: 跑治理影响分析**

Run: `dotnet run --project tools/Qhlc.Governance -- impact CHG-0005`  
Expected: 退出码 0 或可解释的影响报告。

- [ ] **Step 7: Commit**

```bash
git add governance/trace/requirements/REQ-0004.yaml governance/trace/changes/CHG-0005.yaml governance/registry/terms.zh-CN.yaml governance/registry/modules.yaml src/Qhlc.Inventory/Qhlc.Inventory.csproj src/Qhlc.Player/Qhlc.Player.csproj src/Qhlc.World/Qhlc.World.csproj
git commit -m "chore(gov): CHG-0005 character OOP scope and module deps"
```

---

### Task 2: Inventory — 装配槽 / 装配物 / Stub 目录

**Files:**
- Create: `src/Qhlc.Inventory/Public/EquipmentSlot.cs`
- Create: `src/Qhlc.Inventory/Public/EquipmentItem.cs`
- Create: `src/Qhlc.Inventory/Public/IEquipCatalog.cs`
- Create: `src/Qhlc.Inventory/Public/StubEquipCatalog.cs`
- Create: `tests/Qhlc.Core.Tests/StubEquipCatalogTests.cs`（或新建 `tests/Qhlc.Inventory.Tests` 若门禁允许；优先挂现有测试工程并引用 Inventory）

**Interfaces:**
- Produces:
  - `enum EquipmentSlot { Weapon, Body, Head, Shoes, Accessory }`
  - `sealed record EquipmentItem(int Id, string Name, EquipmentSlot Slot, int BodyBlockDelta, int WeaponBlockId, int AtkBonus, int DefBonus, int HpBonus)` — `BodyBlockDelta` 为相对本体块的 part 偏移提示；`WeaponBlockId` 0 表示不改武器
  - `interface IEquipCatalog { bool TryGet(int itemId, out EquipmentItem item); IReadOnlyList<EquipmentItem> All { get; }; EquipmentItem? DefaultForNpc(int npcId); }`
  - `sealed class StubEquipCatalog : IEquipCatalog` — `source: stub`

- [ ] **Step 1: 写失败测试**

```csharp
using Qhlc.Inventory.Public;

public class StubEquipCatalogTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Stub_has_default_loadout_and_swappable_weapon()
    {
        var cat = new StubEquipCatalog();
        Assert.NotNull(cat.DefaultForNpc(10001));
        Assert.True(cat.TryGet(StubEquipCatalog.TrialSwordId, out var sword));
        Assert.Equal(EquipmentSlot.Weapon, sword.Slot);
        Assert.True(sword.AtkBonus > 0);
    }
}
```

若 `Qhlc.Core.Tests` 无 Inventory 引用，在其 csproj 增加：

```xml
<ProjectReference Include="..\..\src\Qhlc.Inventory\Qhlc.Inventory.csproj" />
```

- [ ] **Step 2: Run 确认失败**

Run: `dotnet test tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj --filter FullyQualifiedName~StubEquipCatalogTests`  
Expected: FAIL（类型不存在）

- [ ] **Step 3: 实现类型与 Stub**

```csharp
namespace Qhlc.Inventory.Public;

public enum EquipmentSlot
{
    Weapon = 0,
    Body = 1,
    Head = 2,
    Shoes = 3,
    Accessory = 4,
}

/// <summary>装配物定义。Stub 数值非原版表（source: stub）。</summary>
public sealed record EquipmentItem(
    int Id,
    string Name,
    EquipmentSlot Slot,
    int WeaponBlockId,
    int AtkBonus,
    int DefBonus,
    int HpBonus);

public interface IEquipCatalog
{
    IReadOnlyList<EquipmentItem> All { get; }
    bool TryGet(int itemId, out EquipmentItem item);
    EquipmentItem? DefaultWeaponForNpc(int npcId);
}

/// <summary>切片占位目录（source: stub）。</summary>
public sealed class StubEquipCatalog : IEquipCatalog
{
    public const int TrialSwordId = 9001;
    public const int DefaultSwordId = 9000;

    private readonly Dictionary<int, EquipmentItem> _items;

    public StubEquipCatalog()
    {
        var list = new[]
        {
            new EquipmentItem(DefaultSwordId, "Stub Blade", EquipmentSlot.Weapon, WeaponBlockId: 2000, AtkBonus: 5, DefBonus: 0, HpBonus: 0),
            new EquipmentItem(TrialSwordId, "Trial Fang", EquipmentSlot.Weapon, WeaponBlockId: 2000, AtkBonus: 25, DefBonus: 0, HpBonus: 0),
            new EquipmentItem(9100, "Stub Coat", EquipmentSlot.Body, WeaponBlockId: 0, AtkBonus: 0, DefBonus: 8, HpBonus: 20),
        };
        _items = list.ToDictionary(i => i.Id);
        All = list;
    }

    public IReadOnlyList<EquipmentItem> All { get; }

    public bool TryGet(int itemId, out EquipmentItem item) => _items.TryGetValue(itemId, out item!);

    public EquipmentItem? DefaultWeaponForNpc(int npcId) =>
        _items.TryGetValue(DefaultSwordId, out var w) ? w : null;
}
```

- [ ] **Step 4: Run 确认通过**

Run: 同 Step 2  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Qhlc.Inventory/Public tests/Qhlc.Core.Tests
git commit -m "feat(inventory): stub equip catalog for character OOP"
```

---

### Task 3: Character 继承树与 Equip

**Files:**
- Create: `src/Qhlc.Player/Public/CharacterStats.cs`
- Create: `src/Qhlc.Player/Public/AppearanceBlocks.cs`
- Create: `src/Qhlc.Player/Public/EquipmentLoadout.cs`
- Create: `src/Qhlc.Player/Public/BehaviorState.cs`
- Create: `src/Qhlc.Player/Public/Character.cs`
- Create: `src/Qhlc.Player/Public/PlayerCharacter.cs`
- Create: `src/Qhlc.Player/Public/NpcCharacter.cs`
- Create: `tests/Qhlc.Core.Tests/CharacterEquipTests.cs`
- Modify: `tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj`（引用 Player 若尚未）

**Interfaces:**
- Consumes: `IEquipCatalog`, `EquipmentItem`, `EquipmentSlot`
- Produces:
  - `readonly record struct CharacterStats(int MaxHp, int Hp, int MaxSp, int Sp, int Atk, int Def, int Intel, int Agi, int Element)`
  - `readonly record struct AppearanceBlocks(int BodyBlockId, int WeaponBlockId)`
  - `enum BehaviorState { Idle, Walk, BattleIdle }`
  - `abstract class Character` with `FootX/FootY`, `Facing`, `Behavior`, `BaseStats`, `Equip(...)`, `EffectiveStats()`, `AppearanceBlocks()`, `bool TryEquip(int itemId, out string error)`
  - `class PlayerCharacter : Character`
  - `class NpcCharacter : Character`

- [ ] **Step 1: 写失败测试**

```csharp
using Qhlc.Inventory.Public;
using Qhlc.Player.Public;

public class CharacterEquipTests
{
    [Fact]
    [Trait("TraceId", "TEST-0005")]
    public void Equip_trial_sword_raises_effective_atk()
    {
        var cat = new StubEquipCatalog();
        var hero = new PlayerCharacter(
            id: 10001,
            name: "Hero",
            bodyBlockId: 2000,
            baseStats: new CharacterStats(100, 100, 50, 50, 20, 10, 10, 10, 0),
            catalog: cat);
        hero.TryEquip(StubEquipCatalog.DefaultSwordId, out _);
        var before = hero.EffectiveStats().Atk;
        Assert.True(hero.TryEquip(StubEquipCatalog.TrialSwordId, out _));
        Assert.True(hero.EffectiveStats().Atk > before);
        Assert.Equal(2000, hero.GetAppearanceBlocks().BodyBlockId);
    }
}
```

- [ ] **Step 2: Run 确认失败**

Run: `dotnet test tests/Qhlc.Core.Tests --filter FullyQualifiedName~CharacterEquipTests`  
Expected: FAIL

- [ ] **Step 3: 实现 Character 树（最小可测）**

```csharp
namespace Qhlc.Player.Public;

public readonly record struct CharacterStats(
    int MaxHp, int Hp, int MaxSp, int Sp, int Atk, int Def, int Intel, int Agi, int Element);

public readonly record struct AppearanceBlocks(int BodyBlockId, int WeaponBlockId);

public enum BehaviorState { Idle = 0, Walk = 1, BattleIdle = 2 }

public sealed class EquipmentLoadout
{
    private readonly Dictionary<EquipmentSlot, int> _equipped = new();
    public IReadOnlyDictionary<EquipmentSlot, int> Equipped => _equipped;
    public void Set(EquipmentSlot slot, int itemId) => _equipped[slot] = itemId;
    public bool TryGet(EquipmentSlot slot, out int itemId) => _equipped.TryGetValue(slot, out itemId);
}

public abstract class Character
{
    private readonly IEquipCatalog _catalog;
    protected Character(
        int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
    {
        Id = id; Name = name; BodyBlockId = bodyBlockId;
        BaseStats = baseStats; _catalog = catalog;
        Loadout = new EquipmentLoadout();
    }

    public int Id { get; }
    public string Name { get; }
    public int BodyBlockId { get; }
    public CharacterStats BaseStats { get; protected set; }
    public EquipmentLoadout Loadout { get; }
    public int FootX { get; set; }
    public int FootY { get; set; }
    public int Facing { get; set; } = 4;
    public BehaviorState Behavior { get; set; } = BehaviorState.Idle;

    public bool TryEquip(int itemId, out string error)
    {
        error = "";
        if (!_catalog.TryGet(itemId, out var item))
        {
            error = $"unknown item {itemId}";
            return false;
        }
        Loadout.Set(item.Slot, item.Id);
        return true;
    }

    public CharacterStats EffectiveStats()
    {
        var s = BaseStats;
        var atk = s.Atk; var def = s.Def; var maxHp = s.MaxHp;
        foreach (var id in Loadout.Equipped.Values)
        {
            if (!_catalog.TryGet(id, out var it)) continue;
            atk += it.AtkBonus; def += it.DefBonus; maxHp += it.HpBonus;
        }
        var hp = Math.Min(s.Hp + (maxHp - s.MaxHp), maxHp);
        return s with { Atk = atk, Def = def, MaxHp = maxHp, Hp = hp };
    }

    public AppearanceBlocks GetAppearanceBlocks()
    {
        var weapon = BodyBlockId;
        if (Loadout.TryGet(EquipmentSlot.Weapon, out var wid)
            && _catalog.TryGet(wid, out var w) && w.WeaponBlockId > 0)
            weapon = w.WeaponBlockId;
        return new AppearanceBlocks(BodyBlockId, weapon);
    }

    public virtual void TickBehavior(double dt) { }
}

public sealed class PlayerCharacter : Character
{
    public PlayerCharacter(int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
        : base(id, name, bodyBlockId, baseStats, catalog) { }
}

public sealed class NpcCharacter : Character
{
    public NpcCharacter(int id, string name, int bodyBlockId, CharacterStats baseStats, IEquipCatalog catalog)
        : base(id, name, bodyBlockId, baseStats, catalog) { }
}
```

- [ ] **Step 4: Run 确认通过**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Qhlc.Player/Public tests/Qhlc.Core.Tests/CharacterEquipTests.cs
git commit -m "feat(player): Character hierarchy with equip bonuses"
```

---

### Task 4: ActorAnimator（动画从 WorldMap 迁出）

**Files:**
- Create: `src/Qhlc.Player/Public/ActorAnimator.cs`
- Create: `tests/Qhlc.Core.Tests/ActorAnimatorTests.cs`

**Interfaces:**
- Consumes: `Character`, `JmaFacing`, `NtsJma`, `BehaviorState`
- Produces: `sealed class ActorAnimator` — `void Tick(double dt, Character c)`；`bool TryGetFrame(string assetsRoot, Character c, out NtsRaster raster, out int originX, out int originY)`；粘滞朝向；Walk↔Idle 切换才硬重置帧

- [ ] **Step 1: 写失败测试（纯逻辑相位，可 mock 无 assets 时跳过合成）**

```csharp
[Fact]
[Trait("TraceId", "TEST-0005")]
public void Stick_facing_keeps_walk_frame_phase()
{
    var anim = new ActorAnimator();
    var cat = new StubEquipCatalog();
    var c = new PlayerCharacter(1, "t", 2000, new CharacterStats(10,10,10,10,1,1,1,1,0), cat);
    c.Behavior = BehaviorState.Walk;
    c.Facing = 6;
    anim.NotifyFacing(6);
    anim.ForceFrameForTest(2);
    anim.NotifyFacing(6); // same
    Assert.Equal(2, anim.FrameIndex);
}
```

在 `ActorAnimator` 提供 `internal`/`public` 测试钩子 `ForceFrameForTest` / `FrameIndex` / `NotifyFacing`。

- [ ] **Step 2: Run 确认失败 → Step 3: 实现 → Step 4: PASS**

实现要点（自 `WorldMapController` 迁入）：
- `_walkFacingSeq` + `JmaFacing.StickWalkSeq`
- Idle → `StandSeqForWalkSeq`；Walk → walk seq
- `TryGetFrame` 调 `NtsJma.TrySynthesizeFrame(..., out originX, out originY)`；失败保留缓存帧

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(player): ActorAnimator extracted from world map"
```

---

### Task 5: CharacterFactory、Party、WorldSession

**Files:**
- Create: `src/Qhlc.Player/Public/Party.cs`
- Create: `src/Qhlc.Player/Public/CharacterFactory.cs`
- Modify: `src/Qhlc.World/Public/WorldSession.cs`
- Create: `tests/Qhlc.Core.Tests/CharacterFactoryTests.cs`
- Modify: 删除或 `[Obsolete]` `PartyState.cs` 并修所有引用

**Interfaces:**
- Produces:
  - `sealed class Party { PlayerCharacter Leader; IReadOnlyList<Character> Allies; IEnumerable<Character> Members { get; } }`
  - `static class CharacterFactory { static Party CreateParty(ResolvedContent content, IEquipCatalog catalog); static List<NpcCharacter> CreateMapNpcs(ResolvedContent content, IEquipCatalog catalog, IReadOnlyList<(int X,int Y)> positions); }`
  - `WorldSession` 属性：`PlayerCharacter Player`，`Party Party`，`IReadOnlyList<NpcCharacter> MapNpcs`，`NtsMap Map`；`TryEncounter` 用 Player 脚位 AABB

- [ ] **Step 1: 写 Factory 测试（无 assets 时用假 ResolvedContent）**

```csharp
[Fact]
[Trait("TraceId", "TEST-0005")]
public void Factory_builds_three_allies()
{
    var content = new ResolvedContent(
        10851, "test",
        new[] { 10001, 10002, 10003 },
        new[] { 10206, 10209 },
        new Dictionary<int, int> { [10001]=2000,[10002]=2000,[10003]=2000,[10206]=2000,[10209]=2000 });
    var party = CharacterFactory.CreateParty(content, new StubEquipCatalog());
    Assert.Equal(3, party.Members.Count());
    Assert.Equal(10001, party.Leader.Id);
}
```

Stats 来源：从 `NpcTable` 读真实属性（若 Factory 内打开表）；若为减负，切片可用 ContentResolver 已有 NpcId + 固定模板 Stats，并在注释标明 stub HP/Atk 回退。**优先**：`CharacterFactory` 接收可选 `Func<int, CharacterStats?>`；默认实现读 `NpcTable.TryGet`。

- [ ] **Step 2–4: TDD 实现 Factory / Party / WorldSession**

`WorldSession.TryEncounter`：用 `Player.FootX/Y` 与 `MapNpcs[i].FootX/Y` 做矩形相交（与现 20px hitbox 一致）。

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(world): session hosts Character party and map NPCs"
```

---

### Task 6: BattleUnitMapper + 战斗接线

**Files:**
- Create: `src/Qhlc.Combat/Public/BattleUnitMapper.cs`
- Modify: `scenes/combat/BattleController.cs`
- Create: `tests/Qhlc.Combat.Tests/BattleUnitMapperTests.cs`

**Interfaces:**
- Consumes: `Character.EffectiveStats()`, `Character.Id/Name`
- Produces: `static class BattleUnitMapper { static BattleUnit From(Character c, int team, int slot, bool leader); static List<BattleUnit> FromParty(Party party); }`

- [ ] **Step 1: 失败测试**

```csharp
[Fact]
[Trait("TraceId", "TEST-0005")]
public void Mapper_includes_weapon_atk_bonus()
{
    var cat = new StubEquipCatalog();
    var hero = new PlayerCharacter(1, "H", 2000, new CharacterStats(100,100,50,50,20,10,10,10,0), cat);
    hero.TryEquip(StubEquipCatalog.DefaultSwordId, out _);
    var baseAtk = BattleUnitMapper.From(hero, team: 0, slot: 0, leader: true).Atk;
    hero.TryEquip(StubEquipCatalog.TrialSwordId, out _);
    var boosted = BattleUnitMapper.From(hero, 0, 0, true).Atk;
    Assert.True(boosted > baseAtk);
}
```

- [ ] **Step 2–4: 实现 Mapper；BattleController 从 `WorldSession.Party` + 触发敌创建单位，不再手写 Create**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): map Character effective stats into BattleUnit"
```

---

### Task 7: 拆 WorldMap — ActorView / PathDriver / 瘦 Controller

**Files:**
- Create: `scenes/world/ActorView.cs`
- Create: `scenes/world/PlayerPathDriver.cs`
- Modify: `scenes/world/WorldMapController.cs`（删除内嵌动画/大幅缩减）
- Modify: `scenes/world/WorldMap.tscn`（按需）
- Modify: `Qhlc.Game.csproj`（Compile Include 新文件）
- Modify: `GameMain.cs`（构造 catalog，传入 session 所需）

**Interfaces:**
- `ActorView`：绑定 `Character` + `ActorAnimator`；`_Process` 里 `anim.Tick` + `TryGetFrame` + 设置 Sprite；Y-sort root 脚位
- `PlayerPathDriver`：原 `SetMoveTargetFromMouse` / `StepAlongPath`；成功移动时 `player.Behavior=Walk` 并更新 `Facing`；停止 → Idle
- `WorldMapController`：BuildWorld 调 Factory；生成 ActorView；持有 PathDriver；遇敌 `EnterBattle`

- [ ] **Step 1: 把动画/寻路方法移到新类（行为保持），Controller 只编排**

- [ ] **Step 2: 编译**

Run: `dotnet build Qhlc.Game.csproj -c Debug`  
Expected: 0 errors

- [ ] **Step 3: Godot 手测检查清单（写入 PR/CHG manual_review）**

1. 登录→走图八向与遮挡  
2. 按住鼠标连续走无跳帧闪脸  
3. 遇敌进战可结束回图  

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor(world): split ActorView and path driver from WorldMapController"
```

---

### Task 8: 试穿 HUD + 全量验收

**Files:**
- Modify: `scenes/world/WorldMapController.cs` 或 `scenes/ui/` 增加试穿按钮
- Modify: `governance/trace/changes/CHG-0005.yaml`（verification / manual_review.confirmed）
- Run: `scripts/check_all.ps1`

**Interfaces:**
- UI：按钮「试穿试刀」→ `session.Player.TryEquip(StubEquipCatalog.TrialSwordId, out _)` → 强制 `animator` 刷新

- [ ] **Step 1: 实现试穿按钮（CanvasLayer 下 Button）**

- [ ] **Step 2: 手测 — Equip 后 Atk 面板或战斗日志可见更高伤害**

- [ ] **Step 3: check_all**

```powershell
$env:QHLC_ACTIVE_CHG='CHG-0005'
powershell -NoProfile -File scripts/check_all.ps1
```

Expected: 全部通过

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(ui): stub weapon try-on on world map"
```

---

## Spec coverage（自审）

| Spec 项 | Task |
|---------|------|
| Character 继承树 | 3 |
| 逻辑装备 + Stub | 2, 3, 8 |
| 无 Player→Combat 环 / Mapper | 6 |
| Factory / Party / Session | 5 |
| 拆 WorldMap | 7 |
| ActorAnimator | 4 |
| 术语 / modules / CHG | 1 |
| 测试 TEST-0005 | 2–6 |
| 手测换装进战 | 8 |

## Placeholder scan

无 TBD/TODO；Stub 明确 `source: stub`。

## Type consistency

- `EquipmentItem` / `EquipmentSlot` / `IEquipCatalog` / `StubEquipCatalog.TrialSwordId` 贯穿 Task 2–8
- `Character.EffectiveStats()` / `GetAppearanceBlocks()` 供 Animator 与 Mapper
- `WorldSession.Player` 类型为 `PlayerCharacter`
