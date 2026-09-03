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
- 第一个正式模组“去背景”V0.2.1：本地Static BiRefNet Lite，Auto / GPU / CPU、PNG/JPEG/BMP、原图/结果双预览、轻量EdgeRefinement和桌面PNG输出
- 去背景批量处理：多选、文件/文件夹拖放、缩略图导航、当前项与状态、顺序执行、单项失败隔离、停止和桌面批次文件夹
- 去背景可选性能面板：默认关闭，开启后每秒显示进程CPU、Working Set、当前耗时和平均吞吐

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

去背景模组独立依赖Microsoft.ML.OnnxRuntime.DirectML 1.24.4（Managed 1.24.4、Microsoft.AI.DirectML 1.15.4），同一运行时提供CPU和DirectML后端；模型与native库不进入Host依赖。ALC不是安全沙箱，只导入可信模组。

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

**Module System V0.1 + Background Remover V0.2.1 Frozen Baseline** — Owner已接受的首个独立实际工具生产基线；本次不创建Tag或GitHub Release。

当前状态：**FROZEN / ACCEPTED WITH KNOWN LIMITATIONS**（2026-09-03 Owner确认）。保持B Static BiRefNet Lite与默认EdgeRefinement，参数冻结；Auto / GPU / CPU、Batch、缩略图和默认关闭的性能面板保留，Host / SDK契约保持V1。单张点击“保存PNG”写桌面，Batch逐项自动保存，均重名编号且不覆盖。验收与测量边界见[冻结基线](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#21-v021-frozen-production-baseline)。

当前不是Adobe / remove.bg级专业抠图方案。发丝/绒毛灰雾、色边、背景残留，逆光halo/飞发，透明Alpha和极细结构仍有限制；复杂商品图还可能误删真实主体。三个木箱样例已证实模型原始Alpha存在主体缺失，这是模型能力限制，EdgeRefinement无法恢复被判为背景的区域。

**V0.3研究：DEFERRED / FUTURE QUALITY RESEARCH**。既有Matting/HR-Matting证据保留，未进入生产；不继续ONNX导出、实验或V0.3-P1。addToolBox是通用模块化工具箱，首个模组已完成工程链路与人工验收验证，当前阶段关闭，项目重点回到Module生态。未来质量任务需另行授权；本轮不开始第二模组。

前序 Git 里程碑包括：

- Governance V1
- .NET / WPF project skeleton
- Shell visual baseline
- Soft Workspace V0.1
- Tool Identity / Tool Opening
- Fixed Point 1D / deterministic 2D 研究基础
- Project History / Change Documentation 制度

### 验证状态

Host和独立Module分别构建验证；既有GPU100、CPU smoke、1000项调度、错误隔离、缩略图与性能面板证据见Reference，不作为本轮重复测试。Owner确认V0.2.1速度可接受、缩略图符合预期、边缘修正有改善，并接受已知限制；未虚构逐图评分。最终Build、Package及最小smoke记录见[Module Development Reference V1](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#21-v021-frozen-production-baseline)；历史见[CHANGELOG](CHANGELOG.md)与[PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

### Module 开发与限制

新模组先阅读 [Module Format V1](docs/MODULE_FORMAT_V1.md) 和 [Development Reference](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md)。V0.2 的批量/GPU 位于去背景模组内部；Host 仍无正式 Module Update UI、Module Store、Widget Contract 或 Settings。View 缓存由 Host 保持，去背景模组空闲切换设备时释放旧 Session。不能将模型文件大小当作运行内存需求。

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

正式导入包按模组 README 的 `tools/package.ps1` 生成，导入完整 Package 文件夹，不是其中的 `Models` 子目录。已安装包位于 `%LOCALAPPDATA%\addToolBox\Modules\<id>`；V1 导入拒绝重复 ID，不提供升级替换 UI。模型不会被 Git 跟踪，build 不自动下载模型。

## License

License has not been added yet.
