# AI 治理起步包 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在空仓库中交付可克隆/可叠加的 Godot 4 + C# 2D RPG 模板，以及 `governance/`、`Qhlc.Governance` CLI、脚本、CI、Cursor 规则与中文文档，满足已批准设计规格的全部验收项。

**Architecture:** `governance/registry/modules.yaml` 与九个子模块 `.csproj` 的 `ProjectReference` 构成双源架构校验；追溯对象以 YAML/Markdown 文件 + CLI 校验；变更以 `CHG-*` 驱动 `impact` 与 `check-all`；Godot 工程引用 C# 模块，CI 仅 `dotnet` 不跑 Godot 编辑器。

**Tech Stack:** Godot 4.x、.NET 8、C#、YamlDotNet、xUnit、GitHub Actions、PowerShell 7+、Cursor rules/skills。

## Global Constraints

- 治理范围：阶段一完整 + 阶段二骨架；阶段三仅 `governance/policies/future-multi-agent.zh-CN.md` 提纲。
- 交付形态：模板仓库 + `scripts/install-governance.ps1` 叠加包；`governance/` 为唯一权威来源。
- RPG 模块 ID：`MOD-CORE` … `MOD-UI` 共九个；程序集名 `Qhlc.Core` … `Qhlc.Ui`。
- 协作：单人 + AI；审批字段 `self`；CI 平台 GitHub Actions、`ubuntu-latest`、**.NET 8**。
- 文档：规范性内容简体中文；代码与契约字段英文；REQ/CHG `title`/`acceptance` 中文。
- AI：仓库内置 Cursor 规则与技能；无 CHG 不得改 `src/`；禁止 AI 为通过检查修改 `.github/workflows`、`governance/policies`、`tools/Qhlc.Governance`（除非 CHG `scope` 含 `governance` 且用户明确批准）。
- CI：不运行 headless Godot；行为验证用 `dotnet test`。
- 首版：无完整示例 CHG 演练内容；QUICKSTART 含首个 CHG 占位步骤。
- 默认分支：使用 **`main`**（与 CI/workflow 一致）。

## File Structure（实现后）

| 路径 | 职责 |
|------|------|
| `project.godot` | Godot 4 工程入口，指向 C# 主场景 |
| `Qhlc.Game.csproj` | Godot.NET.Sdk 游戏宿主，引用九模块 |
| `qhlc.sln` | 解决方案（九模块 + Game + Governance + 测试） |
| `src/Qhlc.*/` | 九模块类库，每模块 `Public/` 命名空间 |
| `tests/Qhlc.*.Tests/` | 模块单元测试 |
| `tests/Qhlc.Integration.Tests/` | 关键链路 TEST-0001 |
| `governance/registry/modules.yaml` | 模块依赖 manifest |
| `governance/registry/terms.zh-CN.yaml` | 术语 |
| `governance/registry/contracts/*.yaml` | API 契约 |
| `governance/trace/**` | REQ/CHG/ADR 实例 |
| `governance/templates/**` | 空模板 |
| `governance/policies/**` | 语言、AI 权限、阶段三预留 |
| `tools/Qhlc.Governance/` | `check` / `impact` / `verify-chg` CLI |
| `scripts/check_all.ps1` | 本地全量门禁 |
| `scripts/install-governance.ps1` | 叠加安装 |
| `.cursor/rules/governance.zh-CN.mdc` | Cursor 硬规则 |
| `.cursor/skills/governed-change/SKILL.md` | 变更流程技能 |
| `.cursor/skills/module-boundary/SKILL.md` | 模块边界技能 |
| `.github/workflows/ci.yml` | lint / governance / test |
| `.github/pull_request_template.md` | CHG 审计字段 |
| `docs/governance/QUICKSTART.zh-CN.md` | 上手 |
| `docs/governance/superpowers-integration.zh-CN.md` | Superpowers 配合 |

---

### Task 1: 仓库基础与默认分支

