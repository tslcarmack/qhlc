# Godot 皮肤 UI 管线与可拖动窗体设计规格

| 字段 | 值 |
|------|-----|
| 状态 | 对话批准（2026-11-04） |
| 范围 | 通用皮肤 UI 做法 + 登录壳示范；不批量还原全部 `menu` 窗体 |
| 前序 | `docs/superpowers/specs/2026-11-03-nts-vertical-slice-design.md`；人物 OOP 已合入 `main` |
| 技术底座 | Godot 4.7 + C#；资源根 `assets/menu/skins/default` |
| 协作 | 人在编辑器摆布局并验收；AI 写基类、切片约定与接线 |

## 1. 背景与目标

### 1.1 问题

`assets/menu` 下大量 `form_*`（窗体底图，上方含标题栏）与 `btn_*`（纵向多状态按钮图）。用自然语言描述布局让 AI「一次做对」既难写也难验。当前登录壳仍是运行时解码 BMP + 普通 `Button`，编辑器里看不到最终皮肤。

### 1.2 目标

- 建立**可复制**的皮肤 UI 管线：工具/约定生成底图引用与按钮状态样式；人在 Godot **可视化**摆场景；AI 写薄脚本。
- 提供可复用 **`SkinWindow`**：支持标题栏拖动、关闭、叠放置顶。
- 用 **登录壳 +「进入游戏」** 跑通第一条路径，替换临时 Login 观感（逻辑仍进现有走图流程）。

### 1.3 非目标

- 一次性还原全部菜单窗体 / 账号协议
- 窗体缩放、最小化、复杂模态栈、多皮肤运行时热切换
- 用 AI 视觉模型代替人手测验收

### 1.4 已定决策

| 决策 | 选择 |
|------|------|
| 范围形态 | 通用做法 + 1～2 窗示范（登录） |
| 人机分工 | 混合：自动/约定出图集与样式；**布局由人在编辑器完成** |
| 示范画面 | 登录壳 +「进入游戏」 |
| 按钮状态排版 | **纵向**一列（常态 / 移入 / 按下；帧数以样例实测，缺则降级） |
| 关闭控件 | 独立 `SkinButton` 叠在标题栏右侧（非点 form 图死像素） |
| 实现路径 | 导入约定 + 编辑器布局 + 薄脚本（不做纯运行时拼 UI、不做全自动吐完整 `.tscn`） |

## 2. 资源管线

```text
assets/menu/skins/default
  form_*.bmp / Form_*.bmp  →  TextureRect 底图（含标题栏像素）
  btn_*.bmp（纵向状态条） →  切片 → AtlasTexture / StyleBoxTexture
                                      → SkinButton / Theme
人：在 .tscn 中摆 SkinWindow + Content
AI：SkinWindow / SkinButton 脚本 + LoginController 接线
```

| 步骤 | 负责方 | 产物 |
|------|--------|------|
| 选定源 BMP | 约定 + 工具或手选 | 优先使用已有 `.bmp.import` 的 Godot 导入纹理 |
| 纵向切按钮状态 | 工具或一次性手切 | Normal / Hover / Pressed（可选 Disabled） |
| 窗体底图 | 场景引用 | `form_*` 或现用登录大图 |
| 布局 | **人** | `scenes/ui/*.tscn` |
| 行为 | AI | 拖拽、关闭、`EnterGame` 等信号 |

**验收（人手）**：打开 Login 场景可见正确底图与按钮三态；运行后可拖（若启用）、可关（若启用）、可进入走图。

## 3. `SkinWindow` 基类

所有皮肤窗体的壳（`Control` 场景 + 脚本）。

| 节点 / 成员 | 职责 |
|-------------|------|
| 根 `SkinWindow` | 窗体根；按下标题栏时 `MoveToFront()` |
| 底图 `TextureRect` | 整张 `form_*` |
| `TitleBarHit` | 顶栏命中区；按住拖动整窗 `Position`；高度为导出属性（实现时按样例标定，默认约 24–32px） |
| `CloseButton` | 可选；`Pressed` → `Hide()` 并/或发出 `Closed` |
| `Content` | 内容容器；编辑器内放置业务控件，不挡住标题栏拖拽 |

**行为要点**

- 拖拽仅响应 `TitleBarHit` 的 GUI 输入；位移后钳制在父级可视矩形内。
- 关闭与「进入游戏」分离：后者放在 `Content`，不走关闭逻辑。
- **登录壳**：可复用同一基类；若为全屏底图，可用导出开关关闭拖拽并隐藏关闭按钮，浮动窗（如后续 `form_NewRole_*`）打开拖拽与关闭。

**非目标**：缩放、最小化、模态遮罩层（可后续加子类）。

## 4. `SkinButton` 与登录示范

- `SkinButton`：基于 Godot `Button`（或 `TextureButton`），样式来自纵向切片后的状态图。
- `scenes/ui/Login.tscn`：`SkinWindow` + Content 内「进入游戏」；`LoginController` 保持 / 精简为接线：`Pressed` → 现有 `EnterGame` → `GameMain` 进走图。
- 去掉「仅运行时随机找面板 BMP、编辑器不可见」作为主路径；缺资源时仍允许占位回退。

## 5. 新手工作流（复制模板）

1. 打开 Godot，复制 `SkinWindow.tscn`（或从 Login 另存）。
2. 指定底图纹理；标定 `TitleBarHit` 高度；放置/显示 `CloseButton`。
3. 在 `Content` 下摆业务按钮与控件。
4. 运行：验证拖动、关闭、业务按钮。
5. 新界面重复 1–4；AI 只补该窗专用脚本与信号。

## 6. 模块与治理

- 场景与 UI 脚本：`scenes/ui/`（与现垂直切片一致，挂在 Game 工程）。
- 切片约定 / 小工具：`tools/` 或规格附带的编辑器步骤说明；**不**把 UI 布局逻辑塞进 `WorldMapController`。
- 若改动 `src/` 模块程序集或正式 `MOD-UI` 边界，实现前登记 CHG；本期以场景 + Game 侧脚本为主时在实现计划中写明 scope。

## 7. 错误处理

| 情况 | 行为 |
|------|------|
| 缺底图 / 按钮纹理 | 可见占位 + 日志；不静默空场景 |
| 纵向帧数不足 | 仅用 Normal；Hover/Pressed 回退 Normal |
| 拖出屏幕 | 钳制在父矩形内 |

## 8. 测试与验收

- **自动化**：拖拽/钳制/关闭相关的纯逻辑尽量单测；完整 GUI 不依赖 headless「看图」。
- **手测清单**：编辑器可见底图与三态；标题栏拖动；关闭；进入游戏进走图。

## 9. 成功标准

- 存在可复用的 `SkinWindow`（拖动 + 关闭 + 置顶）与按钮状态约定。
- 登录示范在编辑器中可视化，运行可进游戏。
- 文档级步骤足以让新手复制出第二个窗体，而无需向 AI 描述像素坐标。
