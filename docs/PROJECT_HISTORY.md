# addToolBox Project History

## 项目定位

addToolBox 是 Windows-first、C# / .NET 10 / WPF 的本地模块化工具箱与自由 Workspace 项目。最初目标是建立稳定、可维护、低占用的宿主，让具体工具按真实需要扩展，避免把所有业务和重型依赖装入核心。

项目所有者在本轮确认的长期方向是可扩展的 **World Canvas Workspace**：Window 是观察世界的 Viewport，未来除 Tool 外也可能容纳 Widget、Note、Information Card。这里记录产品演化与原因；[ARCHITECTURE.md](../ARCHITECTURE.md) 仍是架构约束的权威文件，本文不替代架构审批或正式架构决策更新。

## 记录范围与证据约定

首次回溯记录日：**2026-09-03**。下列证据约定和早期研究叙述保留首次回溯时的范围；后续变化见 World Canvas Host Baseline。日期是记录日期，不是未提交试验的推定发生日期。

- 读取本地 `git log --all` 可达的全部 **11 个提交**，逐个核对 `git show --stat`、`git show --summary`，并检查关键源文件及具体 Diff。仓库不是 shallow clone；本地分支为 `main`，可见的 `origin/main` 与 HEAD 相同，未发现 Tag。本轮没有 fetch，不将本地 remote-tracking ref 宣称为远端实时状态。
- 最早可核实提交为 `33ae4c5`，Commit Date 为 **2026-08-31 23:28:44 +08:00**。无法为首次提交之前的讨论、试验补造日期。
- 首次回溯时的已提交基线 / HEAD：`eb8dadd3d41b6b95c5b8b3a7985d6ad2f9477fa1`。作者日期和提交者日期逐项核对一致；下文采用 Git Commit Date，统一保留 `+08:00`。
- 读取当前 [AGENTS](../AGENTS.md)、[ARCHITECTURE](../ARCHITECTURE.md)、[README](../README.md)、[基础技术说明](../本地模块化工具箱项目｜基础技术说明.md)，以及当前工作树。任务开始时没有 CHANGELOG 或 docs 目录中的等价历史体系。
- **Git / Code verified**：提交或源码能证明的内容；本轮静态检查不证明历史 UI 验收或数值扫描已经重跑。
- **Owner-confirmed**：项目所有者在本次历史回溯需求中明确提供的设计决定、试验过程和数字。无法定位提交时注明 `exact commit not isolated`；相关原始报告、输入、随机种子及独立研究实现未在当前仓库找到，本轮不把它们写成独立复测结果。
- **Uncommitted / In progress**：首次回溯时已有的 World Canvas 修改状态；后续验证和交付状态以 Host Baseline 章节为准，不倒写早期验收结果。

阶段状态使用 Active、Superseded、Retired、Research；这些描述方向或阶段是否仍适用，不代表通过测试。Planned / Current direction 单独表示尚未实现的能力。

## 历史时间轴

### 2026-09-03 — Module System V0.1 + First Official Module

状态：**已实现，build / 自动检查通过，Owner-confirmed manual acceptance**。起点为已人工验收且推送的 `24948e8 feat: establish world canvas host baseline`，文档制度独立基线为 `4b26a0a`；本轮 Host/SDK 与 Module/Reference 分别提交，不与 V0.2 混合。

Host/SDK Commit：`c0da1f5591dce9eb19c3f3763775247141718f0a feat: establish module system v0.1`，10 个文件。第一个模组与 Reference 使用单独的 `feat: add background remover module v0.1` 提交，不预填其自身 Hash。

本轮按所有者明确批准的高风险范围首次实现 WPF-free `ToolViewType` SDK 契约、Manifest Metadata SSOT、人工导入、独立 ALC、启动发现与动态 Tile；未修改 Core、Workspace Interaction / WorldCanvas 算法或治理文件。架构文档的“尚未实现 Module”描述仍是已提交 Host 基线快照，不把本文当作架构变更授权。

第一实际模组选择“去背景”，因为它同时验证自定义 UI、大模型资源、私有 native/managed 依赖、图片 IO、后台重计算和缓存 View/Session 生命周期。它是独立工程，只引用 SDK，固定 ONNX Runtime 1.29.0 CPU 和外部 BiRefNet Lite ONNX 模型，不把模型放入 Git。

