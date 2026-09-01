# addToolBox

> A lightweight, modular Windows toolbox built with C#/.NET and WPF.

addToolBox 是一个面向 Windows 的轻量、本地、模块化工具箱，目标是在保持宿主稳定和低资源占用的前提下，通过可扩展工具不断增加工作、学习、娱乐等能力。

## Preview

> UI is currently under active development. Screenshots will be added as the interface stabilizes.

## Features

以下能力已经在当前 Shell / Workspace 中实现并完成 V0.1 验收：

- Windows 11 风格的 WPF 桌面界面
- 自定义窗口与 DWM 圆角
- 自由工具画布
- 工具长按拾起
- Pointer Reactive Highlight
- 拾起粒子
- 实时自由拖拽
- 工具实体碰撞与高速移动保护
- 沿边滑动
- Soft Collision / rua 软体反馈
- 碰撞粒子
- 18px Soft Boundary
- Adaptive Resize
- 动态最小窗口尺寸

## Planned

以下能力仍处于计划阶段，目前尚未实现：

- 工具位置保存
- 布局锁定
- 可选吸附
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
- `AddToolBox.Core`：核心规则和模型的预留项目，目前仍是最小骨架。
- `AddToolBox.SDK`：未来公共 Module 契约的预留项目，目前仍是最小骨架。
- `AddToolBox.Infrastructure`：未来配置、日志和模组加载等基础设施的预留项目，目前仍是最小骨架。
- `AddToolBox.UI`：未来公共 WPF 控件、Design Tokens 和主题资源的预留项目，目前仍是最小骨架。

## Project Status

项目目前处于早期开发阶段。

### 当前里程碑

**Soft Workspace V0.1**

已经建立的 Git 里程碑：

- Governance V1
- .NET / WPF project skeleton
- Shell visual baseline
- Soft Workspace V0.1

### 当前开发中

宿主 Shell 与自由 Workspace 的交互体验仍在持续迭代。实际工具、模组运行时和完整主题系统尚未进入已实现状态。

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
