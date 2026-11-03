# NTS 垂直切片 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Godot 4.7 .NET 仓库中交付纯单机垂直切片：运行时解析 `assets/` 原包 → 登录壳 → 自动选可用地图行走 → 小队参考公式战斗 → 回地图。

**Architecture:** 纯 C# 解析与战斗规则落在 `Qhlc.Core` / `Qhlc.Combat`（可单测）；Godot 场景与输入在 `Ui` / `World` / `Player` / `GameMain`。`ContentResolver` 按表序锁定第一组完整资源。伤害使用 tsdocs `referenceFormula`（`Source = "reference"`）。

**Tech Stack:** Godot 4.7.2 .NET、C# / .NET 8、xUnit、现有 `Qhlc.Governance` 门禁；对照实现 `assets/tsdocs/99_附录_源码/`（tsotool）。

**Spec:** `docs/superpowers/specs/2026-11-03-nts-vertical-slice-design.md`

## Global Constraints

- 无已确认 scope 的 `CHG-*.yaml` 不得改 `src/` / `tests/`。
- 不得为过检修改 `.github/workflows/`、`governance/policies/`、`tools/Qhlc.Governance/`（除非 CHG `scope` 含 `governance` 且用户批准）。
- 规范性文档简体中文；代码标识符与契约字段英文。
- 术语以 `governance/registry/terms.zh-CN.yaml` 为准。
- 完成前跑 `scripts/check_all.ps1`（或 `scripts/check_all.cmd`）。
- 伤害公式必须标记 `reference`，禁止文案宣称原版公式。
- `ModelId` 不得直接当 jma 块号；外观用白名单块号取模绑定。
- 本期 N=3 / M=2；Inventory/Quest/Dialogue/Save 零功能。
- Core 解析层不引用 `Godot` 程序集；栅格输出为 `NtsRaster`（RGBA 字节），Godot 纹理转换放在场景模块。

## File Structure（实现后）

| 路径 | 职责 |
|------|------|
| `governance/trace/requirements/REQ-0003.yaml` | 垂直切片需求 |
| `governance/trace/changes/CHG-0004.yaml` | 本期变更单 |
| `governance/registry/terms.zh-CN.yaml` | 增补 NTS/ContentResolver 等术语 |
| `src/Qhlc.Core/Nts/NtsBuf.cs` | 小端缓冲读写 |
| `src/Qhlc.Core/Nts/NtsText.cs` | 反序 Big5（cp950） |
| `src/Qhlc.Core/Nts/NtsScramble.cs` | 逐字段解扰 |
| `src/Qhlc.Core/Nts/NtsContainer.cs` | `.Dat` 容器判定与 XOR 解密记录 |
| `src/Qhlc.Core/Nts/NtsTableFile.cs` | 定长表打开 → `IReadOnlyList<byte[]>` 原始行 |
| `src/Qhlc.Core/Nts/NtsPaths.cs` | `assets/` 根解析 |
| `src/Qhlc.Core/Nts/Tables/SceneTable.cs` | `Scene.Dat` → `SceneRecord` |
| `src/Qhlc.Core/Nts/Tables/NpcTable.cs` | `Npc.Dat` → `NpcRecord` |
| `src/Qhlc.Core/Nts/Tables/SkillTable.cs` | `Skill.Dat` → `SkillRecord` |
| `src/Qhlc.Core/Nts/NtsGround.cs` | `Ground.MMG` / `.map` / `CanWalk` |
| `src/Qhlc.Core/Nts/NtsArchive.cs` | `.BMg` / `.jmg` 条目读取 |
| `src/Qhlc.Core/Nts/NtsBmp.cs` | BMP → `NtsRaster`（含黑底软抠） |
| `src/Qhlc.Core/Nts/NtsJma.cs` | jma/jxa 白名单块合成帧 |
| `src/Qhlc.Core/Nts/ContentResolver.cs` | 自动选 Scene+队伍+敌 |
| `src/Qhlc.Core/Public/SliceModels.cs` | 对外 DTO：`ResolvedContent`、`NtsRaster` 等 |
| `src/Qhlc.Core/Public/ISliceHost.cs` | 状态机接口（可选） |
| `src/Qhlc.Combat/Public/BattleUnit.cs` | 战斗单位 |
| `src/Qhlc.Combat/Public/ReferenceFormula.cs` | 参考伤害 |
| `src/Qhlc.Combat/Public/Battle.cs` | 回合引擎 |
| `src/Qhlc.Player/Public/PartyState.cs` | 当前小队 NpcId 列表 |
| `src/Qhlc.Player/Public/PlayerMover.cs` | 纯逻辑移动尝试（可测） |
| `src/Qhlc.World/Public/WorldSession.cs` | 地图会话：碰撞、敌人占位 |
| `src/Qhlc.Ui/Public/` | 登录/HUD 辅助（文本常量） |
| `scenes/ui/Login.tscn` + `LoginController.cs` | 登录壳 |
| `scenes/world/WorldMap.tscn` + `WorldMapController.cs` | 走图 |
| `scenes/combat/Battle.tscn` + `BattleController.cs` | 战斗表现 |
| `GameMain.cs` | 状态机编排 |
| `tests/Qhlc.Core.Tests/` | 解析与 Resolver 单测 |
| `tests/Qhlc.Combat.Tests/` | 战斗与参考公式单测 |

