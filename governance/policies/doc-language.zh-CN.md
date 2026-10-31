---
status: active
module: MOD-CORE
applies_from: 0.1.0
replaces: null
---

# 文档语言政策

## 适用范围

本政策适用于 `docs/governance/`、`governance/policies/`、`governance/trace/` 中的 REQ/CHG 标题与验收条目，以及 ADR 正文。

## 规则

1. 规范性说明使用**简体中文**。
2. C# 标识符、Godot 节点名、契约字段名使用**英文**。
3. 第三方协议与 API 名称保留原名。
4. 术语以 `governance/registry/terms.zh-CN.yaml` 为准，禁止随意混用表中列出的禁用别名。

## 检查

合并前运行 `dotnet run --project tools/Qhlc.Governance -- check doc-language`。
