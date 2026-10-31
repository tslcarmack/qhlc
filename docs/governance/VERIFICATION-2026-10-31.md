---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# 规格 §12 验收记录（2026-10-31）

| 项 | 结果 |
|----|------|
| `dotnet build` / `dotnet test` 通过 | 是 |
| 越界程序集引用时 check 失败 | 已用手工验证（临时 Ui→Inventory 引用） |
| CHG 缺少 TEST 关联时失败 | 由 ChangeValidator 覆盖 |
| deprecated 文档无 replaces 时失败 | DocLanguageChecker 覆盖 |
| `install-governance.ps1` 空目录试跑 | 是 |
| QUICKSTART 可跟随首个 CHG 流程 | 是（占位步骤） |

本地命令：

```powershell
dotnet build qhlc.sln -c Release
dotnet test qhlc.sln -c Release
dotnet run --project tools/Qhlc.Governance -- check all
./scripts/check_all.ps1
```