**Files:**
- Create: `.gitignore`, `.editorconfig`, `Directory.Build.props`, `global.json`
- Modify: git branch `master` → `main`

**Interfaces:**
- Produces: `global.json` 固定 SDK `8.0.100`（或仓库可用的 8.0.x）；`Directory.Build.props` 统一 `LangVersion=12`, `Nullable=enable`, `TreatWarningsAsErrors=true`

- [ ] **Step 1:** 创建 `.gitignore`（Godot `.godot/`, `bin/`, `obj/`, `.vs/`, `*.user`, `.idea/`）

- [ ] **Step 2:** 创建 `global.json`

```json
{
  "sdk": {
    "version": "8.0.404",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 3:** 创建 `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4:** `git branch -m main`

- [ ] **Step 5:** 提交

```bash
git add .gitignore .editorconfig Directory.Build.props global.json
git commit -m "chore: add repo foundation and pin dotnet 8"
```

---

### Task 2: 治理 registry、模板与策略

**Files:**
- Create: `governance/registry/modules.yaml`, `governance/registry/terms.zh-CN.yaml`
- Create: `governance/templates/REQ.template.yaml`, `CHG.template.yaml`, `API.template.yaml`
- Create: `governance/policies/doc-language.zh-CN.md`, `governance/policies/ai-permissions.zh-CN.md`, `governance/policies/future-multi-agent.zh-CN.md`
- Create: `governance/registry/contracts/API-0001.yaml`（占位：Save 快照 DTO）

**Interfaces:**
- Produces: `modules.yaml` 九模块条目；依赖图无环：`Ui→Core`, `Player→Core`, `Combat→Core,Player`, `Inventory→Core,Player`, `Quest→Core`, `Dialogue→Core,Quest`, `World→Core`, `Save→Core`, `Core→[]`

- [ ] **Step 1:** 编写完整 `governance/registry/modules.yaml`（每模块含 `id`, `name`, `assembly`, `description_zh`, `public_namespaces`, `depends_on`, `scene_roots: ["scenes/{short}/**"]`, `owner: self`, `scripts_allowed: false`；`Core` 的 `scripts_allowed: true` 且 `scene_roots` 含 `src/Qhlc.Core/Godot/**` 若采用该目录）

- [ ] **Step 2:** 编写 `terms.zh-CN.yaml`（至少：玩家/Player、物品/Item、任务/Quest、存档/Save 四条，含 `forbidden_aliases`）

- [ ] **Step 3:** 复制设计规格 §6.3/§6.4 结构到 `governance/templates/` 三个模板文件

- [ ] **Step 4:** 编写三份 policies（中文正文）；`future-multi-agent` 仅提纲

- [ ] **Step 5:** 创建 `API-0001.yaml`：

```yaml
id: API-0001
title: 存档快照 DTO
owner_module: MOD-SAVE
version: 1.0.0
consumers:
  - MOD-UI
  - MOD-PLAYER
schema:
  type: object
  required: [saveVersion, playerId]
  properties:
    saveVersion:
      type: integer
    playerId:
      type: string
```

- [ ] **Step 6:** 提交 `git commit -m "feat(governance): add registry, templates, and policies"`

---

### Task 3: Godot 工程与九模块 C# 骨架

**Files:**
- Create: `project.godot`, `Qhlc.Game.csproj`, `qhlc.sln`
- Create: `src/Qhlc.{Core,Player,Combat,Inventory,Quest,Dialogue,World,Save,Ui}/*.csproj`
- Create: 每模块 `Public/ModuleMarker.cs`（空壳公开类型）
- Create: `scenes/core/Main.tscn`（最小占位）、`assets/.gitkeep`

**Interfaces:**
- Produces: 程序集 `Qhlc.Core` … `Qhlc.Ui`；`ProjectReference` 必须与 `modules.yaml` 的 `depends_on` 一致
- `Qhlc.Game.csproj` 使用 `Godot.NET.Sdk/4.3.0`（或 4.2.x 与本地 Godot 一致，在 README 注明版本）