当前证据：Host / 独立 Module build 均 0 warnings / 0 errors；真实固定模型的合成图片 Pipeline Smoke Test 25 项、隐藏 Host 集成 15 项通过。所有者随后明确回复“通过”，确认人工功能清单；逐图质量记录则明确要求“暂时不方便，跳过”，因此未提供尺寸/评分/缺陷明细，不把合成图当作质量优秀的证据。

已建立 [MODULE_DEVELOPMENT_REFERENCE_V1](MODULE_DEVELOPMENT_REFERENCE_V1.md)：19 文件约 241.32 MB 的 Package、三组各 3 次 warm build、实际启动/安装复制/加载、1 cold + 3 warm CPU 推理、Working Set / CPU 近似值及 11 类错误探针。冷 Session 初始化约 3.99 s，温 Process median 约 5.78 s；探针进程工作集峰值约 12.60 GB。数据说明重型模组运行内存显著高于磁盘模型大小，后续应据此评估硬件后端和生命周期，而非提前扩 SDK。

合成 PNG 与完整测试安装复制已清理；17 个错误夹具的递归清理被执行策略拦截，保留在已忽略的 artifacts，未修改用户安装包、不进入提交，详见 Reference。五个 Prototype Tile 已在 Host 基线删除，不重复归入本轮修改。V0.2 Batch/GPU 仍未实施。

### 2026-08-31 — Governance V1

状态：**Active**。

关键 Commit：`33ae4c5 chore: establish governance v1`，23:28:44 +08:00。

项目首先提交 AGENTS、ARCHITECTURE 和基础技术说明，再创建工程。这与稳定性、可维护性优先的项目目标一致：在 AI 辅助开发早期建立可检查的边界，避免通过反复补丁、提前抽象或未经批准的依赖扩大宿主。

Governance V1 覆盖修改预算、Root Cause、同类失败最多两次代码修改、禁止盲目第三次修复、Git 写操作授权、禁止提前抽象、Single Source of Truth，以及 Build / Test / Diff 自检。架构文件约束项目分层、按需引用、错误处理和未来 Module 方向。

结果：后续开发有明确的范围与证据要求。此阶段仅建立方向及治理，并未创建 solution、App 或 Module runtime。本次 Change Documentation 是后续治理增补，不倒写成 V1 原有内容。

### 2026-08-31 — .NET 10 / WPF 项目骨架

状态：**Active**。

关键 Commit：`24dc96a chore: initialize .NET project skeleton`，23:47:11 +08:00。

建立 `AddToolBox.sln`、`.gitignore`、`global.json` 及五个项目：

| 项目 | 当时的真实状态 |
| --- | --- |
| AddToolBox.App | `net10.0-windows`，`UseWPF=true`，WinExe；App 启动 MainWindow |
| AddToolBox.Core | `net10.0` 最小类库骨架 |
| AddToolBox.SDK | `net10.0` 最小类库骨架，无 Module Contract |
| AddToolBox.Infrastructure | `net10.0` 最小类库骨架，无配置/日志/加载系统 |
| AddToolBox.UI | `net10.0-windows`，`UseWPF=true` 的类库骨架 |

`global.json` 配置为 SDK `10.0.400`、`rollForward: latestFeature`、`allowPrerelease: false`。这是仓库指定的 SDK 选择规则，不推断为历次实际使用版本。

该提交没有 ProjectReference，也没有第三方 PackageReference。App → Core 引用直到 Tool Identity 阶段才加入。基础技术说明列举的 CommunityToolkit.Mvvm、DI、Logging 等是候选方向，不能据此声称已安装。

### 2026-09-01 — Shell 视觉基线

状态：**Active；具体视觉随后迭代**。

关键 Commit：`4a72839 feat: establish shell visual baseline`，01:02:08 +08:00。

建立 Windows 11 风格自定义 Window：`WindowStyle=None`、WindowChrome、DWM 圆角、自定义标题区、拖动标题栏、双击最大化/还原及窗口控制按钮。Canvas 中布置五个 Tool 图标，作为后续 Workspace 的视觉基础。

此时画布还是固定尺寸、居中的视觉布置；添加工具、设置及 Tool 内容尚属视觉占位。不能把后来的自由拖动、身份模型或打开链路倒写到此阶段。

### 2026-09-01 — Free / Soft Workspace V0.1

状态：**Active 的交互基础；早期 Resize 模型后来 Superseded**。

