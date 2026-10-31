# AI 治理起步包设计规格（Godot 4 2D RPG + Superpowers + Cursor）

| 字段 | 值 |
|------|-----|
| 状态 | 已批准（对话确认 2026-10-31） |
| 范围 | 阶段一完整 + 阶段二骨架；阶段三仅文档预留 |
| 交付形态 | 双形态：可克隆模板 + 可叠加治理包 |
| 技术底座 | Godot 4.x，C# 为主 |
| 协作 | 单人 + AI；CI 为 GitHub Actions |
| AI 框架 | Superpowers 流程 + 仓库内置 Cursor 规则与技能 |

## 1. 背景与目标

### 1.1 问题

在 Godot 2D RPG 从零演进过程中，常见工程化失败模式包括：

- 修改模块 A 的功能导致模块 B 行为异常（隐式耦合）。
- 修改 A 时遗漏同步 B（契约、存档、UI、测试、文档）。
- 历史变更无法回答「为何改、验证了啥」。
- 文档中英文混用、术语不一致，AI 与人类理解漂移。

### 1.2 目标

交付 **可直接复制使用** 的 AI 治理起步包，包含：

- 简体中文规范与结构化模板（REQ / CHG / ADR / 模块清单 / 术语库）。
- 依赖与变更影响检查（CLI + 脚本 + CI）。
- C# 测试骨架与 GitHub Actions 示例。
- Cursor 规则与仓库技能（与 Superpowers 插件配合说明）。
- 本地与 CI 可运行的验证路径。

### 1.3 非目标（首版）

- 不建设复杂图数据库或独立 AI 管理平台。
- 不实现阶段三：多 Agent 合并队列、跨仓库契约平台、指标看板（仅预留说明文档）。
- CI 不运行 headless Godot 编辑器；游戏逻辑验证在纯 C# 层与架构测试中完成。
- 首版不包含完整「示例 CHG 演练」仓库内容（QUICKSTART 保留第一个 CHG 步骤占位）。

## 2. 决策摘要

| 决策 | 选择 |
|------|------|
| 治理阶段 | 阶段一 + 阶段二骨架 |
| 仓库形态 | 模板仓库 + `install-governance` 叠加包 |
| 实现路径 | `governance/` 权威目录 + 轻量 `Qhlc.Governance` CLI |
| RPG 模块 | 九模块：core, player, combat, inventory, quest, dialogue, world, save, ui |
| 审批 | 单人自批；YAML/PR 字段预留双人审批升级 |
| CI | GitHub Actions，.NET 8 |
| AI 绑定 | 仓库内置规则与技能；Superpowers 负责 brainstorming / plans / verification 技能链 |

## 3. 总体架构

### 3.1 双形态交付

| 形态 | 用途 | 入口 |
|------|------|------|
| 模板仓库 | 从零开游戏 | 克隆仓库，打开 Godot，阅读 `docs/governance/QUICKSTART.zh-CN.md` |
| 叠加包 | 已有 Godot 项目 | `scripts/install-governance.ps1 -TargetPath <项目根>` |

两套共用 **`governance/` 为唯一权威来源**；`.cursor/`、`.github/` 由安装脚本复制或合并。

### 3.2 六子系统映射

1. **受治理的软件底座**：模块化单体，九模块各一 C# 程序集，显式依赖，禁止循环依赖与越界引用。
2. **规范与权威来源**：见第 4 节表格。
3. **需求—代码—测试追溯**：稳定 ID + YAML/Markdown + CLI 校验引用。
4. **标准化变更**：所有开发以 `CHG-*` 为单位。
5. **AI 执行与权限**：阶段流程 + 分支/范围约束；禁止 AI 为通过检查而改门禁。
6. **验证、审计与文档治理**：分层 CI + CHG 关联 PR + 文档语言可检查。

### 3.3 推荐目录结构