- [ ] **Step 1:** 用 `dotnet new sln -n qhlc` 创建解决方案

- [ ] **Step 2:** 九模块：

```bash
dotnet new classlib -n Qhlc.Core -o src/Qhlc.Core
# 对其余八个模块重复，命名 Qhlc.Player … Qhlc.Ui
dotnet sln qhlc.sln add src/Qhlc.Core/Qhlc.Core.csproj # … 全部
```

- [ ] **Step 3:** 按 manifest 添加 `ProjectReference`（示例 `Qhlc.Combat.csproj`）：

```xml
<ItemGroup>
  <ProjectReference Include="..\Qhlc.Core\Qhlc.Core.csproj" />
  <ProjectReference Include="..\Qhlc.Player\Qhlc.Player.csproj" />
</ItemGroup>
```

- [ ] **Step 4:** 创建 `Qhlc.Game.csproj`：

```xml
<Project Sdk="Godot.NET.Sdk/4.3.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Qhlc.Game</RootNamespace>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src\Qhlc.Core\Qhlc.Core.csproj" />
    <!-- 引用全部九模块 -->
  </ItemGroup>
</Project>
```

- [ ] **Step 5:** 最小 `project.godot`（config_version=5，`run/main_scene` 指向 `scenes/core/Main.tscn`）

- [ ] **Step 6:** 运行 `dotnet build qhlc.sln`，期望 SUCCESS

- [ ] **Step 7:** 提交 `feat: add Godot project and nine module assemblies`

---

### Task 4: Qhlc.Governance CLI 脚手架（TDD）

**Files:**
- Create: `tools/Qhlc.Governance/Qhlc.Governance.csproj`, `Program.cs`
- Create: `tools/Qhlc.Governance/RepoPaths.cs`
- Create: `tests/Qhlc.Governance.Tests/Qhlc.Governance.Tests.csproj`, `RepoPathsTests.cs`

**Interfaces:**
- Produces: `RepoPaths.FindRepoRoot()` 从 cwd 向上查找含 `governance/registry/modules.yaml` 的目录；找不到抛 `InvalidOperationException`
- CLI 入口：`dotnet run --project tools/Qhlc.Governance -- <command>`

- [ ] **Step 1: 写失败测试**

```csharp
// tests/Qhlc.Governance.Tests/RepoPathsTests.cs
using Qhlc.Governance;
using Xunit;

public class RepoPathsTests
{
    [Fact]
    public void FindRepoRoot_finds_governance_folder()
    {
        var root = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
        Assert.True(File.Exists(Path.Combine(root, "governance", "registry", "modules.yaml")));
    }
}
```

- [ ] **Step 2:** `dotnet test tests/Qhlc.Governance.Tests` → FAIL（类型不存在）

- [ ] **Step 3:** 实现

```csharp
// tools/Qhlc.Governance/RepoPaths.cs
namespace Qhlc.Governance;

public static class RepoPaths
{
    public static string FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir != null)
        {
            var marker = Path.Combine(dir.FullName, "governance", "registry", "modules.yaml");
            if (File.Exists(marker)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("未找到 governance/registry/modules.yaml，请在仓库根目录执行。");
    }
}
```

- [ ] **Step 4:** `Program.cs` 解析子命令占位：`check`, `impact`, `verify-chg`（未知命令 exit 1）

- [ ] **Step 5:** 测试 PASS；提交 `feat(tools): add governance CLI scaffold`

---

### Task 5: 模块 manifest 与 csproj 架构检查

**Files:**
- Create: `tools/Qhlc.Governance/ModuleManifest.cs`, `ArchitectureChecker.cs`
- Create: `tests/Qhlc.Governance.Tests/ArchitectureCheckerTests.cs`

