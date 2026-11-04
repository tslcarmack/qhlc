# Godot 皮肤 UI 管线与 SkinWindow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立可复制的皮肤 UI 管线：纵向切 `btn_*`、可拖动/关闭的 `SkinWindow`，并以登录壳 +「进入游戏」在编辑器中可视化验收。

**Architecture:** 纯逻辑辅助（切片矩形、拖拽钳制）可单测；Godot 场景模板 `SkinWindow` / `SkinButton` 供人手布局；`LoginController` 仅接线；不把布局硬编码进 `WorldMapController`。

**Tech Stack:** Godot 4.7 .NET、C# / .NET 8、xUnit、`Qhlc.Governance` 门禁。

**Spec:** `docs/superpowers/specs/2026-11-04-godot-skin-ui-pipeline-design.md`

## Global Constraints

- 无已确认 scope 的 `CHG-*.yaml` 不得改 `src/` / `tests/`（本期以 `scenes/ui` + Game 脚本为主；若测纯逻辑放 `tests/` 须 CHG 含 `tests`）。
- 规范性文档简体中文；代码标识符与契约字段英文。
- 术语以 `governance/registry/terms.zh-CN.yaml` 为准。
- 布局由人在编辑器完成；AI 不凭自然语言猜像素坐标作为主路径。
- `btn_*` 状态为**纵向**排列；帧数不足则 Hover/Pressed 回退 Normal。
- 关闭为独立 `SkinButton`，不点 form 图死像素。
- 完成前跑 `scripts/check_all.ps1`（设置 `QHLC_ACTIVE_CHG`）。
- 不把 UI 布局逻辑塞进 `WorldMapController`。

## File Structure（实现后）

| 路径 | 职责 |
|------|------|
| `governance/trace/requirements/REQ-0005.yaml` | 皮肤 UI 管线 / SkinWindow / 登录可视化 |
| `governance/trace/changes/CHG-0007.yaml` | 本期变更单 |
| `governance/registry/terms.zh-CN.yaml` | 增补 skin window / skin button 等术语（若需） |
| `scenes/ui/SkinUiMath.cs` | 纯逻辑：纵向切片矩形、拖拽钳制（无 Godot 节点依赖，便于测） |
| `scenes/ui/SkinWindow.cs` | 可拖动/关闭/置顶窗体基类 |
| `scenes/ui/SkinWindow.tscn` | 窗体模板场景 |
| `scenes/ui/SkinButton.cs` | 纵向状态按钮 |
| `scenes/ui/Login.tscn` | 改为基于 SkinWindow 的登录壳 |
| `scenes/ui/LoginController.cs` | 精简为接线 + 可选全屏禁用拖拽 |
| `Qhlc.Game.csproj` | Compile 新脚本 |
| `tests/Qhlc.Core.Tests/SkinUiMathTests.cs` | 切片与钳制单测（TEST-0007） |
| `docs/superpowers/specs/2026-11-04-godot-skin-ui-pipeline-design.md` | 已有规格（只读） |

---

### Task 1: 登记 REQ/CHG/术语

**Files:**
- Create: `governance/trace/requirements/REQ-0005.yaml`
- Create: `governance/trace/changes/CHG-0007.yaml`
- Modify: `governance/registry/terms.zh-CN.yaml`

**Interfaces:**
- Produces: 用户确认的 CHG scope：`ui`, `tests`, `governance`（场景脚本在 Game/`scenes/ui`；纯逻辑测在 Core.Tests）
- Produces: TraceId `TEST-0007`

- [ ] **Step 1: 写 REQ-0005**

```yaml
id: REQ-0005
title: Godot 皮肤 UI 管线与可拖动窗体
module: MOD-UI
status: active
priority: high
description_zh: >
  建立 form/btn 皮肤约定与 SkinWindow（拖动/关闭），登录壳在编辑器可视化，
  进入游戏仍接现有走图流程。
acceptance:
  - SkinWindow 支持标题栏拖动、关闭、置顶
  - btn 纵向状态可应用到 SkinButton
  - Login 场景编辑器可见底图与进入按钮
  - 纯逻辑切片/钳制有 TEST-0007
tests:
  - TEST-0007
```

- [ ] **Step 2: 写 CHG-0007**

```yaml
id: CHG-0007
title: 实现皮肤 UI 管线与登录 SkinWindow 示范
requirements:
  - REQ-0005
scope:
  - ui
  - tests
  - governance
impact:
  modules:
    - MOD-UI
    - MOD-CORE
  apis: []
  data_migration: false
  manual_review:
    - description: Godot 手测登录可视化、按钮态、拖动/关闭开关、进入走图
      confirmed: false
acceptance:
  - 符合 REQ-0005
verification:
  tests:
    - TEST-0007
  checks:
    - architecture
    - contracts
    - doc-language
documentation:
  - docs/superpowers/specs/2026-11-04-godot-skin-ui-pipeline-design.md
  - docs/superpowers/plans/2026-11-04-godot-skin-ui-pipeline.md
recovery:
  rollback: code_only
  notes: ""
approvals:
  implementer: self
  reviewer: optional
  merged_by: self
```