---

### Task 1: 登记 REQ/CHG/术语（实现前门禁）

**Files:**
- Create: `governance/trace/requirements/REQ-0003.yaml`
- Create: `governance/trace/changes/CHG-0004.yaml`
- Modify: `governance/registry/terms.zh-CN.yaml`

**Interfaces:**
- Produces: 用户可确认的 scope：`core`, `player`, `combat`, `world`, `ui`, `tests`（不含 `governance` 工具改动）

- [ ] **Step 1: 写 REQ-0003**

```yaml
id: REQ-0003
title: NTS 资源垂直切片可玩闭环
module: MOD-CORE
status: active
acceptance:
  - 冷启动可进入登录壳并进入自动解析出的地图
  - 地图上可移动且不可穿墙
  - 接触敌人可进入小队回合战并分出胜负后回到地图
  - 伤害结算标记为 reference，非宣称原版公式
  - scripts/check_all.ps1 通过
implements:
  apis: []
  tests:
    - TEST-0003
    - TEST-0004
related_adrs: []
```

- [ ] **Step 2: 写 CHG-0004**

```yaml
id: CHG-0004
title: 实现 NTS 垂直切片（登录壳-走图-小队战）
requirements:
  - REQ-0003
scope:
  - core
  - player
  - combat
  - world
  - ui
  - tests
impact:
  modules:
    - MOD-CORE
    - MOD-PLAYER
    - MOD-COMBAT
    - MOD-WORLD
    - MOD-UI
  apis: []
  data_migration: false
  manual_review:
    - 需在 Godot 编辑器中人工点一遍登录→走图→战斗
acceptance:
  - 符合 REQ-0003 全部验收项
  - ContentResolver 启动日志打印 Scene/Npc/块号
verification:
  tests:
    - TEST-0003
    - TEST-0004
  checks:
    - architecture
    - contracts
    - doc-language
documentation:
  - docs/superpowers/specs/2026-11-03-nts-vertical-slice-design.md
recovery:
  rollback: code_only
  notes: ""
approvals:
  implementer: self
  reviewer: optional
  merged_by: self
```

- [ ] **Step 3: 术语追加**（在 `terms:` 列表末尾）

```yaml
  - concept: nts_asset_runtime
    name_zh: NTS资源运行时
    identifier_en: NtsAssetRuntime
    forbidden_aliases:
      - 资源烘焙器
  - concept: content_resolver
    name_zh: 内容解析器
    identifier_en: ContentResolver
    forbidden_aliases:
      - 关卡选择器
  - concept: reference_damage
    name_zh: 参考伤害公式
    identifier_en: ReferenceFormula
    forbidden_aliases:
      - 原版伤害公式
```

- [ ] **Step 4: 向用户确认 CHG-0004 scope**（对话中明确列出 scope 列表，等确认后再改 `src/`）