**Interfaces:**
- Consumes: `ModuleManifest.Load(repoRoot)` → `IReadOnlyDictionary<string, ModuleEntry>`（key 为 `MOD-CORE`）
- `ArchitectureChecker.Validate(repoRoot)` → `IReadOnlyList<string>` 错误信息；空列表表示通过
- 规则：每个 csproj 的 `ProjectReference` 解析为程序集名，映射 MOD；引用必须在 `depends_on` 内；manifest 中有但 csproj 缺失的依赖报错；检测环（DFS）

- [ ] **Step 1:** 测试「当前仓库无越界时 Validate 返回空」

- [ ] **Step 2:** 测试「临时添加 Ui→Inventory 非法引用时返回错误」（测试内写 temp csproj 或使用 `[Fact(Skip=...)]` 改 Inventory 引用后断言 — 推荐在测试中复制 minimal manifest 到 Temp 目录）

- [ ] **Step 3:** 实现 `ModuleManifest`（YamlDotNet 反序列化 `List<ModuleEntry>`）

- [ ] **Step 4:** 实现 `ArchitectureChecker`（扫描 `src/**/*.csproj` 的 `ProjectReference Include`）

- [ ] **Step 5:** `Program.cs` 子命令 `check architecture` 调用并打印错误，非 0 退出

- [ ] **Step 6:** 提交 `feat(tools): architecture check against modules.yaml`

---

### Task 6: 契约校验与文档 front matter

**Files:**
- Create: `tools/Qhlc.Governance/ContractChecker.cs`, `DocLanguageChecker.cs`
- Create: `tests/Qhlc.Governance.Tests/ContractCheckerTests.cs`, `DocLanguageCheckerTests.cs`

**Interfaces:**
- `ContractChecker.Validate(repoRoot)`：每个 `governance/registry/contracts/API-*.yaml` 必含 `id`, `consumers`, `owner_module`；`owner_module` 存在于 manifest
- `DocLanguageChecker.Validate(repoRoot)`：扫描 `docs/governance/**/*.md` 与 `governance/trace/adrs/**/*.md`；front matter 必含 `status`, `module`, `applies_from`；`status: deprecated` 时 `replaces` 非空

- [ ] **Step 1:** 测试 deprecated 无 replaces 失败

- [ ] **Step 2:** 测试 API 缺 consumers 失败

- [ ] **Step 3:** 实现两 checker

- [ ] **Step 4:** `check contracts` 与 `check doc-language`  wired 到 CLI

- [ ] **Step 5:** 为 `docs/governance/QUICKSTART.zh-CN.md`（Task 12 创建）预留 front matter 在 Task 12 一并满足

- [ ] **Step 6:** 提交

---

### Task 7: CHG 校验与 impact 命令

**Files:**
- Create: `tools/Qhlc.Governance/ChangeRecord.cs`, `ChangeValidator.cs`, `ImpactAnalyzer.cs`
- Create: `governance/trace/changes/CHG-0001.yaml`（本起步包实现变更）
- Create: `governance/trace/requirements/REQ-0001.yaml`
- Create: `tests/Qhlc.Governance.Tests/ChangeValidatorTests.cs`

**Interfaces:**
- `ChangeValidator.Validate(repoRoot, chgId)`：文件 `governance/trace/changes/{id}.yaml` 存在；`requirements` 中每个 REQ 文件存在；`verification.tests` 每个 TEST 在 `tests/**` 的 `[Trait("TraceId", "TEST-xxxx")]` 或 `governance/registry/test-trace.yaml` 登记
- `ImpactAnalyzer.Run(repoRoot, chgId)`：输出模块闭包（反向 depends_on + API consumers）；`impact.manual_review` 项须含 `confirmed: true` 或列表为空

- [ ] **Step 1:** 创建 `REQ-0001.yaml`（标题：治理起步包可构建）与 `CHG-0001.yaml`（scope 含 `governance`, `core`；`verification.tests: [TEST-0001]`；`manual_review: []`）