- [ ] **Step 3: 增补术语**

在 `terms.zh-CN.yaml` 追加：

```yaml
  - concept: skin_window
    name_zh: 皮肤窗体
    identifier_en: SkinWindow
    forbidden_aliases: []

  - concept: skin_button
    name_zh: 皮肤按钮
    identifier_en: SkinButton
    forbidden_aliases: []
```

- [ ] **Step 4: 请用户确认 CHG-0007 scope（含 `governance`）**

对话中列出 scope 并等待确认后再改测试与场景脚本。

- [ ] **Step 5: Commit**

```bash
git add governance/trace/requirements/REQ-0005.yaml governance/trace/changes/CHG-0007.yaml governance/registry/terms.zh-CN.yaml
git commit -m "chore(gov): CHG-0007 skin UI pipeline scope"
```

---

### Task 2: SkinUiMath（纵向切片 + 拖拽钳制）TDD

**Files:**
- Create: `scenes/ui/SkinUiMath.cs`
- Create: `tests/Qhlc.Core.Tests/SkinUiMathTests.cs`
- Modify: `Qhlc.Game.csproj`（Compile `SkinUiMath.cs`）
- Modify: `tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj`（若需引用 Game 则**避免**——把 `SkinUiMath` 放到可被 Core.Tests 引用的位置）

**Interfaces:**
- Produces:
  - `static class SkinUiMath`
  - `static Rect2I VerticalStripFrame(int textureWidth, int textureHeight, int frameCount, int frameIndex)`
  - `static Vector2 ClampPosition(Vector2 desired, Vector2 windowSize, Vector2 parentSize)`

**放置说明：** 为避免 Game→测试循环引用，将 `SkinUiMath.cs` 放在 `src/Qhlc.Core/Public/SkinUiMath.cs`（纯数学、无 Godot API；用 `int` 返回值元组代替 `Rect2I`/`Vector2`）。若坚持放 `scenes/ui`，则测试放在能引用 Game 的工程——**本期采用 Core 纯数学**，Godot 脚本再包装。

更正后的文件：

- Create: `src/Qhlc.Core/Public/SkinUiMath.cs`
- Create: `tests/Qhlc.Core.Tests/SkinUiMathTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using Qhlc.Core.Public;

public class SkinUiMathTests
{
    [Fact]
    [Trait("TraceId", "TEST-0007")]
    public void Vertical_strip_splits_equal_rows()
    {
        var (x, y, w, h) = SkinUiMath.VerticalStripFrame(
            textureWidth: 40, textureHeight: 90, frameCount: 3, frameIndex: 1);
        Assert.Equal(0, x);
        Assert.Equal(30, y);
        Assert.Equal(40, w);
        Assert.Equal(30, h);
    }

    [Fact]
    [Trait("TraceId", "TEST-0007")]
    public void Clamp_keeps_window_inside_parent()
    {
        var (x, y) = SkinUiMath.ClampPosition(
            desiredX: -50, desiredY: 900,
            windowW: 100, windowH: 80,
            parentW: 800, parentH: 600);
        Assert.Equal(0, x);
        Assert.Equal(520, y);
    }
}
```

- [ ] **Step 2: Run 确认失败**

Run: `dotnet test tests/Qhlc.Core.Tests/Qhlc.Core.Tests.csproj --filter FullyQualifiedName~SkinUiMathTests`  
Expected: FAIL（类型不存在）

- [ ] **Step 3: 实现**

```csharp
namespace Qhlc.Core.Public;

/// <summary>Skin UI pure helpers (vertical btn strips + window clamp).</summary>
public static class SkinUiMath
{
    public static (int X, int Y, int W, int H) VerticalStripFrame(
        int textureWidth, int textureHeight, int frameCount, int frameIndex)
    {
        if (textureWidth <= 0 || textureHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(textureWidth));
        if (frameCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
        var h = textureHeight / frameCount;
        return (0, h * frameIndex, textureWidth, h);
    }

    public static (float X, float Y) ClampPosition(
        float desiredX, float desiredY,
        float windowW, float windowH,
        float parentW, float parentH)
    {
        var maxX = Math.Max(0, parentW - windowW);
        var maxY = Math.Max(0, parentH - windowH);
        return (Math.Clamp(desiredX, 0, maxX), Math.Clamp(desiredY, 0, maxY));
    }
}
```