关键 Commit：`42f088d feat: establish soft workspace v0.1`，10:10:44 +08:00。

Workspace 转为随窗口铺展的自由 Canvas，加入 260 ms 长按 Pickup、实时 Tool Drag、Pointer Reactive Highlight、拾起粒子、实体碰撞、高速移动保护、沿边滑动、Soft Collision / rua、碰撞粒子和 18 DIP Soft Boundary。

此阶段的 Adaptive Resize 读取当前位置，对边界和 Pairwise overlap 进行推动，再写回 Canvas，同时计算动态最小窗口尺寸。它是已接入窗口的早期行为，与后来提交的 Exact Projection 研究必须区分。

后续 Commit：`0f05e50 docs: add project readme`，10:22:40 +08:00。README 固化了 Soft Workspace V0.1 的能力与当时验收陈述；本轮只将其作为历史文档证据，不等同于重新运行该版本。

**取消的早期方向：** 以下试验与原因来自本轮所有者确认；可达 Git 快照和当前拖动实现支持现有自由拖动/实体阻挡原则，但未隔离出试验及删除的独立提交。

- **Snap / Magnetic Layout — Retired。** 曾尝试，后正式取消。预览和磁吸干扰拖动，手感不符合自由 Canvas 目标；当前原则为纯自由拖动、不自动 Snap。`Owner-confirmed design decision; exact commit not isolated.` WPF 的 `SnapsToDevicePixels` 是像素对齐设置，不是此处的布局吸附功能。
- **Jelly Pass-through — Retired。** 曾尝试，后正式取消。穿透破坏鼠标跟手与实体碰撞的一致性；当前保持实体阻挡，rua 是软体视觉反馈。`Owner-confirmed design decision; exact commit not isolated.`

不能仅凭当前没有相关代码推定取消日期，也不能将 README 中仍写为 Planned 的“可选吸附”当作继续有效的产品承诺。

### 2026-09-01 — 布局控制与边界反馈细化

状态：**Active；Reset 的坐标语义随后随 World Canvas 演化**。

关键 Commit：`6b8b167 feat: refine workspace layout and boundary feedback`，13:06:25 +08:00。

加入 Layout Lock、默认布局捕获、Reset Layout、空白区右键菜单及锁定状态反馈。初版 Reset 先检查默认布局在当前 Workspace 中是否合法，再恢复位置。

边界反馈从短暂闪动演化为按接触状态持续显示、离开后释放，并让高亮跟随接触位置；同时细化边框和粒子。目标是让控制入口和软边界反馈更一致，不引入第二套拖动规则。

### 2026-09-01 — Tool Identity V0.1

状态：**Active**。

关键 Commit：`313b546 feat: establish tool identity v0.1`，13:45:02 +08:00。

在 `AddToolBox.Core/ToolDefinition.cs` 建立 `ToolDefinition(Id, DisplayName, IconKey)` 和 BuiltInTools，并在 App 中核实身份与视觉一一对应、ID 非空且不重复。此时才新增 App → Core ProjectReference。

| Id | DisplayName | IconKey |
| --- | --- | --- |
| `builtin.calculator` | 计算器 | `calculator` |
| `builtin.image` | 图片 | `image` |
| `builtin.file` | 文件 | `file` |
| `builtin.text` | 文本 | `text` |
| `builtin.color` | 取色器 | `color` |

这是当时 Host 建立的 Tool Identity。虽然类型在 Core 中声明为 public，它不是已发布的公共 Module SDK Contract；SDK 项目未因此建立插件协议。身份为随后“点开哪个 Tool”提供依据，五个入口不代表五套业务工具已经完成。Host Baseline 保留 ToolDefinition，移除这份 BuiltInTools 原型数据。

### 2026-09-01 — 真实边界接触与 Context Menu 修正

状态：**Active；World Canvas 工作树进一步适配坐标转换**。

关键 Commit：`2891a5b fix: refine boundary contact and context menu`，14:09:10 +08:00。

原反馈只看鼠标期望位置是否越界；Diff 改为同时检查碰撞求解后的实际位置和向边界施压的方向，使用 `BoundaryContactEpsilon=0.75`。这样被其他 Tool 挡住、尚未触边时，不应仅因鼠标越界就触发边界反馈。另加入 Workspace ContextMenu、MenuItem、Header 的统一样式。