- [ ] **Step 5: Commit**

```bash
git add governance/trace/requirements/REQ-0003.yaml governance/trace/changes/CHG-0004.yaml governance/registry/terms.zh-CN.yaml
git commit -m "docs: register REQ-0003 and CHG-0004 for NTS vertical slice"
```

---

### Task 2: Core 文本与解扰基础（TDD）

**Files:**
- Create: `src/Qhlc.Core/Nts/NtsBuf.cs`
- Create: `src/Qhlc.Core/Nts/NtsText.cs`
- Create: `src/Qhlc.Core/Nts/NtsScramble.cs`
- Create: `tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj`
- Create: `tests/Qhlc.Core.Tests/NtsTextTests.cs`
- Create: `tests/Qhlc.Core.Tests/NtsScrambleTests.cs`
- Modify: `qhlc.sln`（加入测试项目）

**Interfaces:**
- Produces:
  - `NtsBuf.ReadU16Le(ReadOnlySpan<byte> s, int offset) -> ushort`
  - `NtsBuf.ReadU32Le(...)`
  - `NtsText.DecodeReversedBig5(ReadOnlySpan<byte> field) -> string`
  - `NtsText.DecodeNpcName(ReadOnlySpan<byte> rec97) -> string`（`+0` 长度，`+1..+10` 反序）
  - `NtsScramble.Unscramble(uint raw, int width, uint key, int bias) -> uint`
- Consumes: 无

- [ ] **Step 1: 建测试项目并写失败测试**

`tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj` 参照 `tests/Qhlc.Integration.Tests`，`ProjectReference` 指向 `..\..\src\Qhlc.Core\Qhlc.Core.csproj`。

```csharp
namespace Qhlc.Core.Tests;

public class NtsTextTests
{
    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void DecodeReversedBig5_ZhangJiao_name_slot()
    {
        // 「張角」Big5 = B1 69 A8 A4；槽宽 10，右对齐，前导 0x00
        var slot = new byte[] { 0, 0, 0, 0, 0, 0, 0xA4, 0xA8, 0x69, 0xB1 };
        Assert.Equal("張角", Qhlc.Core.Nts.NtsText.DecodeReversedBig5(slot));
    }
}

public class NtsScrambleTests
{
    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void Unscramble_u16_roundtrip_inverse()
    {
        const uint key = 0xECEA;
        const int bias = 5;
        uint raw = 0x1234;
        var plain = Qhlc.Core.Nts.NtsScramble.Unscramble(raw, 2, key, bias);
        var back = Qhlc.Core.Nts.NtsScramble.Scramble(plain, 2, key, bias);
        Assert.Equal(raw, back);
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
dotnet test tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj --filter TraceId=TEST-0003
```

Expected: FAIL（类型不存在）

- [ ] **Step 3: 实现**

`NtsText.DecodeReversedBig5`：拷贝字段 → `Array.Reverse` → 去尾 `0x00` → `Encoding.GetEncoding(950).GetString`（禁止替换回退；非法则返回空串并可供日志）。

`NtsScramble`（对照 `assets/tsdocs/99_附录_源码/src/core/scramble.ts`）：

```csharp
public static uint Unscramble(uint raw, int width, uint key, int bias)
{
    uint mask = width switch { 1 => 0xffu, 2 => 0xffffu, 4 => 0xffffffffu, _ => throw new ArgumentOutOfRangeException(nameof(width)) };
    uint mod = width switch { 1 => 0x100u, 2 => 0x10000u, 4 => 0u /* use ulong */, _ => 0 };
    uint x = (raw ^ (key & mask)) & mask;
    if (width == 4)
    {
        ulong y = (x + (ulong)(0x100000000L - bias)) & 0xffffffffUL;
        return (uint)y;
    }
    return (uint)((x + (mod - (uint)bias)) % mod) & mask;
}
```

`Scramble` 为逆运算（先加 bias 再 XOR），与 ts 一致。`NtsBuf` 用 `BinaryPrimitives`。

- [ ] **Step 4: 测试通过后 Commit**