- [ ] **Step 4: Run 确认通过**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Qhlc.Core/Public/SkinUiMath.cs tests/Qhlc.Core.Tests/SkinUiMathTests.cs
git commit -m "feat(core): SkinUiMath vertical strip and clamp helpers"
```

---

### Task 3: SkinWindow + SkinButton 场景与脚本

**Files:**
- Create: `scenes/ui/SkinWindow.cs`
- Create: `scenes/ui/SkinWindow.tscn`
- Create: `scenes/ui/SkinButton.cs`
- Modify: `Qhlc.Game.csproj`

**Interfaces:**
- Consumes: `SkinUiMath`
- Produces:
  - `partial class SkinWindow : Control` — 导出 `TitleBarHeight`、`EnableDrag`、`EnableClose`；信号 `Closed`
  - `partial class SkinButton : Button` — 导出源纹理与 `FrameCount`；`_Ready` 用纵向切片设 StyleBox

- [ ] **Step 1: 实现 SkinWindow.cs（要点）**

```csharp
namespace Qhlc.Game;

public partial class SkinWindow : Control
{
    [Export] public float TitleBarHeight { get; set; } = 28f;
    [Export] public bool EnableDrag { get; set; } = true;
    [Export] public bool EnableClose { get; set; } = true;

    [Signal] public delegate void ClosedEventHandler();

    private bool _dragging;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        var title = GetNode<Control>("TitleBarHit");
        title.GuiInput += OnTitleGuiInput;
        title.CustomMinimumSize = new Vector2(0, TitleBarHeight);
        // size flags: expand horizontal; fixed height via offset or min size

        var close = GetNodeOrNull<Button>("CloseButton");
        if (close is not null)
        {
            close.Visible = EnableClose;
            close.Pressed += () =>
            {
                Hide();
                EmitSignal(SignalName.Closed);
            };
        }
    }

    private void OnTitleGuiInput(InputEvent e)
    {
        if (!EnableDrag) return;
        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _dragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                MoveToFront();
            }
            else _dragging = false;
        }
        else if (e is InputEventMouseMotion && _dragging)
        {
            var parent = GetParent() as Control;
            var parentSize = parent?.Size ?? GetViewport().GetVisibleRect().Size;
            var desired = GetGlobalMousePosition() - _dragOffset;
            var local = parent is null ? desired : parent.GetGlobalTransformWithCanvas().AffineInverse() * desired;
            // Prefer setting Position in parent space:
            var (cx, cy) = Qhlc.Core.Public.SkinUiMath.ClampPosition(
                local.X, local.Y, Size.X, Size.Y, parentSize.X, parentSize.Y);
            Position = new Vector2(cx, cy);
        }
    }
}
```

（实现时按 Godot 坐标系微调：以父 `Control` 本地坐标为准；TitleBarHit 锚在顶部全宽。）

- [ ] **Step 2: 写 SkinWindow.tscn 节点树**

```
SkinWindow (Control) [script SkinWindow.cs]
  Background (TextureRect) — 全铺
  TitleBarHit (Control) — 顶栏
  CloseButton (Button) — 右上
  Content (Control) — 标题栏以下区域
```

- [ ] **Step 3: 实现 SkinButton.cs**

```csharp
namespace Qhlc.Game;

public partial class SkinButton : Button
{
    [Export] public Texture2D? StripTexture { get; set; }
    [Export] public int FrameCount { get; set; } = 3;

    public override void _Ready()
    {
        if (StripTexture is null || FrameCount <= 0) return;
        var tw = StripTexture.GetWidth();
        var th = StripTexture.GetHeight();
        ApplyStyle(StyleBox.StyleboxNormal, 0, tw, th);
        ApplyStyle(StyleBox.StyleboxHover, Math.Min(1, FrameCount - 1), tw, th);
        ApplyStyle(StyleBox.StyleboxPressed, Math.Min(2, FrameCount - 1), tw, th);
    }

    private void ApplyStyle(StringName styleName, int frame, int tw, int th)
    {
        var (x, y, w, h) = Qhlc.Core.Public.SkinUiMath.VerticalStripFrame(tw, th, FrameCount, frame);
        var atlas = new AtlasTexture { Atlas = StripTexture, Region = new Rect2(x, y, w, h) };
        var box = new StyleBoxTexture { Texture = atlas };
        AddThemeStyleboxOverride(styleName, box);
        CustomMinimumSize = new Vector2(w, h);
    }
}
```

（`StyleboxNormal` 等常量名以实现时 Godot 4 C# API 为准：`"normal"` / `"hover"` / `"pressed"`。）

- [ ] **Step 4: 更新 csproj + 编辑器打开场景无报错**

```xml
<Compile Include="scenes\ui\SkinWindow.cs" />
<Compile Include="scenes\ui\SkinButton.cs" />
```

Run: `dotnet build Qhlc.Game.csproj -c Debug`  
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add scenes/ui/SkinWindow.cs scenes/ui/SkinWindow.tscn scenes/ui/SkinButton.cs Qhlc.Game.csproj
git commit -m "feat(ui): SkinWindow drag/close and SkinButton strip styles"
```

