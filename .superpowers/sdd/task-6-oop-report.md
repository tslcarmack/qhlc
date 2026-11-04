# Task 6 Report: BattleUnitMapper + 战斗接线

## 状态

**完成** — `BattleUnitMapperTests` 1/1；`check_all` 通过。

## 交付物

| 文件 | 说明 |
|---|---|
| `src/Qhlc.Combat/Public/BattleUnitMapper.cs` | `From(Character)` / `FromParty(Party)`；读 `EffectiveStats()` |
| `tests/Qhlc.Combat.Tests/BattleUnitMapperTests.cs` | TEST-0005：试刀后 Atk 升高 |
| `scenes/combat/BattleController.cs` | `Session.Party` + `MapNpcs` 映射；移除 NpcTable 手写 Create |
| `GameMain.cs` | `MountBattle` 注入 stashed `WorldSession` |

## 验证

```text
dotnet test tests/Qhlc.Combat.Tests --filter BattleUnitMapperTests → 1 passed
$env:QHLC_ACTIVE_CHG='CHG-0005'; .\scripts\check_all.ps1 → pass
```

## 设计要点

- Mapper 在 Combat 侧消费 Player `Character`，无 Player→Combat 反向引用。
- 0 HP 单位不进战列表（我方/敌方均跳过）。
- `Uid` 仍为 `team * 1000 + slot + 1`。

## Commit

`feat(combat): map Character effective stats into BattleUnit`

## 变更单

`CHG-0005`