### 2026-09-01 — Tool Opening V0.1

状态：**Host / Back 基础保留；原型入口在 Host Baseline 移除**。

关键 Commit：`1e82548 feat: establish tool opening v0.1`，15:27:54 +08:00。

首次建立 **Icon → ToolDefinition → Main Window Tool Host**。短按候选和长按拖动分别处理；打开时清理交互、切换到 ToolHostView、显示名称，返回时恢复 Workspace。Host 内容明确显示“该工具当前为原型入口”。

本阶段没有加入 OpenMode、WindowMode 或独立窗口协议。已知需求只有主窗口内打开与返回；按真实需求演化的治理原则足以支撑它，尚无证据需要扩大公共协议。以后若出现真实多窗口需求，应另行设计，不能把未实现的模式列为当前能力。

### 2026-09-02 — Fixed Point 1D 研究基础

状态：**Research foundation**。

关键 Commit：`2710ac12 refactor: use fixed point 1d resize projection`，11:37:03 +08:00。

只修改 `WorkspaceInteraction.cs`，加入 Preferred Fast Path、PAV / Isotonic-style 投影、`1/1024 DIP` Fixed Point、Int128 有理比较和明确失败状态。目的在于替代渐近推动中的残余重叠与数值不确定性，详见下方研究章节。

该提交没有修改 MainWindow 的 Resize 调用；不能称为产品已切换到 Exact 1D。

### 2026-09-02 — deterministic Axis Group 2D 研究

状态：**Research；后续由所有者报告的 Directed Separation 研究取代方向，代码仍保留**。

关键 Commit：`eb8dadd3 refactor: add deterministic 2d resize solver`，11:58:52 +08:00。

在同一文件新增二维输入/输出和 MergeTrace，以 Conflict Graph、X/Y Axis Groups、确定性的冲突选择和 1D 投影构建 Solve2D。失败结果区分 GeometricInfeasible、FixedGridInfeasible、NumericInvariantViolation 等。

**接入边界：** HEAD 的 MainWindow 仍调用 `ConstrainForResize`；`Solve2D` 及其内部 `Project1DCore` 没有成为窗口 Resize 入口。提交证明的是研究实现进入仓库，而不是最终产品 Resize 方案。

### 2026-09-03 — 历史制度建立与 World Canvas 收口起点

状态：**文档制度 Active；当时 World Canvas 为 Uncommitted / In progress**。

文档制度已独立提交：`4b26a0a docs: establish project history and change documentation policy`，01:14:51 +08:00。该提交只包含 AGENTS、CHANGELOG、PROJECT_HISTORY，没有生产代码。当时已有三个已跟踪文件的修改及一个未跟踪文件：`MainWindow.xaml`、`MainWindow.xaml.cs`、`WorkspaceInteraction.cs`、`WorldCanvasState.cs`，均位于 `src/AddToolBox.App/`。

源码显示 WorldLayer、世界坐标、Pan / Zoom / Reset View 和 Dynamic WorldExtent 已接入；旧的自动 Resize 重排入口已从 MainWindow 移除，但算法源码保留。详见当前状态章节。本轮没有改动这些生产文件，也不为其补造完成时间或运行验收。

本轮另外建立 CHANGELOG、PROJECT_HISTORY 和 AGENTS 的 Change Documentation 完成条件，让未来重要变更、取消和研究决策在对应任务内留下证据。它是治理文档制度，不是已经安装的自动生成脚本或 CI。

### 2026-09-03 — World Canvas Host Baseline

状态：**Active / 正式 Host Milestone；已实现并通过项目所有者人工验收**。作为进入 Module 开发前的开发主线基线冻结，不是正式用户 Release，不创建 Tag。

主体 Workspace 从 Window Canvas 转为 World Canvas：Tool WorldPosition 是布局真相，Window 是观察世界的 Viewport。Resize、Pan、Zoom 和 WorldExtent 调整不修改 Tool 世界位置；用户拖动提交位置，用户主动 Reset Layout 恢复默认位置。自动收拢从核心 Resize 行为正式退役，不再隐式蜂窝排列或根据 Item BoundingBox 重排。

保留中键 Pan、鼠标锚点 Zoom（0.25–3.00）、Reset View 和大缓存 / Lazy WorldExtent。Reset View 只恢复初始 Camera 中心和 1.0 Zoom；Reset Layout 只恢复 Tool 默认世界位置，二者独立，默认 Tool 离屏也合法。WorldExtent 只管理逻辑 Rect，保护 Viewport 与全部 Item bounds，不创建空白 Chunk Visual，不删除 Item。

