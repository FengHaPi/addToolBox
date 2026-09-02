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
- Module System V0.1：人工导入文件夹 Package、启动发现已安装模组、独立 managed/native 依赖加载、动态 64×64 Tile 与缓存 View
- 第一个正式模组“去背景”：本地 BiRefNet Lite CPU 推理，PNG/JPEG/BMP 输入、双预览和透明 PNG 保存

未安装模组时 Workspace 为空。五个 calculator / image / file / text / color 原型测试 Tile 已移除；真实工具通过独立 Module Package 安装，不编译进 Host。

Automatic Compaction 已从核心 Resize 行为退役；不再自动收拢、蜂窝排列或根据对象包围盒调整最小窗口。Snap / Magnetic Layout 已取消，不列入计划。既有 Project1D / Solve2D 研究代码保留，但不参与当前 Window Resize。

## Planned

以下能力仍处于计划阶段，目前尚未实现：

- 工具位置保存
- 设置页面
- 字体、动效与外观设置
- 模组市场、更新与卸载 UI
- 主题系统
- Portable 与 Installer
- 更多实际工具

## Tech Stack

- C#
- .NET 10
- WPF
- Windows

> The current shell is built using .NET/WPF without third-party runtime packages.

去背景模组独立依赖 Microsoft.ML.OnnxRuntime 1.29.0；大模型与 native 库不进入 Host 依赖。ALC 不是安全沙箱，只导入可信模组。

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
- `AddToolBox.SDK`：WPF-free 的最小 `IAddToolBoxModuleV1.ToolViewType` 契约；身份元数据只来自 `module.json`。
- `AddToolBox.Infrastructure`：未来配置、日志和模组加载等基础设施的预留项目，目前仍是最小骨架。
- `AddToolBox.UI`：未来公共 WPF 控件、Design Tokens 和主题资源的预留项目，目前仍是最小骨架。

## Project Status

项目目前处于早期开发阶段。

### 当前里程碑

**Module System V0.1 + Background Remover V0.1** — 首个独立实际工具的工程与手工验收基线，不是正式用户 Release。

前序 Git 里程碑包括：

- Governance V1
- .NET / WPF project skeleton
- Shell visual baseline
- Soft Workspace V0.1
- Tool Identity / Tool Opening
- Fixed Point 1D / deterministic 2D 研究基础
- Project History / Change Documentation 制度

### 验证状态

Host 和独立 Module build 均为 0 warnings / 0 errors；已验证模型 Smoke Test、SDK/ALC 边界和 Host 集成，并由项目所有者确认 V0.1 人工验收通过。逐图质量评分按所有者要求跳过。性能、资源消耗与限制见 [Module Development Reference V1](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md)；历史见 [CHANGELOG](CHANGELOG.md) 与 [PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

### Module 开发与限制

新模组先阅读 [Module Format V1](docs/MODULE_FORMAT_V1.md) 和 [Development Reference](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md)。V0.1 不含批量、GPU、Module Store、Widget Contract 或 Settings；重型模组的 View/Session 在宿主退出前保留。CPU FP32 推理内存成本较高，不能将模型文件大小当作运行内存需求。

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

去背景工程位于 `modules/AddToolBox.BackgroundRemover/`，不加入 Host solution。按其 [README](modules/AddToolBox.BackgroundRemover/README.md) 明确准备模型，再独立构建：

```powershell
dotnet build -c Release modules/AddToolBox.BackgroundRemover/AddToolBox.BackgroundRemover.csproj
```

导入完整 `modules/AddToolBox.BackgroundRemover/bin/Release/net10.0-windows/win-x64` 文件夹，不是其中的 `Models` 子目录。已安装包位于 `%LOCALAPPDATA%\addToolBox\Modules\<id>`。模型不会被 Git 跟踪，build 不自动下载模型。

## License

License has not been added yet.
