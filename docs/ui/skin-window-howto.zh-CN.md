# 复制 SkinWindow 窗体（新手五步）

本文说明如何在 Godot 编辑器中基于 `SkinWindow` 模板复制出新的皮肤窗体，无需向 AI 描述像素坐标。规格详见 [Godot 皮肤 UI 管线设计](../superpowers/specs/2026-11-04-godot-skin-ui-pipeline-design.md) §5。

**模板路径：** `res://scenes/ui/SkinWindow.tscn`  
**登录示范：** `res://scenes/ui/Login.tscn`（全屏壳内嵌 SkinWindow，可参考布局）

## 五步工作流

### 1. 复制模板

在 Godot **文件系统** 中右键 `scenes/ui/SkinWindow.tscn` → **复制**，粘贴并重命名（例如 `MyPanel.tscn`）。  
也可打开 `Login.tscn`，选中其中的 SkinWindow 实例 → **另存为** 独立场景。

将新场景挂到父节点（或作为子场景实例化）。若需专用逻辑，为新场景添加脚本并继承或组合 `SkinWindow` 行为。

### 2. 换底图并标定标题栏

1. 选中 **Background**（`TextureRect`），在检查器中为 **Texture** 指定窗体底图（已导入的 `form_*` 等皮肤图）。
2. 选中根 **SkinWindow** 节点，设置导出项 **Title Bar Height**（默认 28），与美术标题栏高度一致。
3. 选中 **TitleBarHit**，确认其高度与 **Title Bar Height** 对齐（模板默认顶栏全宽、高度 28）。
4. 需要关闭按钮时：保留 **CloseButton** 并摆放至右上角；根节点 **Enable Close** 保持 `true`。不需要关闭时设 **Enable Close** 为 `false`（登录壳即如此）。

### 3. 在 Content 内摆控件

在 **Content** 节点下添加业务控件（按钮、输入框等）。  
按钮建议使用 **SkinButton**（`scenes/ui/SkinButton.cs`）：为其 **Strip Texture** 指定纵向三态 `btn_*` 图，**Frame Count** 设为纵向帧数（通常 3）。

调整 **Content** 的 `offset_top`，使其从标题栏下方开始，避免遮挡标题栏点击区。

### 4. 运行验证

运行场景或主游戏，确认：

- 在标题栏（**TitleBarHit** 区域）按住左键可拖动窗体（**Enable Drag** 为 `true` 时）；窗体不会拖出父控件范围。
- 点击 **CloseButton** 可关闭并触发 **Closed** 信号（**Enable Close** 为 `true` 时）。
- 业务按钮（含 SkinButton 三态）可正常点击。

登录示范：运行后点击「进入游戏」应进入现有走图流程；登录壳默认关闭拖动与关闭按钮。

### 5. 重复用于新界面

每个新窗体重复步骤 1–4。AI 或开发者只需补充该窗专用脚本与信号接线，布局与皮肤资源由人在编辑器完成。

## 相关资源

| 节点 / 脚本 | 说明 |
|-------------|------|
| `SkinWindow.cs` | 拖动、关闭、置顶；导出 `TitleBarHeight`、`EnableDrag`、`EnableClose` |
| `SkinButton.cs` | 纵向切片按钮三态 |
| `SkinUiMath`（Core） | 切片矩形与拖拽钳制（`TEST-0007` 覆盖） |

## 常见问题

- **缺纹理**：检查导入路径；仍无图时会有占位/日志，勿静默空场景。
- **纵向帧数不足**：Hover/Pressed 回退为 Normal 帧。
- **拖出屏幕**：位置由 `SkinUiMath.ClampPosition` 钳制在父矩形内。
