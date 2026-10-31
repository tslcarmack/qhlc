---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# Superpowers 与仓库治理配合

## 推荐技能链

1. **brainstorming** — 产出设计规格（`docs/superpowers/specs/`）。
2. **writing-plans** — 产出实现计划（`docs/superpowers/plans/`）。
3. 每个实现任务创建 **CHG-*** 变更单后再改 `src/`。
4. **verification-before-completion** — 运行 `scripts/check_all.ps1` 后再声称完成。

## 与 Cursor 规则

仓库 `.cursor/rules/governance.zh-CN.mdc` 强制 CHG 流程；与全局 Superpowers 插件并存，以本仓库规则为准。

## 禁止

- 无 CHG 修改游戏代码。
- 为通过 CI 修改 `.github/workflows` 或 `tools/Qhlc.Governance`（须 scope 含 `governance` 且用户明确批准）。
