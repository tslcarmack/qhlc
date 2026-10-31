---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# 快速上手（治理起步包）

## 克隆模板

1. 克隆本仓库，安装 [.NET 8 SDK](https://dotnet.microsoft.com/download) 与 **Godot 4.7**（与 `Qhlc.Game.csproj` 中 `Godot.NET.Sdk/4.7.2` 一致）。
2. 运行 `dotnet build qhlc.sln`。
3. 用 Godot 打开 `project.godot`。

`global.json` 仅使用 `rollForward: latestFeature`，不锁定具体 SDK 补丁号；需 **.NET 8** 或更高 feature  band。

## 示例变更 CHG-0002（已收录）

本仓库包含完整示例：**存档快照 playerId 不可为空**。

| 对象 | 路径 |
|------|------|
| 需求 | `governance/trace/requirements/REQ-0002.yaml` |
| 变更 | `governance/trace/changes/CHG-0002.yaml` |
| 实现 | `src/Qhlc.Save/Public/SaveSnapshot.cs` → `Create` |
| 测试 | `TEST-0002`（`CriticalPathTests.cs`） |

### 复现治理流程

```powershell
dotnet run --project tools/Qhlc.Governance -- impact CHG-0002
$env:QHLC_ACTIVE_CHG = "CHG-0002"
./scripts/check_all.ps1
```

影响分析会列出契约 `API-0001` 的消费者（`MOD-UI`、`MOD-PLAYER`），提醒改 Save 时同步关注展示与玩家模块。

### 自行新建 CHG-0003 时

1. 复制 `governance/templates/CHG.template.yaml`。
2. 新建 REQ（或使用已有 REQ）。
3. 为测试添加 `[Trait("TraceId", "TEST-xxxx")]` 并写入 CHG 的 `verification.tests`。
4. 设置 `QHLC_ACTIVE_CHG` 后运行 `check_all.ps1`。

## 叠加到已有项目

```powershell
./scripts/install-governance.ps1 -TargetPath D:\path\to\your\godot-game
```

## 环境变量

- `QHLC_ACTIVE_CHG`：`check all` 时校验的变更单，默认 `CHG-0001`（起步包元变更）；功能变更请设为当前 CHG。