五个 calculator / image / file / text / color 测试 Tile 在本阶段真正移除，包括视觉实例、硬编码映射、初始位置、原型提示及 BuiltInTools 数据。保留纯 C# ToolDefinition、通用 Tool 交互和 Tool Host / Back，不以新假 Tool 或大型 Empty State 替代。没有 Module 时默认 World Canvas 为空是合法产品状态。

验证证据：`dotnet build .\AddToolBox.sln` 为 0 warnings / 0 errors；162 项坐标 / WorldExtent 模型检查通过，覆盖 25% / 50% / 100% / 200% / 300% Zoom、空集合与远处 Item 的保护范围；216 项空画布宿主检查通过，覆盖三种窗口尺寸、Camera / Reset / Extent 和零默认 Visual。已启动实际 addToolBox 窗口并核对窗口句柄和响应状态。没有使用自动鼠标模拟；项目所有者随后明确回复“通过，上传”，确认当前 Host 人工验收通过并批准基线上传。模型检查与人工验收作为不同证据分别记录。

README 同步当前能力；ARCHITECTURE 仅修正项目骨架已创建等事实，未重写分层、模块或公共契约原则。既有 Project1D / Legacy Solve2D 研究资产继续保留，以下研究数据与退役原因不改写。

下一主要里程碑：**Module System V0.1**。未来 Workspace 内容主要由独立 Module 提供；本阶段不实现 Module Contract、Loader、Store、Widget Contract、Background Remover 或 Settings。

## Adaptive Resize Research

最初目标：窗口缩小时，Tool 不重叠、不越界并自然收拢；窗口放大时，Tool 恢复原位。研究逐步区分了数值精确性、二维可行性、位置质量与产品语义。

下列顺序依据已核实提交和本轮所有者提供的演化过程。没有提交日期的阶段不推定日期。**扫描数字均为 Owner-confirmed historical results，本轮未重跑；当前仓库未找到对应测试项目、Probe 输入/种子和原始报告。** 因此保留研究结论，但不能保证仅靠当前 checkout 就能完整复现后续试验。

### Pairwise Relaxation：问题来自状态回写与渐近推动

状态：**Superseded 的产品路径；源码仍保留**。

Git 证据来自 `42f088d` 建立的 `ConstrainForResize` 及 HEAD 的 `ApplyAdaptiveResize`：当前位置 → Clamp / Pairwise 碰撞推动 → 写回 Canvas。每轮基于当前已经被挤压的位置工作，未将恢复原位所需的 Preferred 布局作为独立求解输入。

所有者报告的诊断现象包括：压缩后无法回位、单向压力堆积、密集五 Tool chain 中约 **0.003 DIP** 的残余 overlap，以及渐近收敛。该残差不是本轮从源码测得；它作为历史诊断案例推动了 Exact Projection 研究。

### Exact 1D / Dyadic Fixed Point

状态：**Research foundation；Git 实现已保留**。

关键 Commit：`2710ac12 refactor: use fixed point 1d resize projection`。

PAV / Isotonic-style 1D Projection 将顺序与不重叠约束转为投影问题。所有者报告初期 double 的 ULP 问题；提交中的数值方案采用：

- `TicksPerDip=1024`，即 `1/1024 DIP` 的 Dyadic Fixed Point。
- 尺寸向上量化、可用边界向内量化，结合 checked 运算和明确的数值范围失败状态。
- PAV block 的均值用 Int128 交叉相乘比较，避免将有理数先转换为 double；采用 nearest-even 舍入。
- Preferred Fast Path：原布局合法时直接返回原始 Preferred 坐标，避免无必要的量化与位移。

所有者报告的 **5,000** 与 **100,000 property scan**：`NumericInvariantViolation = 0`、`strict overlap = 0`，并保持 deterministic。报告证明范围限于当时扫描输入；没有在此声称所有输入的形式化正确性，也没有把算法存在等同于产品 Resize 接入。

### Axis Group 2D：可重复决策与表达能力边界

状态：**Research / Superseded direction**。

关键 Commit：`eb8dadd3 refactor: add deterministic 2d resize solver`。

