# Task 6 Report: ContentResolver

## 状态

**完成** — `ContentResolverTests` 与全量 `Qhlc.Core.Tests` 11/11 通过（本地 `assets/` 存在时）。

## 交付物

| 文件 | 说明 |
|---|---|
| `src/Qhlc.Core/Nts/ContentResolver.cs` | §4.4 选组：Scene 表序 → 地图 + 贴图 → 3 友 2 敌 + JMA |
| `src/Qhlc.Core/Public/SliceModels.cs` | `ResolvedContent` 记录 |
| `src/Qhlc.Core/Nts/NtsArchive.cs` | `ContainsEntry`（供后续归档查询） |
| `tests/Qhlc.Core.Tests/ContentResolverTests.cs` | 条件集成测 |

## 验证

```text
dotnet test tests/Qhlc.Core.Tests --filter FullyQualifiedName~ContentResolverTests
→ 1 passed

dotnet test tests/Qhlc.Core.Tests
→ 11 passed
```

### 实测锁定（`d:\code\qhlc` + 完整 assets）

```text
ContentResolver: Scene=<Big5 名> AreaId=10851 allies=[10001,10002,10003] enemies=[10206,10209] blocks=[10001->2000,...]
```

- 未触发 `LastResolveWarning`：Npc 表按序可凑满 Faction 1–4 我方 3 人 + 非 1–4 敌方 2 人。
- 块号：五人均 `JmaWhitelist.Blocks[i % 1] = 2000`，`TrySynthesizeFrame(..., seq:0, frame:0)` 全过。

## 设计要点

- **Scene 序**：`SceneTable.Load`（已跳过 idx0 版本卡）；逐条试 `AreaId`。
- **地图**：`NtsMap.LoadFromGround`；`tiles[].Scene` 须在 `assets/pic/Map*.JMg` 有条目 `{scene}.jpg`（与 tsdocs `mapindex.ts` 一致）；可走格 `(gx∈[0,gw-2], gy∈[0,gh-2])` 且 `grid==0`，计数 ≥ **50**。
- **物件 meta**：不解析、不否决（§4.4 第 3 点）。
- **阵营**：`IsAllyFaction` = Faction ∈ {1,2,3,4}；敌方 = 其余；表内按序各取 3/2。不足时 `LastResolveWarning` + 前 3 / 接下来 2 条 Npc。
- **JMA**：禁止 ModelId 查块；绑定 `Blocks[i % len]` 并合成校验。
- **失败**：零候选 → `InvalidOperationException`（中文「无完整 Scene/Npc/jma 组合」）。
- **日志**：`Console.WriteLine` 输出 Scene 名、AreaId、NpcId、块映射。

## Commit

`feat(core): ContentResolver auto-picks playable slice content`

## 变更单

`CHG-0004`（scope: core, tests）