- [ ] **Step 2:** 测试 CHG 引用不存在的 REQ 失败

- [ ] **Step 3:** 测试 `manual_review` 含未确认项失败

- [ ] **Step 4:** 实现 Validator + Analyzer

- [ ] **Step 5:** CLI：`impact CHG-0001` 打印中文摘要；`verify-chg CHG-0001`

- [ ] **Step 6:** `check all` 聚合 `architecture`, `contracts`, `doc-language`, `verify-chg CHG-0001`（或通过环境变量 `QHLC_ACTIVE_CHG=CHG-0001`）

- [ ] **Step 7:** 提交

---

### Task 8: 游戏测试骨架与 TEST-0001

**Files:**
- Create: `tests/Qhlc.Core.Tests/`, `tests/Qhlc.Integration.Tests/CriticalPathTests.cs`
- Modify: `qhlc.sln`

**Interfaces:**
- Produces: `[Trait("TraceId", "TEST-0001")]` 集成测试 `SaveSnapshot_roundtrip_placeholder`（纯 C#：实例化 `Qhlc.Save.Public.SaveSnapshot` DTO，序列化 JSON 再反序列化，断言 `saveVersion`）

- [ ] **Step 1:** 在 `Qhlc.Save/Public/SaveSnapshot.cs` 实现与 API-0001 对齐的属性

- [ ] **Step 2:** 写集成测试（xUnit Trait）

- [ ] **Step 3:** `dotnet test qhlc.sln` PASS

- [ ] **Step 4:** 更新 `REQ-0001` 的 `implements.tests: [TEST-0001]`

- [ ] **Step 5:** 提交

---

### Task 9: check_all.ps1 与架构越界验收

**Files:**
- Create: `scripts/check_all.ps1`

**Interfaces:**
- 顺序：`dotnet format --verify-no-changes qhlc.sln` → `dotnet run --project tools/Qhlc.Governance -- check all` → `dotnet test qhlc.sln --no-build`（或 test 前 build）

- [ ] **Step 1:** 实现 `check_all.ps1`，任何步骤失败 `$LASTEXITCODE` 非 0

- [ ] **Step 2:** 本地运行 PASS

- [ ] **Step 3:** 临时在 `Qhlc.Ui` 添加对 `Qhlc.Inventory` 的引用，运行 `check all` 期望 FAIL，然后 revert

- [ ] **Step 4:** 提交

---

### Task 10: install-governance.ps1

**Files:**
- Create: `scripts/install-governance.ps1`, `scripts/install-manifest.txt`（列出需复制的相对路径）

**Interfaces:**
- 参数：`-TargetPath`（必填），`-WhatIf`
- 行为：复制 manifest 中路径；`.cursor/rules` 冲突备份到 `.cursor/rules.bak/<timestamp>/`；若目标已有 `.github/workflows/ci.yml` 则只 `Write-Host` diff 提示

- [ ] **Step 1:** 编写 manifest 列表（governance/, tools/Qhlc.Governance/, scripts/, .cursor/rules/, .cursor/skills/, .github/ 可选）

- [ ] **Step 2:** 在 `$env:TEMP/qhlc-install-test` 空目录试跑，断言 `governance/registry/modules.yaml` 存在

- [ ] **Step 3:** 在 `docs/governance/QUICKSTART.zh-CN.md` 增加叠加安装一节

- [ ] **Step 4:** 提交

---

### Task 11: Cursor 规则与技能

**Files:**
- Create: `.cursor/rules/governance.zh-CN.mdc`
- Create: `.cursor/skills/governed-change/SKILL.md`, `.cursor/skills/module-boundary/SKILL.md`

**Interfaces:**
- 规则 `alwaysApply: true`：中文；强制 CHG；禁止无批准改 CI/ policies / Governance 工具

- [ ] **Step 1:** 编写 `governance.zh-CN.mdc`（与设计 §9 一致）