```bash
git add src/Qhlc.Core/Nts tests/Qhlc.Core.Tests qhlc.sln
git commit -m "feat(core): add NTS text and scramble primitives"
```

---

### Task 3: `.Dat` 容器与 Scene/Npc/Skill 表

**Files:**
- Create: `src/Qhlc.Core/Nts/NtsPaths.cs`
- Create: `src/Qhlc.Core/Nts/NtsContainer.cs`
- Create: `src/Qhlc.Core/Nts/NtsTableFile.cs`
- Create: `src/Qhlc.Core/Nts/Tables/SceneTable.cs`
- Create: `src/Qhlc.Core/Nts/Tables/NpcTable.cs`
- Create: `src/Qhlc.Core/Nts/Tables/SkillTable.cs`
- Create: `src/Qhlc.Core/Public/SliceModels.cs`（表 DTO 部分）
- Create: `tests/Qhlc.Core.Tests/NtsTableTests.cs`

**Interfaces:**
- Produces:
  - `NtsPaths.ResolveAssetsRoot(string? overridePath = null) -> string`（默认：从 `AppContext.BaseDirectory` 向上找含 `assets/data/Item.Dat` 的目录，否则 `Directory.GetCurrentDirectory()`）
  - `NtsTableFile.Open(string path, int recordSize) -> NtsTableFile`（跳过 idx0 版本卡的迭代器 `EnumerateDataRows()`）
  - `SceneTable.Load(string assetsRoot) -> IReadOnlyList<SceneRecord>`
  - `NpcTable.Load(...) -> IReadOnlyList<NpcRecord>`
  - `SkillTable.Load(...) -> IReadOnlyList<SkillRecord>`
- `SceneRecord(int AreaId, byte Seq, byte MapSet, string Name)`
- `NpcRecord(int Id, string Name, int ModelId, int Level, int Hp, int Sp, int Atk, int Def, int Int, int Agi, int Faction)`
- `SkillRecord(int Id, string Name, int Kind, int Mp, int P32, int TargetParam)`

- [ ] **Step 1: 写失败测试（有 assets 才跑全量）**

```csharp
public class NtsTableTests
{
    private static string? AssetsRoot()
    {
        var root = Qhlc.Core.Nts.NtsPaths.ResolveAssetsRoot();
        return File.Exists(Path.Combine(root, "assets", "data", "Npc.Dat")) ? root : null;
    }

    [Trait("TraceId", "TEST-0003")]
    [Fact]
    public void NpcTable_skips_version_card_and_decodes_name()
    {
        var root = AssetsRoot();
        if (root is null) return; // 无资源环境跳过
        var rows = Qhlc.Core.Nts.Tables.NpcTable.Load(root);
        Assert.True(rows.Count >= 100);
        Assert.Contains(rows, n => n.Name.Contains('張') || n.Name.Length > 0);
    }

    [Fact]
    public void SceneTable_areaId_positive()
    {
        var root = AssetsRoot();
        if (root is null) return;
        var scenes = Qhlc.Core.Nts.Tables.SceneTable.Load(root);
        Assert.True(scenes[0].AreaId > 0);
    }
}
```

- [ ] **Step 2: 实现容器**

记录长常量（无头）：`Npc=97`，`Scene=134`，`Skill=86`（见 tsdocs §03.3）。

`NtsContainer` 解密逻辑移植自 `assets/tsdocs/99_附录_源码/src/core/container.ts` + §03.1：第 0 条明文；奇数号记录与末条/倒数第二条整段 XOR（严格按 ts 实现逐字节对齐，禁止自创变体）。

字段读取：
- Scene：解扰 `AreaId`（查 descramble 规格；若本期嵌入最小规格表，至少覆盖 Scene `+0` u16）。名称 `+4..+23` 为**正向**定长 Big5（§04.4），用 `Encoding.GetEncoding(950)` 直接解，去尾 0。
- Npc：名称用 `NtsText.DecodeNpcName`；数值字段按 §04.2 偏移读取。若该表需逐字段解扰，从 tsotool `spec`/`scramble` 嵌入 Npc 字段 KEY 表；无解扰则明文读（以 tsotool `npc.ts` 为准）。
- Skill：对照 `assets/tsdocs/99_附录_源码/src/game/skills.ts` 与 §04.3 映射 `Id/Name/Kind/Mp/p32/target`。

