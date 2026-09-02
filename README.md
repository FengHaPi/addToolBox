# addToolBox

> A lightweight, modular Windows toolbox built with C#/.NET and WPF.

addToolBox 是一个面向 Windows 的轻量、本地、模块化工具箱，目标是在保持宿主稳定和低资源占用的前提下，通过可扩展工具不断增加工作、学习、娱乐等能力。

## Preview

> UI is currently under active development. Screenshots will be added as the interface stabilizes.

## Features

当前已实现的 Host 基础：

- Windows 11 风格的 WPF 桌面界面
- 自定义窗口与 DWM 圆角
- World Canvas Workspace：Window 是 Viewport，Resize 只改变可见范围，不重新排列对象
- 中键 Pan：移动 Camera，不修改 Tool WorldPosition
- 鼠标位置锚定的滚轮 Zoom：25%–300%，默认 100%
- Reset View：只恢复初始 Camera 中心和 100% Zoom
- Tool / Item 世界坐标基础与动态 WorldExtent：积极扩张、懒惰回收，空白范围不生成 Visual
- Layout Lock / Reset Layout 基础：锁定只阻止 Tool 拖动；恢复布局只恢复默认世界位置，不移动 Camera 或改变 Zoom
- Tool Host / Back 基础，保留后续工具打开与返回的宿主链路
- 保留 260ms 长按、世界坐标拖拽、Pointer Highlight、实体碰撞 / rua、粒子及 18 DIP 屏幕软边界的交互基础

默认 Workspace 为空。五个 calculator / image / file / text / color 原型测试 Tile 已移除，不以占位 Tile 替代；Module 系统尚未实现，因此当前没有内置业务工具可打开。

Automatic Compaction 已从核心 Resize 行为退役；不再自动收拢、蜂窝排列或根据对象包围盒调整最小窗口。Snap / Magnetic Layout 已取消，不列入计划。既有 Project1D / Solve2D 研究代码保留，但不参与当前 Window Resize。

## Planned

以下能力仍处于计划阶段，目前尚未实现：

- 工具位置保存
- 设置页面
- 字体、动效与外观设置
- 真正的工具 Module 系统
- 模组安装与卸载
- 主题系统
- Portable 与 Installer
- 更多实际工具

## Tech Stack

- C#
- .NET 10
- WPF
- Windows

> The current shell is built using .NET/WPF without third-party runtime packages.

## Design Principles

- Stability first
- Lightweight host
- Modular by design
- Dependencies added only when needed
- UI and business logic should remain separable
- No silent fallback
- Root-cause fixes over patch stacking

## Project Structure

```text
src/
├─ AddToolBox.App
├─ AddToolBox.Core
├─ AddToolBox.SDK
├─ AddToolBox.Infrastructure
└─ AddToolBox.UI
```

- `AddToolBox.App`：WPF 应用入口；当前 Shell 和 Workspace 交互实现位于这里。
- `AddToolBox.Core`：最小 ToolDefinition 身份模型（Id / DisplayName / IconKey），不依赖 WPF；不包含测试工具列表或 Module Contract。
- `AddToolBox.SDK`：未来公共 Module 契约的预留项目，目前仍是最小骨架。
- `AddToolBox.Infrastructure`：未来配置、日志和模组加载等基础设施的预留项目，目前仍是最小骨架。
- `AddToolBox.UI`：未来公共 WPF 控件、Design Tokens 和主题资源的预留项目，目前仍是最小骨架。

## Project Status

项目目前处于早期开发阶段。

### 当前里程碑

**World Canvas Host Baseline** — 进入 Module 开发前的 Host 主体基线，不是正式 Release。

前序 Git 里程碑包括：

- Governance V1
- .NET / WPF project skeleton
- Shell visual baseline
- Soft Workspace V0.1
- Tool Identity / Tool Opening
- Fixed Point 1D / deterministic 2D 研究基础
- Project History / Change Documentation 制度

### 验证状态

已通过 solution build、轻量坐标模型和空画布检查，并已启动实际窗口；项目所有者已确认当前空画布人工交互验收通过。详细记录见 [CHANGELOG](CHANGELOG.md) 与 [PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

### Next major milestone：Module System V0.1

未来 Workspace 内容主要由独立 Module 提供。本轮仅冻结 Host 基础，不实现 Module Loader、Module Store、Widget Contract、Background Remover 或 Settings；这些都不是已完成功能。

## Build

目标环境：Windows，并安装 .NET 10 SDK。

在仓库根目录构建：

```powershell
dotnet build AddToolBox.sln
```

运行桌面应用：

```powershell
dotnet run --project src/AddToolBox.App
```

## License

License has not been added yet.
