# Changelog

记录对用户和开发者有意义的变化，不逐条复制 Git Log。详细原因、研究数据和证据边界见 [PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

截至 **2026-09-03**，World Canvas Host Baseline 已通过 build、模型与空画布检查、实际窗口启动验证，并由项目所有者确认人工验收通过，作为进入 Module 开发前的正式 Host 里程碑。它不是正式用户 Release，不创建 Tag；正式 Release 后再按真实版本归档。

## Unreleased

### Added

- 建立简洁 CHANGELOG、详细 PROJECT_HISTORY，以及 [AGENTS Change Documentation](AGENTS.md#23-change-documentation) 同任务更新制度，已独立提交：`4b26a0a docs: establish project history and change documentation policy`。
- World Canvas：Tool 世界坐标与 WorldLayer 相机投影；默认空 Workspace 合法。
- Middle Mouse Pan：空白或对象上的中键拖动移动 Viewport。
- Mouse Wheel Zoom：以鼠标位置为锚点，范围 0.25–3.00。
- Reset View：恢复初始 Camera 中心与 1.0 Zoom，不修改 Tool 或 Layout Lock。
- Dynamic WorldExtent：逻辑 Rect 的按需扩张与交互结束后的 Lazy Shrink，保护 Viewport 和全部 Item bounds，不生成空白区域 Visual。

### Changed

- Reset Layout 与 Reset View 完全独立：Reset Layout 只恢复 Tool 默认世界位置，保留 Camera 中心和 Zoom，允许默认 Tool 仍在视口外；无 Item 时同样可安全执行。
- Window Resize 只更新 Viewport 可见范围和相机投影，不改 Camera 中心或 Tool WorldPosition。Tool 离屏是合法状态。
- Workspace 方向转为可扩展的 World Canvas。Widget、Note、Information Card 等非 Tool 内容仍为 Planned / Current direction，尚无公共 WorkspaceItem 抽象。

### Removed

- 删除当前产品的 Automatic Compaction / 自动 Resize 重排调用链，以及依赖 Tool 排布的动态最小窗口尺寸更新；保留已提交的 Project1D / Legacy Solve2D 研究基础。
- 删除五个 prototype built-in test tiles（calculator / image / file / text / color）的视觉实例、初始位置、硬编码映射及原型提示，移除 Core 中无运行用途的 BuiltInTools 测试列表；保留 ToolDefinition 与 Tool Host / Back 基础。

### Deprecated / Retired

- **Automatic Compaction — Retired from core Resize behavior。** 自动收拢与长期 World Canvas 语义冲突，当前产品路径已移除，研究基础继续保留。未来用户主动触发的 Arrange / Auto layout 仍只是可评估方向。

## Historical Milestones

按 Git Commit Date 顺序排列，时区为 `+08:00`。详细历史中保留完整 Hash；无法定位提交的试验不补写日期或 Hash。

### 2026-08-31 — Governance 与 .NET / WPF 骨架

- **Added：** Governance V1，建立根因优先、修改预算、两次失败修改上限、Git 授权、Single Source of Truth 及验证规则（`33ae4c5 chore: establish governance v1`）。
- **Added：** 五项目 solution 与 .NET 10 / WPF 骨架；`global.json` 以 SDK `10.0.400` 为基准。当时无 ProjectReference、无第三方 PackageReference（`24dc96a chore: initialize .NET project skeleton`）。

### 2026-09-01 — Shell、自由 Workspace 与 Tool 链路

- **Added：** Windows 11 风格 Shell、自定义标题栏、WindowChrome、DWM 圆角及 Tool 视觉（`4a72839 feat: establish shell visual baseline`）。
- **Added：** Soft Workspace V0.1：自由 Canvas、长按拾起、自由拖动、实体碰撞与沿边滑动、Soft Collision / rua、粒子、18 DIP Soft Boundary、早期 Adaptive Resize（`42f088d feat: establish soft workspace v0.1`）。
- **Added / Changed：** Layout Lock、Reset Layout、持续边界反馈及窗口视觉细化（`6b8b167 feat: refine workspace layout and boundary feedback`）。
- **Added：** Host Tool Identity，五个 builtin Tool 的 `Id / DisplayName / IconKey` 与视觉一一映射；本阶段才新增 App → Core 引用（`313b546 feat: establish tool identity v0.1`）。
- **Fixed：** 边界反馈同时检查实际接触位置与鼠标施压方向，减少被其他 Tool 挡住时的错误触边反馈；统一右键菜单视觉（`2891a5b fix: refine boundary contact and context menu`）。
- **Added：** Icon → ToolDefinition → 主窗口 Tool Host 的打开/返回链路，内容仍是原型入口（`1e82548 feat: establish tool opening v0.1`）。
- **Retired，归于早期自由 Workspace 阶段、确切日期未定：** Snap / Magnetic Layout 与 Jelly Pass-through 的尝试由所有者确认已取消；保留纯自由拖动和实体阻挡。`Owner-confirmed design decision; exact commit not isolated.`

### 2026-09-02 — Adaptive Resize Research

- **Research / Technical Milestone：** 提交 PAV 风格 Exact 1D、`1/1024 DIP` Fixed Point、Int128 比较与 Preferred Fast Path（`2710ac12 refactor: use fixed point 1d resize projection`）。
- **Research / Technical Milestone：** 提交基于 Conflict Graph 与 X/Y Axis Groups 的 deterministic Solve2D（`eb8dadd3 refactor: add deterministic 2d resize solver`）。这两次提交未把研究求解器接入窗口 Resize。
- 后续 Directed Separation、Centered Honeycomb 的研究与转向 World Canvas 的原因，见 [Adaptive Resize Research](docs/PROJECT_HISTORY.md#adaptive-resize-research)。其未定位提交的研究过程和数字由本轮所有者提供，不能归为上述两次提交的测试成果，也不补写发生日期。