- [ ] **Step 2:** 两技能文件（步骤：创建 CHG → impact → 更新 TEST trait → check_all）

- [ ] **Step 3:** 提交

---

### Task 12: GitHub Actions 与 PR 模板

**Files:**
- Create: `.github/workflows/ci.yml`, `.github/pull_request_template.md`

**Interfaces:**
- Jobs: `lint`（format verify）、`governance`（`dotnet run ... check all`，env `QHLC_ACTIVE_CHG=CHG-0001`）、`test`（dotnet test）
- PR 模板字段：CHG ID、REQ、check-all 摘要

- [ ] **Step 1:** 编写 `ci.yml`（trigger: pull_request, push branches: [main]）

```yaml
name: ci
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
jobs:
  lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet format --verify-no-changes qhlc.sln
  governance:
    runs-on: ubuntu-latest
    env:
      QHLC_ACTIVE_CHG: CHG-0001
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet run --project tools/Qhlc.Governance -- check all
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet test qhlc.sln -c Release
```

- [ ] **Step 2:** PR 模板（中文说明）

- [ ] **Step 3:** 本地 `dotnet format` + `check all` + `test` 全绿

- [ ] **Step 4:** 提交

---

### Task 13: 中文文档与 ADR 种子

**Files:**
- Create: `docs/governance/QUICKSTART.zh-CN.md`, `docs/governance/superpowers-integration.zh-CN.md`, `docs/governance/release-checklist.zh-CN.md`
- Create: `governance/trace/adrs/ADR-0001.md`（模块化单体与 C# 为主）
- Modify: 根目录 `README.md`（中文简介 + 链接 QUICKSTART）

**Interfaces:**
- 每份 md 含合法 front matter（`module: MOD-CORE` 或 `governance` 对应说明）

- [ ] **Step 1:** QUICKSTART：克隆 → build → 首个 CHG 占位步骤 → check_all → 开 PR

- [ ] **Step 2:** superpowers-integration：brainstorming / writing-plans / verification-before-completion 与 CHG 关系

- [ ] **Step 3:** ADR-0001 中文正文

- [ ] **Step 4:** `dotnet run --project tools/Qhlc.Governance -- check doc-language` PASS

- [ ] **Step 5:** 提交

---

### Task 14: 规格验收对照（verification-before-completion）

**Files:**
- Modify: `governance/trace/changes/CHG-0001.yaml`（标记完成状态可选字段 `status: merged`）

- [ ] **Step 1:** 运行规格 §12 清单逐项打勾写入 `docs/governance/VERIFICATION-2026-10-31.md`（简体中文，front matter）

- [ ] **Step 2:** `scripts/check_all.ps1` 最终 PASS

- [ ] **Step 3:** 提交 `docs: add verification record for governance starter`

- [ ] **Step 4:** 可选：更新 `CHG-0001` 的 `documentation` 列表包含 VERIFICATION 文件

---

## Plan Self-Review

**Spec coverage:** §3–§11 均映射 Task 2–14；§13 future-multi-agent → Task 2；§12 验收 → Task 14；Godot 工程 → Task 3；双形态 → Task 10。

**Placeholder scan:** 无 TBD；Task 3 Godot SDK 版本需在 README 与 `Qhlc.Game.csproj` 写死同一版本号。

**Type consistency:** CLI 子命令统一为 `check architecture|contracts|doc-language|all`；环境变量 `QHLC_ACTIVE_CHG` 在 Task 7/9/12 一致。

**Gap fixed:** `test-trace.yaml` 可选；优先 Trait `TraceId`，在 Task 7 Step 1 的 CHG-0001 与 Task 8 对齐 TEST-0001。

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-10-31-ai-governance-starter.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — 每个 Task 单独 subagent，任务间人工/Agent 复核  
2. **Inline Execution** — 本会话按 Task 顺序执行，每 2–3 个 Task 设检查点

**Which approach?**
