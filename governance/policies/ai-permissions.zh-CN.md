---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# AI 权限与执行约束

## 阶段流程

理解需求 → 影响分析 → 用户确认 scope → 任务分支实现 → check-all → 用户合并。

## 权限

| 对象 | AI 默认 |
|------|---------|
| `src/`、`tests/` | 须有关联 CHG，且在任务分支 |
| `governance/trace/` | 随 CHG 更新 |
| `governance/policies/`、`tools/Qhlc.Governance/`、`.github/workflows/` | 禁止，除非 CHG 的 `scope` 含 `governance` 且用户在对话中明确批准 |

## 禁止行为

- 无 CHG 修改游戏代码。
- 为通过 CI 而修改门禁或检查规则。
- 写入密钥或删除审计记录。

## 单人模式

审批字段可使用 `self`；确定性检查以 CLI 与 CI 为准，第二 Agent 复核不能替代脚本。