```text
qhlc/
├── project.godot
├── qhlc.sln
├── src/
│   ├── Qhlc.Core/
│   ├── Qhlc.Player/
│   ├── Qhlc.Combat/
│   ├── Qhlc.Inventory/
│   ├── Qhlc.Quest/
│   ├── Qhlc.Dialogue/
│   ├── Qhlc.World/
│   ├── Qhlc.Save/
│   └── Qhlc.Ui/
├── tests/
│   └── …（各模块测试 + 关键链路集成测试）
├── governance/
│   ├── registry/
│   │   ├── modules.yaml
│   │   ├── terms.zh-CN.yaml
│   │   └── contracts/
│   ├── trace/
│   │   ├── requirements/
│   │   ├── changes/
│   │   └── adrs/
│   ├── templates/
│   └── policies/
├── tools/
│   └── Qhlc.Governance/
├── scripts/
│   ├── check_all.ps1
│   └── install-governance.ps1
├── assets/ 或 scenes/          # 按 module 分子目录（manifest 登记）
├── .cursor/rules/
├── .cursor/skills/             # 或与 governance/skills 同步
├── .github/workflows/ci.yml
└── docs/
    ├── superpowers/specs/
    └── governance/
```

## 4. 权威来源

| 信息 | 权威来源 | 禁止重复维护 |
|------|----------|--------------|
| 业务目标、验收标准 | `governance/trace/requirements/REQ-*.yaml` | 不得在多处手写不同验收文案 |
| 模块职责、依赖 | `governance/registry/modules.yaml` + 程序集引用 | 架构规则以 manifest + CLI 为准 |
| 对外 API / 事件 / DTO | `governance/registry/contracts/API-*.yaml` | 实现与契约不一致时以冲突处理流程为准 |
| 数据结构演进 | 契约 + ADR +（未来）迁移脚本 | |
| 技术取舍 | `governance/trace/adrs/ADR-*.md` | |
| 运行时行为 | C# 代码 + `dotnet test` | 与 REQ 冲突必须开 CHG 解决 |
| 开发说明 | `docs/governance/**`（与版本同库） | |
| AI 流程 | `.cursor/rules` + 技能 + Superpowers 全局技能 | |

## 5. 模块化单体（九模块）

### 5.1 模块清单

| MOD ID | 目录程序集 | 职责摘要 |
|--------|------------|----------|
| MOD-CORE | Qhlc.Core | 启动、DI/服务定位、Godot 适配、跨模块基础设施 |
| MOD-PLAYER | Qhlc.Player | 玩家实体、输入、移动、属性 |
| MOD-COMBAT | Qhlc.Combat | 战斗规则、伤害、状态 |
| MOD-INVENTORY | Qhlc.Inventory | 物品、装备、背包 |
| MOD-QUEST | Qhlc.Quest | 任务状态机、目标 |
| MOD-DIALOGUE | Qhlc.Dialogue | 对话树、选项 |
| MOD-WORLD | Qhlc.World | 场景、地图、交互物 |
| MOD-SAVE | Qhlc.Save | 存档读写、版本迁移入口 |
| MOD-UI | Qhlc.Ui | HUD、菜单、展示 |

### 5.2 底座规则（须 CLI/测试强制执行）

- 模块间仅通过 **公开类型** 或 **contracts 中声明的事件/DTO** 通信。
- 禁止循环程序集引用。
- 禁止未经 manifest 允许的跨层调用（例如 UI 直接写 Inventory 内部类型）。
- 数据所有权：各模块拥有本域数据；跨模块写入须经 Save 或明确 ADR 的同步机制。
- GDScript：默认 `scripts_allowed: false`；若使用，仅允许 ADR 批准的目录（如 Core 内 Godot 胶水）。

### 5.3 modules.yaml 字段（最小集）

每个模块条目至少包含：

- `id`, `name`, `assembly`, `description_zh`
- `public_namespaces`
- `depends_on`（MOD ID 列表）
- `scene_roots`（glob）
- `owner`（单人模式下可为同一负责人）