---

### Task 4: 登录壳接入 SkinWindow

**Files:**
- Modify: `scenes/ui/Login.tscn`
- Modify: `scenes/ui/LoginController.cs`
- Modify: `GameMain.cs`（仅当实例类型/信号需调整）

**Interfaces:**
- Consumes: `SkinWindow`, `SkinButton`, 现有 `EnterGame` 信号
- Produces: 编辑器可见的登录布局；全屏登录默认 `EnableDrag=false`、`EnableClose=false`

- [ ] **Step 1: 重构 Login.tscn**

- 根可仍为 `LoginController : Control`，子节点挂载 `SkinWindow` 实例，**或**让 `LoginController` 继承/`[Tool]` 组合 SkinWindow。
- **推荐组合：** 根 `LoginController` 全屏；子 `SkinWindow` 居中（或全铺底图）；`Content/EnterButton` 为 `SkinButton`。
- 底图：在编辑器为 `Background` 指定已导入的登录/form 纹理（路径示例 `res://assets/menu/skins/default/...` 或现用 `assets/pic` 已导入资源）；去掉「仅运行时盲找 BMP」作为主路径。
- `LoginController._Ready`：`EnableDrag/EnableClose = false`（登录壳）；`EnterButton.Pressed` → `EmitSignal(EnterGame)`。

- [ ] **Step 2: 精简 LoginController**

删除或降级运行时 `NtsBmp.Decode` 主路径为可选回退（纹理未指定时）。

- [ ] **Step 3: 编译**

Run: `dotnet build Qhlc.Game.csproj -c Debug`  
Expected: 0 errors

- [ ] **Step 4: Godot 手测清单**

1. 打开 `Login.tscn`：看得见底图与按钮  
2. 运行：点「进入游戏」进入走图  
3. （可选）临时打开拖拽验证 SkinWindow  

- [ ] **Step 5: Commit**

```bash
git add scenes/ui/Login.tscn scenes/ui/LoginController.cs
git commit -m "feat(ui): Login uses SkinWindow template in editor"
```

---

### Task 5: 新手复制说明 + CHG 验收 + check_all

**Files:**
- Modify: `docs/superpowers/specs/2026-11-04-godot-skin-ui-pipeline-design.md`（可选：在文末加「实现附注」链到场景路径，或单独 `docs/superpowers/specs/` 不改规格、写短 `docs/ui/skin-window-howto.zh-CN.md`）
- Create: `docs/ui/skin-window-howto.zh-CN.md`
- Modify: `governance/trace/changes/CHG-0007.yaml`（`manual_review.confirmed`）

- [ ] **Step 1: 写 how-to（简体中文）**

内容包含规格 §5 五步：复制 SkinWindow → 换底图 → 标定 TitleBarHit → Content 摆控件 → 运行验证。

- [ ] **Step 2: 确认 manual_review**

手测通过后将 CHG-0007 `manual_review.confirmed: true`（描述写明自动化 + 建议手测）。

- [ ] **Step 3: check_all**

```powershell
$env:QHLC_ACTIVE_CHG='CHG-0007'
powershell -NoProfile -File scripts/check_all.ps1
```

Expected: 全部通过

- [ ] **Step 4: Commit**

```bash
git add docs/ui/skin-window-howto.zh-CN.md governance/trace/changes/CHG-0007.yaml
git commit -m "docs(ui): SkinWindow howto and CHG-0007 verification"
```

---

## Spec coverage（自审）

| Spec 项 | Task |
|---------|------|
| 资源管线 / 纵向 btn | 2, 3 |
| SkinWindow 拖动/关闭/置顶 | 3 |
| 登录示范可视化 | 4 |
| 新手复制工作流 | 5 |
| 错误回退（缺纹理/帧不足/钳制） | 2, 3, 4 |
| 治理 CHG | 1, 5 |
| TEST-0007 | 2 |

## Placeholder scan

无 TBD/TODO；坐标微调处已标明「以实现时 Godot API/坐标系为准」。

## Type consistency

- `SkinUiMath.VerticalStripFrame` / `ClampPosition` 贯穿 Task 2–3
- `SkinWindow.EnableDrag` / `EnableClose` / `Closed` 用于 Task 4 登录开关
- TraceId `TEST-0007`；CHG `CHG-0007`；REQ `REQ-0005`
