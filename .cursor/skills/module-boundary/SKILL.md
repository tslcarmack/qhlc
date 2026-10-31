---
name: module-boundary
description: 新增或修改 QHLC 模块边界、API 契约与 modules.yaml
---

# 模块边界

## 何时使用

新增模块、跨模块引用、或新增对外 API/DTO。

## 步骤

1. 更新 `governance/registry/modules.yaml` 的 `depends_on` 与 `public_namespaces`。
2. 同步 `src/` 下 csproj 的 `ProjectReference`。
3. 新增 `governance/registry/contracts/API-xxxx.yaml`，声明 `consumers`。
4. 若属重大结构决策，新增 `governance/trace/adrs/ADR-xxxx.md`。
5. 运行 `dotnet run --project tools/Qhlc.Governance -- check architecture` 与 `check contracts`。

## ADR 触发条件

- 改变模块数量或 Save 数据所有权
- 允许 GDScript 或绕过 Public API 的引用
