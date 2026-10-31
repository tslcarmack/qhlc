---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# 快速上手（治理起步包）

## 克隆模板

1. 克隆本仓库，安装 [.NET 8 SDK](https://dotnet.microsoft.com/download) 与 Godot 4.3（与 `Qhlc.Game.csproj` 中 SDK 版本一致）。
2. 运行 `dotnet build qhlc.sln`。
3. 用 Godot 打开 `project.godot`。

## 首个变更（占位）

1. 复制 `governance/templates/CHG.template.yaml` 为 `governance/trace/changes/CHG-0002.yaml`。
2. 填写 `requirements`、`scope`、`impact`、`verification.tests`。
3. 运行 `dotnet run --project tools/Qhlc.Governance -- impact CHG-0002`。
4. 在任务分支修改代码，运行 `scripts/check_all.ps1`。
5. 提交 PR，在描述中填写 `CHG-0002`。

## 叠加到已有项目

```powershell
./scripts/install-governance.ps1 -TargetPath D:\path\to\your\godot-game
```

## 环境变量

- `QHLC_ACTIVE_CHG`：`check all` 时校验的变更单，默认 `CHG-0001`。
