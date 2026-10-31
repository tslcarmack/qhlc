---
name: governed-change
description: 在 QHLC 仓库中按 CHG 变更单执行受治理的代码修改
---

# 受治理变更

## 何时使用

用户要求改游戏逻辑、模块或测试，且仓库含 `governance/` 目录。

## 步骤

1. 复制 `governance/templates/CHG.template.yaml` 为新 `CHG-xxxx.yaml`。
2. 填写 `requirements`、`scope`、`impact`（含 `manual_review` 待确认项）。
3. 运行：`dotnet run --project tools/Qhlc.Governance -- impact CHG-xxxx`
4. 实现代码，并为测试添加 `[Trait("TraceId", "TEST-xxxx")]`.
5. 更新 CHG 的 `verification.tests` 与 `documentation` 路径。
6. 运行 `scripts/check_all.ps1`（或设置 `QHLC_ACTIVE_CHG`）。

## 禁止

无 CHG 改 `src/`；不得绕过 CLI 修改门禁配置。