代码建立 Conflict Graph，在 X / Y 两轴维护 Groups，对冲突分别模拟合并候选，再按确定性顺序选择。每次接受合并后 group 数减少，是代码可核实的单调推进结构；MergeTrace 保存所选轴和候选结果。所有者同时报告研究中的 monotonicity 检查，不将这一名称扩展为任意输入或视觉连续性的保证。

所有者提供的 Known-Witness 结果：

| 检查 | 历史结果 |
| --- | --- |
| 有已知合法布局的样本 | 5,000 cases |
| Greedy Success | 3,538 |
| Conservative Failure | 1,462，即 29.24% |
| 对上述失败继续做完整 Axis-State Search | 873 可由 alternate merge plan 恢复 |
| Axis Group 模型仍无法表达 | 589 |

诊断区分了两个层次：Greedy 的不可撤销决策使部分可行分支过早丢失；即使完整搜索，Transitive Group Overconstraint 和 Preferred Ordering 限制仍让模型把无需成为全序链的对象捆成链。`3,538 + 1,462 = 5,000`，`873 + 589 = 1,462`；后续研究因此转向更局部的约束表达，而非增加重试掩盖失败。

完整 Axis-State Search 及这些结果是所有者报告；不能把它们当作 `eb8dadd3` 已包含的测试或实现。

### Directed Separation：减少无必要的全链约束

状态：**Research，Owner-confirmed；exact commit not isolated**。

后续尝试 Pairwise Directed Separation Constraints。`A →X B` 只表达 A / B 在 X 轴上的分离关系，不因为处于同一 Component 就把整个 Component 强制变成 Total Chain。这直接针对 Axis Group 过度约束的根因。

所有者提供的历史验证如下；对应 Directed 实现和原始测试材料未在本轮仓库中找到：

| 验证范围 | 历史报告 |
| --- | --- |
| 原先模型无法表达的 589 例 | 589 / 589 恢复 |
| 原 Known-Witness 样本 | 5,000 / 5,000 |
| 额外五工具高压样本 | 10,000 / 10,000 |
| Axis brute-force | 50,000 cases |
| 2D Packing brute-force | 20,000 cases |

同一研究报告还包括 `false negative = 0`、`illegal success = 0`、deterministic、monotonicity、no cumulative drift。准确结论是：**在当时测试范围内，二维合法布局求解能力很强。** 这不是所有输入永远正确的形式化证明；没有报告材料时也不自行补造各检查的 seed、运行命令、耗时或版本 Hash。

### 为什么没有把 Directed Solver 直接产品化

状态：**Research conclusion，Owner-confirmed；exact commit not isolated**。

后续仍发现 Stress continuity 跳变、Relation Reversal 和 First-success 位置质量长尾。求出合法布局不等于布局变化自然，也不等于用户原本的空间关系得到保留。

这使问题从“能否放得下”转向“Window Resize 是否应该自动重新布局用户世界”。Feasibility 结果因此保留为研究成果，没有直接升格为产品 Resize 验收。

### Centered Honeycomb Compaction

状态：**Research / Retired direction，Owner-confirmed；exact commit not isolated**。

曾进一步尝试 Compact Target、Preferred → Compact Morph，以及五 Tool 的 **2–1–2** 居中蜂窝形态。研究同时出现 World Canvas 雏形、Dynamic WorldExtent 和 Pan，试图结合自动收拢与更大的自由空间。

所有者确认的发现包括：

- 自动 Morph 与实体碰撞可能卡位；目标持续推进，解除卡位后出现视觉跳变。
- Viewport Boundary 与 World Boundary 的语义冲突，收拢状态与 Pan / Tool Drag 竞争。
- Item 合法离屏也可能触发收拢；如果未来内容分布于整个 World，全局收拢本身就不合理。

当前可达提交与工作树未找到 Honeycomb / Compact Target / Morph 的独立实现快照，不能将这些试验绑定到 Axis Group 提交，或声称当前仍具有蜂窝整理功能。

### World Canvas 决策与 Automatic Compaction 退役

产品决策状态：**Retired from core Resize behavior**。

决策来源：**Owner-confirmed design decision; exact commit not isolated.** 当前产品路径的移除已在 World Canvas Host Baseline 核实；交付与验收状态见该里程碑章节。