- [ ] **Step 3: 测试通过 + Commit**

```bash
git add src/Qhlc.Core tests/Qhlc.Core.Tests
git commit -m "feat(core): load Scene, Npc, and Skill tables from assets"
```

---

### Task 4: Ground.MMG 与 CanWalk

**Files:**
- Create: `src/Qhlc.Core/Nts/NtsGround.cs`
- Create: `tests/Qhlc.Core.Tests/NtsGroundTests.cs`
- Create: `tests/Qhlc.Core.Tests/Fixtures/canwalk_toy.bin`（可选小夹具）或仅 assets 条件测

**Interfaces:**
- Produces:
  - `NtsMap.LoadFromGround(string assetsRoot, int areaId) -> NtsMap`
  - `NtsMap` 属性：`int ImgW, ImgH, Gw, Gh`；`byte[] Grid`；`IReadOnlyList<NtsMapTile> Tiles`；`bool CanWalk(int px, int py)`
- `CanWalk` 必须与 §05.2 一致：

```csharp
public bool CanWalk(int px, int py)
{
    int gx = px / 20, gy = py / 20;
    if (gx < 0 || gy < 0 || gx >= Gw - 1 || gy >= Gh - 1) return false;
    return Grid[gx * Gh + gy] == 0;
}
```

- [ ] **Step 1: 失败测试**

```csharp
[Fact]
public void CanWalk_matches_column_major_rule()
{
    var map = new Qhlc.Core.Nts.NtsMap(
        imgW: 40, imgH: 40, gw: 3, gh: 3,
        grid: new byte[] { 0, 1, 1,  0, 1, 1,  1, 1, 1 },
        tiles: Array.Empty<Qhlc.Core.Nts.NtsMapTile>());
    Assert.True(map.CanWalk(0, 0));
    Assert.False(map.CanWalk(0, 20)); // grid[0*3+1]==1
    Assert.False(map.CanWalk(100, 0)); // OOB
}
```

- [ ] **Step 2: 实现** — 目录定位与 `.map` 切段移植 `assets/tsdocs/99_附录_源码/src/format/ground.ts`（公开方法签名保持 `LoadFromGround` / `CanWalk`）。`tiles[].scene` 保留为 `int Scene` + `int X` + `int Y`。

- [ ] **Step 3: 若有 assets，追加实测** `AreaId=10001` 可走格计数 > 0（§05.2 称 145）。

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): parse Ground.MMG and CanWalk"
```

---

### Task 5: 图像归档、BMP、JMA 白名单合成

**Files:**
- Create: `src/Qhlc.Core/Nts/NtsArchive.cs`
- Create: `src/Qhlc.Core/Nts/NtsBmp.cs`
- Create: `src/Qhlc.Core/Nts/NtsJpeg.cs`（或用 `System.Drawing`/`ImageSharp`——**禁止**新增未批准 NuGet；优先手写 JPEG 解码失败时：对 `.jpg` 条目用 Godot 侧解码。Core 仅返回 `byte[]` 压缩载荷 + 元数据，`NtsRaster` 仅对 BMP。）
- Create: `src/Qhlc.Core/Nts/NtsJma.cs`
- Create: `src/Qhlc.Core/Nts/JmaWhitelist.cs`
- Create: `tests/Qhlc.Core.Tests/NtsBmpTests.cs`

**Interfaces:**
- Produces:
  - `NtsArchive.Open(string path) -> NtsArchive`
  - `bool TryReadEntry(string entryName, out byte[] payload)`
  - `NtsBmp.Decode(ReadOnlySpan<byte> bmp) -> NtsRaster`（RGBA；纯黑 RGB(0,0,0) alpha=0）
  - `JmaWhitelist.Blocks -> int[]` 初始至少含 `2000`（§05.1 实证）
  - `NtsJma.TrySynthesizeFrame(string assetsRoot, int blockId, int seq, int frameIndex, out NtsRaster frame) -> bool`

**决策（钉死）：** 地图 JPEG 不在 Core 解码；`WorldMapController` 用 `Image.LoadJpgFromBuffer`。Core 的 `NtsArchive` 对 `.jmg` 只提供 JPEG 字节。

- [ ] **Step 1: BMP 单测** — 构造最小 2×1 的 24-bit BMP（一手写字节），断言解码宽高与像素。

- [ ] **Step 2: 实现 BMg 目录** — 移植 `assets/tsdocs/99_附录_源码/src/format/sprites.ts`（32 字节目录项）。

- [ ] **Step 3: JMA** — 移植 `jma.ts` / `jma_anim.ts` 的最小路径：给定块号 `2000`，合成一帧非全透明 `NtsRaster`。失败返回 `false`。

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): BMg archive, BMP decode, JMA whitelist synthesize"
```

