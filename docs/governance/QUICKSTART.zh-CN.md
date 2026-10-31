---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# 快速上手（治理起步包）

## 克隆模板

1. 克隆本仓库，安装 [.NET 8 SDK](https://dotnet.microsoft.com/download) 与 **Godot 4.7 .NET 版**（见下方「C# 与 .NET 版 Godot」）。
2. 运行 `dotnet build qhlc.sln`。
3. 用 **.NET 版** Godot 打开 `project.godot`（首次打开可在编辑器中执行「构建」/ Build 项目）。

### C# 与 .NET 版 Godot

本仓库根目录有 `Qhlc.Game.csproj`，**必须使用带 C#/.NET 模块的 Godot**，不能使用仅 GDScript 的标准版。

| 现象 | 原因 |
|------|------|
| 警告：「该项目使用 C#，但 Godot 的这个版本没有 Mono 模块」 | 当前 exe 是 **标准版** Godot，不含 .NET |

**处理步骤：**

1. 打开 [Godot 下载页](https://godotengine.org/download/windows/)（或你的平台）。
2. 下载与 **4.7** 同主版本的 **「.NET」** 包（Windows 常见文件名含 `mono` 或 `dotnet`，例如 `Godot_v4.7.x_mono_win64.zip` / `…-net_…`）。
3. 解压后用其中的 Godot 可执行文件打开本项目的 `project.godot`。
4. 确认本机已装 **.NET 8 SDK**（`dotnet --version` 显示 8.x）。
5. 编辑器菜单 **项目 → 工具 → C#**（或顶部 **Build**）构建一次；也可在仓库根目录执行 `dotnet build qhlc.sln`。

标准版 Godot 可继续用于纯 GDScript 项目；**本模板请固定使用 .NET 版 4.7**。

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