最终认知是：Window 不要求容纳所有内容；Window 只是 Viewport。Item 离开 Viewport 完全合法。长期模型是 **WorldPosition + Viewport + Pan + Zoom + Dynamic WorldExtent**，窗口尺寸变化只改变观察范围，不应擅自移动所有 Item。

Automatic Compaction 的主动退役原因是：经过上述工程研究，自动收拢与 World Canvas 长期产品模型存在语义冲突，其复杂度与 Bug 面积显著高于实际产品收益。这是产品模型的取舍，不把有价值的数值研究概括成“功能失败”。

Host 收口源码确认旧 `ScheduleAdaptiveResize` / `ApplyAdaptiveResize` 及动态最小窗口尺寸更新已从 MainWindow 移除，SizeChanged / StateChanged 转而调度相机投影。`ConstrainForResize`、`Project1D`、Axis Group `Solve2D` 等源码仍在 `WorkspaceInteraction.cs`；当前窗口没有调用这些求解入口。因此不能写成“所有收拢/研究源码已经删除”，Host Milestone 也不等同于正式 Release。

### 研究资产与未来复用

当前 Git 可恢复的资产包括 Fixed Point、1D 投影、Axis Group 2D、合法性检查和 MergeTrace。Directed Separation 与 Honeycomb 的结论在本文保留，但其独立实现、原始报告和完整复现材料当前不可由仓库提供。

未来若真实需要 **Arrange current view、Auto layout、Honeycomb arrangement、Grid arrangement、Organize selected items**，可评估复用 Compact slot generation、Fixed Point、collision legality、Directed Separation 和 deterministic assignment。需要先恢复并核实相应研究资产，再验证实际交互。

这些候选能力应由用户主动触发；目前均为 **Planned / Current direction**，不作为 Window Resize 的隐式行为。

## 当前 World Canvas 状态与未来边界

以下对应 World Canvas Host Baseline，验证范围见上述里程碑。相关源文件：[MainWindow.xaml](../src/AddToolBox.App/MainWindow.xaml)、[MainWindow.xaml.cs](../src/AddToolBox.App/MainWindow.xaml.cs)、[WorkspaceInteraction.cs](../src/AddToolBox.App/WorkspaceInteraction.cs)、[WorldCanvasState.cs](../src/AddToolBox.App/WorldCanvasState.cs)。

| 能力 | 当前证据与状态 |
| --- | --- |
| Tool WorldPosition | 以 Tool ID 保存 Default / Preferred 世界坐标，WorldLayer 中的坐标用于显示和拖动；保留交互基础，默认原型实例为 0，没有提前加入 Module 内容注册机制 |
| Middle Mouse Pan | 空白或 Tool 上均从 Workspace 预览事件进入 Pan；不以 Zoom、窗口尺寸、离屏或 Layout Lock 禁用；不修改 Tool WorldPosition |
| Mouse Wheel Zoom | 鼠标锚点缩放，范围 0.25–3.00，每刻度因子 1.10；模型已检查，默认 1.0 |
| Reset Layout / Reset View | Layout 只恢复 Tool 默认世界位置，Camera 中心和 Zoom 不变，允许 Tool 离屏；View 只恢复初始中心 `(0, 0)` 和缩放 1.0，不改变 Tool / Default / Lock。零 Item 的两个操作已检查无异常 |
| Window Resize | SizeChanged → ScheduleCameraProjection → ApplyCameraProjection；初始化完成后更新相机矩阵及 extent，不经旧 Resize 链重写 Tool 布局 |
| Dynamic WorldExtent | WorldCanvasState 中按需扩张；初始 margin 4096，边缘触发距离 768，扩张步长 4096，均为 world DIP |
| 大缓存迟滞 / Lazy Shrink | retention margin 3072、最小回收量 4096；Pan 结束、拖动结束、Reset Layout 和 Reset View 后触发 Shrink，不是每帧追缩；两个 Reset 的 extent 检查均不移动 Camera 或 Tool |
| 空白区域 Visual 成本 | WorldExtent 是 Rect 数据，不为 extent 的空白面积生成额外 Visual；这是源码结构事实，未做整体性能测试，也不宣称所有离屏 Item 已虚拟化 |
| WorldExtent 回收 | Shrink 将所有传入 Item bounds 与 Viewport bounds 纳入保护范围，只改变 extent，不删除 Item |
| 交互边界 | Tool 仍有实体碰撞；主动拖动使用换算后的 Viewport 安全边距，允许已离屏 Tool 连续拖回，不因离屏自动全局收拢 |
| 默认 Workspace / Tool Host | 默认空 Canvas，无五个测试入口及原型提示；Tool Host / Back 基础保留，下一阶段接入真实 Module 内容 |