---

### Task 6: ContentResolver

**Files:**
- Create: `src/Qhlc.Core/Nts/ContentResolver.cs`
- Create: `src/Qhlc.Core/Public/SliceModels.cs`（补全 `ResolvedContent`）
- Create: `tests/Qhlc.Core.Tests/ContentResolverTests.cs`

**Interfaces:**
- Produces:

```csharp
public sealed record ResolvedContent(
    int AreaId,
    string SceneName,
    IReadOnlyList<int> AllyNpcIds,
    IReadOnlyList<int> EnemyNpcIds,
    IReadOnlyDictionary<int, int> NpcIdToBlockId);

public static class ContentResolver
{
    public const int AllyCount = 3;
    public const int EnemyCount = 2;
    public static ResolvedContent Resolve(string assetsRoot);
}
```

规则严格按规格 §4.4：Scene 表序 → 地图 tiles JPEG 全命中且可走格 ≥ 50 → 物件缺图忽略 → Npc 按序选 Faction 可入队 3 人 + 敌 2 人 → 块号 `JmaWhitelist.Blocks[i % len]` → 第一组成功即返回；否则抛 `InvalidOperationException`。

Faction「可入队」钉死：`Faction` ∈ {1,2,3,4} 视为我方候选；`Faction` ∈ {0,5,6} 或其余视为敌方候选（若实数据导致零候选，放宽为：前 3 条有效 Npc 我方、接下来 2 条敌方，并在日志 warning）。实现时先跑 Resolver 一次，以能解析为准并在注释写清最终规则。

- [ ] **Step 1: 测试**（无 assets 则 return）

```csharp
[Fact]
public void Resolve_returns_3_allies_and_2_enemies()
{
    var root = NtsTableTests_Assets(); // 复用 Task3 探测
    if (root is null) return;
    var r = ContentResolver.Resolve(root);
    Assert.Equal(3, r.AllyNpcIds.Count);
    Assert.Equal(2, r.EnemyNpcIds.Count);
    Assert.True(r.AreaId > 0);
}
```

- [ ] **Step 2: 实现 + Commit**

```bash
git commit -am "feat(core): ContentResolver auto-picks playable slice content"
```

---

### Task 7: Combat 参考公式与回合引擎

**Files:**
- Create: `src/Qhlc.Combat/Public/BattleUnit.cs`
- Create: `src/Qhlc.Combat/Public/BattleAction.cs`
- Create: `src/Qhlc.Combat/Public/ReferenceFormula.cs`
- Create: `src/Qhlc.Combat/Public/Battle.cs`
- Create: `tests/Qhlc.Combat.Tests/Qhlc.Combat.Tests.csproj`
- Create: `tests/Qhlc.Combat.Tests/BattleTests.cs`
- Modify: `qhlc.sln`

