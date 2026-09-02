# Changelog

记录对用户和开发者有意义的变化，不逐条复制 Git Log。详细原因、研究数据和证据边界见 [PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

当前没有 Git Tag；这里的 V0.1 是开发里程碑名称，不代表正式 Release。以下状态截至 **2026-09-03**：已提交基线为 `eb8dadd3`，World Canvas 属于既有未提交工作树；静态核实不等同于运行验收。正式 Release 后再按真实版本归档。

## Unreleased

### Added

- 建立简洁 CHANGELOG、详细 PROJECT_HISTORY，以及 [AGENTS Change Documentation](AGENTS.md#23-change-documentation) 同任务更新制度（本轮文档/治理修改，未提交）。
- World Canvas 未提交实现：Tool 世界坐标、WorldLayer 相机投影、中键 Pan、以鼠标位置为锚点的滚轮 Zoom、Reset View、Dynamic WorldExtent 扩张及按交互结束触发的 Lazy Shrink。代码已接入，交互验收尚未在本轮执行。

### Changed

- 未提交工作树将 Window Resize 改为更新 Viewport / 相机投影；不再通过原 Resize 调用链重新写入所有 Tool 位置。Tool 离开 Viewport 是合法状态。
- Workspace 方向转为可扩展的 World Canvas。Widget、Note、Information Card 等非 Tool 内容仍为 Planned / Current direction，尚无公共 WorkspaceItem 抽象。

### Removed

- 未提交工作树已移除旧 `MainWindow` 的 `ScheduleAdaptiveResize` / `ApplyAdaptiveResize` 调用链和依赖 Tool 排布的动态最小窗口尺寸更新；相关研究求解器源码仍保留，未完成提交与运行验收。

### Deprecated / Retired

- **Automatic Compaction — Decision: retiring / removal in progress。** 产品决策为 `Retired from core Resize behavior`：自动收拢与长期 World Canvas 语义冲突。当前入口移除状态如上；不宣称所有研究源码已经删除。未来可评估用户主动触发的 Arrange / Auto layout。

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