未来 World Canvas 可能承载 Note、Text widget、CPU / GPU monitor、Clock、Information Card、Image decoration、Shortcut、Module-provided widget。**当前没有提前建立公共 WorkspaceItem 抽象，也没有这些非 Tool Item 的实现。** 等第一个非 Tool Item 有真实需求和实现范围时，再按治理流程设计。

此表保留 Host Baseline 时点的状态；后续真实工具与 Module 加载/安装已由本文的 Module V0.1 里程碑实现。位置持久化、主题系统、设置及发行安装方案仍未实现，不能从预留项目或候选技术说明推定这些能力存在。

## 既有文档差异与证据缺口

- **README 旧差异已同步：** Host Baseline 修正了 Soft Workspace V0.1 仍被列为当前里程碑、Layout Lock / Snap 的错误计划状态，以及旧 Adaptive Resize / 动态最小窗口描述。
- **ARCHITECTURE 旧事实已同步：** 按所有者本次明确授权，修正 solution / 五项目尚未创建等事实；分层与治理约束保留，没有通过历史文档代替架构审批。
- **研究可复现性缺口：** 未在当前源码/文档和可达 Git 文件树中找到 Directed / Honeycomb 的实现与扫描原始报告，也没有测试项目。本文保留所有者明确提供的数据与结论；不能补造缺失的完整工程资产。

## 关键 Commit 索引

此索引用于核对上文证据，不代替演化叙述。保留首次回溯的 11 个提交，并追加已核实的文档制度提交；日期均为 Commit Date `+08:00`。Host 基线自身 Hash 由实际 Git 提交结果确定，不预填。

| 日期与时间 | 完整 Hash | Commit subject |
| --- | --- | --- |
| 2026-08-31 23:28:44 | `33ae4c563151151fc8e50a10291526545ab5d2f0` | chore: establish governance v1 |
| 2026-08-31 23:47:11 | `24dc96aacdb8499df80197a5f95cba0528a4f5ed` | chore: initialize .NET project skeleton |
| 2026-09-01 01:02:08 | `4a7283985923a4c2abd75b44188f66862ca286bb` | feat: establish shell visual baseline |
| 2026-09-01 10:10:44 | `42f088d16521e99ec280fc7c67c477760e3e6536` | feat: establish soft workspace v0.1 |
| 2026-09-01 10:22:40 | `0f05e50185b7936291348bd64694b0f2ccf82d70` | docs: add project readme |
| 2026-09-01 13:06:25 | `6b8b1671643c5c62c90e0d2503c85f342c2f608e` | feat: refine workspace layout and boundary feedback |
| 2026-09-01 13:45:02 | `313b5464f5a3c37ecdcf0889e5affef10765597c` | feat: establish tool identity v0.1 |
| 2026-09-01 14:09:10 | `2891a5bbae9aa5994f615df35cc51cca2d3c40c2` | fix: refine boundary contact and context menu |
| 2026-09-01 15:27:54 | `1e825484c3657f7496d9858a50537b280b779b9e` | feat: establish tool opening v0.1 |
| 2026-09-02 11:37:03 | `2710ac12d6d535e58adb072fbe1959f0ac2e2ad0` | refactor: use fixed point 1d resize projection |
| 2026-09-02 11:58:52 | `eb8dadd3d41b6b95c5b8b3a7985d6ad2f9477fa1` | refactor: add deterministic 2d resize solver |
| 2026-09-03 01:14:51 | `4b26a0a601ea6f443285d28e47c73e5aa6e708c2` | docs: establish project history and change documentation policy |

## 后续维护

面向人类快速阅读的摘要维护于 [CHANGELOG](../CHANGELOG.md)，具体触发条件和完成检查见 [AGENTS Change Documentation](../AGENTS.md#23-change-documentation)。普通 Bugfix 通常只写 CHANGELOG；重要里程碑、方向替代、正式取消和影响产品方向的研究才扩展本文。

后续状态变化时保留原因与证据来源，及时更新当前状态；研究结论进入产品前，另行提供对应版本的实现、测试及交互验收。补写历史不授权改写 Git，也不授权重新启用已退役行为。