**Interfaces:**
- Produces:
  - `ReferenceFormula.Source => "reference"`
  - `ReferenceFormula.Physical(...)` / `Magical(...)` 对齐 tsotool：
    - `coef = 0.6 + Min(p32,120)/120.0`
    - 物理：`max(1, atk*coef*1.5 - def*0.5) * crit * defend * elementMul`
    - 暴击率物理 0.05 / 法术 0.03，暴击 ×1.6；防御姿态 ×0.5
  - `Battle.FromParties(IReadOnlyList<BattleUnit> allies, IReadOnlyList<BattleUnit> enemies)`
  - `Battle.RunAuto(Random rng) -> BattleOutcome`（全自动直到胜负或 30 回合）
  - 定序：agi 降序；同 agi 我方先，再 slot
  - 结算：队长死 → 败；团灭；回合上限

- [ ] **Step 1: 失败测试**

```csharp
[Trait("TraceId", "TEST-0004")]
[Fact]
public void ReferenceFormula_source_is_reference()
{
    Assert.Equal("reference", ReferenceFormula.Source);
}

[Fact]
public void AutoBattle_finishes_with_winner()
{
    var allies = new[] { BattleUnit.Create(1, "A", team: 0, slot: 0, hp: 200, atk: 80, def: 10, intel: 10, agi: 50) };
    var enemies = new[] { BattleUnit.Create(2, "B", team: 1, slot: 0, hp: 50, atk: 5, def: 5, intel: 5, agi: 10) };
    var outcome = new Battle(allies, enemies).RunAuto(new Random(0));
    Assert.True(outcome.WinnerTeam is 0 or 1);
}
```

- [ ] **Step 2: 移植引擎骨架** — 从 `assets/tsdocs/99_附录_源码/src/game/battle.ts` 精简：仅攻击/技能（无道具/逃跑/捕捉）；敌我 AI = 活目标 + 普攻。`BattleUnit.Create` 填充 defaults（sp/skills 空则普攻）。

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(combat): reference formula and auto battle engine"
```

---

### Task 8: Player / World 纯逻辑会话

**Files:**
- Create: `src/Qhlc.Player/Public/PartyState.cs`
- Create: `src/Qhlc.Player/Public/PlayerMover.cs`
- Create: `src/Qhlc.World/Public/WorldSession.cs`
- Create: `tests/Qhlc.Core.Tests/PlayerMoverTests.cs`（或 `tests/Qhlc.Player.Tests`）

**Interfaces:**
- `PartyState(IReadOnlyList<int> AllyNpcIds, IReadOnlyList<int> EnemyNpcIds)`
- `PlayerMover.TryMove(NtsMap map, int x, int y, int dx, int dy) -> (int X, int Y)`：先试轴分离滑墙
- `WorldSession`：持有 `ResolvedContent`、`NtsMap`、玩家像素坐标、敌人世界坐标列表；`TryEncounter(playerRect) -> bool`；开战成功后 `MarkEncounterConsumed`

- [ ] **Step 1: PlayerMover 测试** — 目标格阻挡时坐标不变；一侧可走时只滑动可走轴。

- [ ] **Step 2: 实现 + Commit**

```bash
git commit -am "feat(player,world): movement and encounter session logic"
```

---

### Task 9: Godot 登录壳与状态机

**Files:**
- Modify: `GameMain.cs`
- Create: `scenes/ui/Login.tscn`
- Create: `src/Qhlc.Ui/Godot/LoginController.cs`（若 Ui 无 Godot SDK：脚本放在 `Game` 项目下 `scenes/ui/LoginController.cs` 并加入 `Qhlc.Game.csproj` Compile）
- Modify: `Qhlc.Game.csproj`（Compile 纳入新脚本）
- Modify: `project.godot`（主场景仍 `scenes/core/Main.tscn`）

**钉死：** 所有 `*.cs` 挂到场景上的脚本编入 `Qhlc.Game` 程序集（与现有 `GameMain.cs` 一致），调用各模块 Public API。避免给每个模块改 Godot SDK。

**Interfaces:**
- `GameMain` 状态：`Boot → Resolving → Login → WorldRoam ↔ Battle`
- Login：`Button`「进入游戏」；背景优先加载 `assets/menu/skins/default` 下任一面板 BMP（经 Core `NtsBmp` + `Image.CreateFromData`）；失败则 `ColorRect`

- [ ] **Step 1: 实现 Boot** — `_Ready` 中 `ResolveAssetsRoot`；缺文件则加 `Label` 中文错误，不进 Login。

- [ ] **Step 2: Resolving** — 调 `ContentResolver.Resolve`，`GD.Print` 锁定信息；缓存于 `GameMain` 字段。

- [ ] **Step 3: 切换子场景** — `ChangeState` 用 `AddChild`/`QueueFree` 挂载 Login/World/Battle 场景实例。

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(game): slice state machine and login shell"
```