## 6. 追溯系统

### 6.1 ID 规范

- `REQ-####` 需求
- `MOD-*` 模块（上表）
- `API-####` 契约
- `ADR-####` 决策记录
- `TEST-####` 测试追溯（C# Trait 或 sidecar 映射）
- `CHG-####` 变更单

文件路径与 `id` 一致，便于 grep 与 CI。

### 6.2 关系

```text
REQ → module → API / 数据 / 事件 → 实现 → TEST
                    ↓
              depends_on 模块

CHG → requirements, scope, impact, verification, documentation, recovery, approvals
```

### 6.3 REQ 模板（结构）

```yaml
id: REQ-0128
title: （简体中文）
module: MOD-COMBAT
status: draft | active | deprecated
acceptance:
  - （可验证条目）
implements:
  apis: []
  tests: []
related_adrs: []
```

### 6.4 CHG 模板（结构）

```yaml
id: CHG-0086
title: （简体中文）
requirements: []
scope: []                    # 模块短名：combat, ui, governance, …
impact:
  modules: []
  apis: []
  data_migration: false
  manual_review: []          # 待确认项；关闭前 check-all 失败
acceptance: []
verification:
  tests: []
  checks: [architecture, contracts, doc-language]
documentation: []
recovery:
  rollback: code_only | forward_fix_required
  notes: ""
approvals:
  implementer: self
  reviewer: optional
  merged_by: self
```

### 6.5 契约 consumers

每个 `API-*.yaml` 含 `consumers: [MOD-UI, …]`，impact 分析时展开下游。**无法静态分析的依赖**（报表口径、异步事件、外部 mod）必须列入 CHG 的 `manual_review` 并人工确认。

## 7. 影响分析（防改 A 忘 B）

命令：`dotnet run --project tools/Qhlc.Governance -- impact <CHG-id>`

输出报告包含：

1. **模块闭包**：scope 内模块的上游/下游（manifest `depends_on` 反向图 + consumers）。
2. **契约**：`impact.apis` 对应 consumers 与 schema 变更标记。
3. **追溯完整性**：REQ 与 TEST 是否列入 `verification`。
4. **文档**：`documentation` 路径存在且 front matter 合法。
5. **manual_review**：未确认则 exit code 非 0。

合并前本地与 CI 均运行 `check-all`（含 impact）。

## 8. 文档与术语治理

### 8.1 语言政策

- 规范性文档：简体中文（REQ/CHG title、acceptance、policies、ADR 正文、QUICKSTART）。
- 代码与契约字段名：英文。
- API 外部协议名称：保留原名。

### 8.2 术语库

`governance/registry/terms.zh-CN.yaml` 定义概念、中文名、英文标识、禁用别名。检查脚本对 `docs/governance` 与 trace 标题做抽样/规则扫描（允许代码块内英文）。

### 8.3 Markdown front matter

```yaml
---
status: active | deprecated
module: MOD-COMBAT
applies_from: 0.1.0
replaces: null
---
```

`deprecated` 必须提供 `replaces` 路径。

## 9. AI 执行与权限（单人 + AI）

### 9.1 阶段流程

```text
理解需求 → impact 分析 → 用户确认 scope → 任务分支实现
    → check-all → （可选）第二 Agent 复核 → 用户合并
```

### 9.2 权限矩阵

| 对象 | AI 默认 | 用户 |
|------|---------|------|
| src/, tests/ | 任务分支 + 有效 CHG | 合并 |
| governance/trace | 随 CHG 更新 | 批准 scope |
| governance/policies, tools/Qhlc.Governance, .github/workflows | 禁止，除非 CHG scope 含 governance 且对话明确批准 | 必须 |
| 生产密钥、审计删除 | 禁止 | — |

### 9.3 Cursor 规则要点

- 无 CHG 不得修改 `src/`。
- 不得为通过 CI 而修改门禁配置。
- 与 Superpowers：`brainstorming` → 本 spec 类文档 → `writing-plans` → 每任务 CHG → `verification-before-completion` 再声称完成。

### 9.4 仓库技能（内置）

- `governed-change`：创建/更新 CHG、运行 impact、更新 TEST 映射。
- `module-boundary`：新增 API、修改 modules.yaml、ADR 触发条件。

说明文档：`docs/governance/superpowers-integration.zh-CN.md`。

## 10. 验证与 CI

### 10.1 分层门禁

| 层级 | 检查 |
|------|------|
| 基础 | dotnet format、EditorConfig、可选密钥扫描 |
| 架构 | 模块依赖、无环、越界引用 |
| 契约 | API YAML schema、破坏性变更标记 |
| 行为 | dotnet test（单元 + 至少 1 条关键链路占位测试） |
| 变更 | PR 关联 CHG、impact 通过、文档语言 |
| 发布 | 文档 checklist（首版不自动化） |

### 10.2 本地入口

`scripts/check_all.ps1` 依次调用 format、governance CLI、test。

### 10.3 GitHub Actions

- Runner：`ubuntu-latest`，.NET 8。
- Jobs：`lint`、`governance`、`test`。
- PR 模板要求填写 `CHG-*`；workflow 解析并校验文件存在。

### 10.4 审计字段（PR 模板）

- CHG ID、关联 REQ
- impact 摘要
- check-all 结果（可选粘贴）
- 例外批准（如有）

Git 记录「怎么变」；CHG + PR 记录「为何变、验证过什么」。

## 11. 叠加安装脚本行为

`scripts/install-governance.ps1`：

1. 复制 `governance/`、`tools/Qhlc.Governance/`、`scripts/`（及技能/rules 源目录）。
2. 合并 `.cursor/rules`；冲突备份至 `.cursor/rules.bak/`。
3. 无 `ci.yml` 则复制；否则打印 diff 清单供人工合并。
4. 生成 `modules.yaml` stub，提示用户映射现有目录到九模块。

## 12. 实现完成验收标准

- [ ] `dotnet build` 与 `dotnet test` 在模板仓库通过。
- [ ] 故意添加越界程序集引用 → `check-all` 失败。
- [ ] CHG 缺少 `verification.tests` 中登记的 TEST → 失败。
- [ ] deprecated 文档无 `replaces` → 失败。
- [ ] `install-governance.ps1` 对空目录试跑成功（幂等或文档说明二次运行行为）。
- [ ] `docs/governance/QUICKSTART.zh-CN.md` 可让单人开发者完成首个 CHG 流程。

## 13. 阶段三预留

文件：`governance/policies/future-multi-agent.zh-CN.md`

内容提纲：多 Agent 隔离工作区、合并队列、跨仓库契约、缺陷逃逸率指标——**不在本实现计划内**。

## 14. 风险与缓解

| 风险 | 缓解 |
|------|------|
| C# 与 Godot 场景耦合难以静态分析 | scene_roots glob + ADR 登记；关键链路集成测试 |
| 单人自批流于形式 | 强制 CLI + CHG 字段；可选第二 Agent 复核说明 |
| 叠加安装覆盖用户 CI | 默认不覆盖已有 workflow，仅 diff |
| 治理过重拖慢早期原型 | scope 允许 `governance` 仅在有 CHG 时更新；YAGNI 模块内可先空壳 |

## 15. 附录：参考变更影响维度（战斗数值类）

调整伤害精度或公式时，impact 与 manual_review 应覆盖：数据与 Save 版本、Combat 计算、Buff 顺序（ADR）、UI 展示、Quest 条件（若依赖数值）、契约 consumers、边界测试与回归 TEST、中文规则文档。无法自动推断的项不得默认为「无影响」。

---

**文档维护**：实现过程中若目录或命令变更，须通过 scope 含 `governance` 的 CHG 更新本 spec 或 supersede 新 spec。