---

### Task 10: WorldMap 走图与遇敌表现

**Files:**
- Create: `scenes/world/WorldMap.tscn`
- Create: `scenes/world/WorldMapController.cs`（编入 Game）
- Modify: `GameMain.cs`（进入 WorldRoam 时传入 `ResolvedContent`）

**行为:**
1. 用 `NtsMap.LoadFromGround` + 对每个 tile `NtsArchive` 读 JPEG → `Image.LoadJpgFromBuffer` → `Sprite2D` 按 `(t.X,t.Y)` 放置。
2. 主角 `Sprite2D`：用 `NtsJma.TrySynthesizeFrame` 第一帧；`Camera2D` 跟随。
3. `_Process`：读输入向量，经 `PlayerMover.TryMove` 更新位置。
4. 在可走格放置 2 个敌人精灵；`Area2D` 碰撞 → `GameMain.EnterBattle()`。

- [ ] **Step 1: 实现并在编辑器手动验证清单项 1–2（规格 §8.2）**

- [ ] **Step 2: Commit**

```bash
git commit -am "feat(world): map render, walk collision, encounter trigger"
```

---

### Task 11: Battle 场景接线与收尾

**Files:**
- Create: `scenes/combat/Battle.tscn`
- Create: `scenes/combat/BattleController.cs`
- Modify: `GameMain.cs`
- Modify: `tests/Qhlc.Integration.Tests/CriticalPathTests.cs`（可选：解析烟测）
- Modify: `tests/Qhlc.Integration.Tests/Qhlc.Integration.Tests.csproj`（引用 Core/Combat）

**行为:**
1. 从 `NpcTable` 填 3+2 `BattleUnit`（字段映射 §05.4）。
2. 每帧或 Timer 推进 `Battle` 自动回合；飘字可用 `Label`。
3. 结束后 `GameMain.ReturnToWorld(won: bool)`。
4. HUD 可用纯 Control 节点；有皮肤则贴 `bar_*` / `panel*`。

- [ ] **Step 1: 实现自动战斗 UI 循环**

- [ ] **Step 2: 跑门禁**

```powershell
dotnet test qhlc.sln
.\scripts\check_all.ps1
```

Expected: 全部通过。

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(combat-ui): battle scene and slice loop complete"
```

---

## Spec Coverage（自检）

| 规格项 | 任务 |
|--------|------|
| 运行时 C# 读包 | Task 2–5 |
| ContentResolver 自动选组 | Task 6 |
| 登录壳 | Task 9 |
| 走图 + CanWalk | Task 4, 8, 10 |
| 小队 N=3/M=2 遇敌 | Task 6, 8, 10 |
| 参考公式 + 全自动战斗 | Task 7, 11 |
| ModelId 缺口 / 白名单块号 | Task 5–6 |
| 错误：缺 assets / 零候选 | Task 9 |
| check_all | Task 11 |
| CHG/REQ/术语 | Task 1 |
| 不做联网/存档/背包 | 无对应任务（刻意） |

## Placeholder / 类型一致性（自检）

- 统一 `NtsRaster`、`ResolvedContent`、`BattleUnit`、`NtsMap.CanWalk(int,int)` 命名贯穿任务。
- JPEG 解码仅在 Game/World Godot 侧；Core 不引用 Godot。
- 无 TBD；Faction 放宽规则已写明决策路径。

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-11-03-nts-vertical-slice.md`. Two execution options:

**1. Subagent-Driven (recommended)** — 每个 Task 派一个新子代理，任务间审查，迭代快  

**2. Inline Execution** — 本会话用 executing-plans 按检查点批量执行  

Which approach?
