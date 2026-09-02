# Module Development Reference V1

第一个正式模组的工程样板与测量记录：**Module System V0.1 + 去背景 / Background Remover 0.1.0**。

本文件是经验与证据，不是新的 SDK 规范。正式契约见 [MODULE_FORMAT_V1](MODULE_FORMAT_V1.md) 与 `IAddToolBoxModuleV1`；项目架构权威仍为 [ARCHITECTURE](../ARCHITECTURE.md)。开发下一个 Module 前先阅读本文件，并复制末尾 Checklist。

## 1. 状态与证据边界

- 2026-09-03：所有者明确回复“通过”，确认 V0.1 人工功能验收。真实图片的逐图尺寸、格式、评分与缺陷明细，所有者随后明确要求“暂时不方便，跳过”；未补造数据或质量评分。
- 前一轮自动验证：25 项模型/Package/ALC/Pipeline 检查、15 项隐藏 Host 集成检查通过。不是 40 项真实图片质量测试。
- 本轮 Reference：实际执行 1 次冷推理、3 次温推理、构建计时、启动/加载/安装复制测量及 11 类错误探针。测量没有改变生产代码、模型或依赖。
- 测量时 Git HEAD 为 `24948e8fd5932513d05bf0676a09d30e9b2c5763`，但包含本轮未提交的 Module Host 实现；**不能把下文性能归给原始空 Host commit**。该 Host/SDK 源码现已独立保存于 `c0da1f5591dce9eb19c3f3763775247141718f0a feat: establish module system v0.1`；Module/Reference 使用随后独立提交。
- `ARCHITECTURE.md` 的“尚未实现 Module / SDK 骨架”仍是较早 Host 基线的事实快照。本轮没有获得额外治理文档修改授权，因此未顺手改写；分层与信任边界没有改变。
- 本里程碑不是正式用户 Release，没有 Installer、Module Store、签名、权限沙箱或 Hot Unload UI。

## 2. 测量环境

记录日期：2026-09-03，Asia/Shanghai（UTC+08:00），约 03:56 起。

| 项目 | 实测值 |
| --- | --- |
| Windows | Windows 11 专业版，10.0.26200 / build 26200 |
| CPU | Intel Core i7-12650H，10 cores / 16 logical processors |
| RAM | 34,078,588,928 bytes（31.74 GiB） |
| GPU | NVIDIA GeForce RTX 4060 Laptop GPU，driver 32.0.16.1088；另枚举到 OrayIddDriver Device |
| .NET SDK | 10.0.400 |
| 执行 Runtime | Microsoft.NETCore.App / Microsoft.WindowsDesktop.App 10.0.11 |
| Host configuration | Debug，net10.0-windows |
| Module configuration | Release，net10.0-windows / win-x64，manifest version 0.1.0 |
| ONNX Runtime | CPU package 1.29.0，ORT_ENABLE_ALL |
| GPU inference | **Not enabled**；没有测量 VRAM，不能写成 0 MB |

测量未清系统/文件缓存，未停止用户已有应用，未改变 CPU 线程策略。数值是此机器当时状态的参考，不是硬件最低要求或跨设备保证。

## 3. 工程与源码规模

### Host / SDK

新增生产源码 6 个文件，合计 **412 物理行**（包含空行/注释）：

| 文件 | 职责 | 行数 |
| --- | --- | ---: |
| SDK/IAddToolBoxModuleV1.cs | BCL-only ToolViewType 契约 | 7 |
| App/ModuleManifest.cs | 严格 metadata / Windows ID / 路径验证 | 76 |
| App/ModuleInstallation.cs | 重复检查、临时复制、原子目录切换 | 66 |
| App/ModuleLoadContext.cs | managed/native 依赖解析与共享 SDK/WPF | 55 |
| App/LoadedModule.cs | Entry / ALC / 缓存 View 生命周期 | 57 |
| App/MainWindow.Modules.cs | 人工导入、启动发现、动态 Tile | 151 |

修改已有生产文件 3 个：App.csproj（+1 行 SDK reference）、MainWindow.xaml（+13/-1）、MainWindow.xaml.cs（+14/-1）。没有重写已有拖拽/碰撞/Resize 算法，没有修改 Core。文档、测试 Harness、bin/obj 不计入生产 LOC。

### Background Remover

独立目录 `modules/AddToolBox.BackgroundRemover/`，不加入 Host solution；共 10 个版本化源/说明文件。

| 类型 | 数量 / 行数 |
| --- | ---: |
| C# | 3 文件 / 327 行：Engine 196、View code-behind 123、Entry 8 |
| XAML | 1 文件 / 79 行 |
| csproj | 1 文件 / 27 行 |
| module.json | 1 文件 / 9 行 |
| prepare-model.ps1 | 1 文件 / 31 行 |
| 文档 | README、THIRD_PARTY_NOTICES、Models/README |

Entry 只提供 View Type；Engine 负责模型、预处理、推理、Alpha；View 负责 UI/文件选择/后台任务。没有 Manager/Factory/Repository 或额外 Host Service 抽象。

## 4. 构建测量

使用仓库已有 SDK；每组先做一次预热构建，再连续测量 3 次 warm build。elapsed 为完整 `dotnet` 命令的墙钟时间，包含正常 restore 检查，不清缓存。

| 命令 | warm 1 / 2 / 3（ms） | median（ms） | warnings / errors |
| --- | --- | ---: | --- |
| `dotnet build .\AddToolBox.sln` | 1439.508 / 1363.942 / 1402.250 | 1402.250 | 每次 0 / 0 |
| `dotnet build .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj` | 1529.189 / 1581.877 / 1521.105 | 1529.189 | 每次 0 / 0 |
| `dotnet build -c Release .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj` | 1463.356 / 1361.490 / 1344.553 | 1361.490 | 每次 0 / 0 |

Host solution build 不会构建独立 Module；两条构建链都必须验证。ONNX 模型由显式 `tools/prepare-model.ps1` 准备，build 不联网下载模型。缺少模型仍可编译和打开 View，但处理时明确失败。

## 5. Package 与安装体积

MB 采用十进制：1 MB = 1,000,000 bytes。最终 Release Folder：`modules/AddToolBox.BackgroundRemover/bin/Release/net10.0-windows/win-x64/`。

| 文件 | bytes | MB |
| --- | ---: | ---: |
| AddToolBox.BackgroundRemover.dll | 34,304 | 0.034304 |
| module.json | 283 | 0.000283 |
| Microsoft.ML.OnnxRuntime.dll | 240,440 | 0.240440 |
| onnxruntime.dll | 16,149,344 | 16.149344 |
| onnxruntime_providers_shared.dll | 21,856 | 0.021856 |
| System.Numerics.Tensors.dll | 410,936 | 0.410936 |
| Models/model.onnx | 224,005,088 | 224.005088 |
| AddToolBox.BackgroundRemover.deps.json | 2,868 | 0.002868 |
| AddToolBox.BackgroundRemover.runtimeconfig.json | 554 | 0.000554 |
| AddToolBox.BackgroundRemover.pdb | 22,496 | 0.022496 |
| onnxruntime.lib / providers_shared.lib | 2,124 / 2,314 | 0.002124 / 0.002314 |
| README.md | 3,149 | 0.003149 |
| THIRD_PARTY_NOTICES.md | 2,560 | 0.002560 |
| Models/README.md | 791 | 0.000791 |
| Licenses/ONNXRuntime-LICENSE.txt | 1,094 | 0.001094 |
| Licenses/ONNXRuntime-ThirdPartyNotices.txt | 343,249 | 0.343249 |
| Licenses/System.Numerics.Tensors-LICENSE.txt | 1,139 | 0.001139 |
| Licenses/System.Numerics.Tensors-ThirdPartyNotices.txt | 75,640 | 0.075640 |
| **合计 19 文件** | **241,320,229** | **241.320229** |

原生 import libraries、PDB 等是 NuGet/常规 build 输出，未为节省少量字节做额外裁剪。Package 不含私有 SDK DLL、Host DLL、源代码、obj 或 `.git`。模型只存在于被忽略的本地资源/输出目录，不进入 Git。

### Import

生产 `ModuleInstallation.Install` 在隔离目标复制完整 Package：**19 文件、241,320,172 bytes、153.333 ms**，包含复制、安装副本 Manifest 校验及目录 Move，不包含用户选择/确认时间，也不包含随后加载。

此复制测量发生在 README 验收状态文字更新前；最终 Package 只增加 README 的 57 bytes，所以最终体积为上表。用户实际安装目录仍为 **19 文件、241,320,172 bytes**；测量前逐文件 SHA 与当时 Release 完全一致。没有为了文档更新覆盖用户安装包，运行 DLL/模型未变化。

Installed root：`%LOCALAPPDATA%\addToolBox\Modules\addtoolbox.background-remover`。测量用复制目标与用户安装目录分离，完成后该临时完整复制已由探针删除。

## 6. 启动、加载与 View

启动测量是新探针进程中 `MainWindow` 构造 → Show → Dispatcher ContextIdle 的耗时，不包含 OS 创建进程到进入 Main 的时间。窗口隐藏、不抢焦点。空 Host 条件在探针内设置 `_modulesDiscovered=true` 跳过发现，以保留用户已安装目录；这是明确的测试条件，不是生产 fallback。

| 条件 | 3 次（ms） | median（ms） | Tile |
| --- | --- | ---: | ---: |
| 空 Host（探针抑制 discovery） | 536.799 / 546.225 / 539.761 | 539.761 | 0 |
| 实际已安装模组，正常 startup discovery | 553.259 / 537.383 / 558.632 | 553.259 | 1 |

不能把约 13.5 ms 的 median 差值当作精确恒定成本；样本少，OS/文件缓存/JIT 会产生噪声。启动只加载 Entry/Tile，不初始化模型。

一次细分加载测量（相同 Host 加载步骤，在探针中拆段计时）：

| 阶段 | ms |
| --- | ---: |
| 一级目录枚举、Manifest 读取验证、ordinal 排序（1 module） | 9.409 |
| ALC / resolver 建立与入口 Assembly load | 4.319 |
| Entry Type 查找与构造 | 2.015 |
| 首次 GetOrCreateView | 14.636 |
| 再次 GetOrCreateView | 0.142 |

Warm View 与首次 View `ReferenceEquals=true`。测量 View create 不等于首次像素呈现。独立运行的 Host 集成检查另确认 ContentControl 挂载、Back detach、再次打开复用、64×64 Tile、Lock、Reset、Pan/Zoom/Resize 世界坐标保持。

实际依赖路径已核实：ONNX managed 与 native DLL 都来自 `%LOCALAPPDATA%/addToolBox/Modules/addtoolbox.background-remover/`；managed ALC 名称为 `Module:addtoolbox.background-remover`，SDK 为 `Default`。没有把 ONNX Runtime 装入 Host 或 SDK。

## 7. 模型、预处理和图片格式

- Model：[onnx-community/BiRefNet_lite-ONNX](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX)。
- Base：[ZhengPeng7/BiRefNet_lite](https://huggingface.co/ZhengPeng7/BiRefNet_lite)。
- File：`onnx/model.onnx`，FP32，MIT；本地 `Models/model.onnx`。
- SHA256：`5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333`，下载后及首次 Session 初始化前验证。
- 外部预训练模型，不是 addToolBox 自己训练。模型和 ONNX Runtime 的许可证/第三方 notices 随 Package 提供。

Pipeline：WPF `BitmapDecoder + OnLoad` → 冻结 straight BGRA32 → bilinear 1024×1024 → RGB / 255 → ImageNet mean `[0.485,0.456,0.406]`、std `[0.229,0.224,0.225]` → float32 NCHW → CPU ONNX → stable sigmoid → bilinear mask resize → `PredictedAlpha × OriginalAlpha`。RGB 保留原图，不做激进阈值/羽化。

实际模型 Metadata：唯一输入 `input_image System.Single [1,3,1024,1024]`，唯一输出 `output_image System.Single [1,1,1024,1024]`；Engine 严格检查后才使用，不猜接口。

已自动实测 PNG、JPEG、BMP 解码，输入句柄释放；PNG 输出重解码验证尺寸、Alpha、原透明区不复活和 RGB 保持。输出为 RGBA PNG（内存 BGRA32）。TIFF 未支持/未实测，WebP 不支持。

## 8. Cold / Warm CPU、UI 响应与内存

输入是程序生成的 320×256 PNG 等价冻结 Bitmap（白底和彩色矩形，含一个原透明像素），每次固定同一输入。仅测流水与资源，不评价真实抠图质量。每次均走生产 Engine，后台 Task.Run，单 Session / 单 Run，没有并发图片。

首次 Session 初始化（含 SHA 和 Metadata 验证）**3989.622 ms**，独立计时；之后第一次 Process 是 cold inference。后续 3 次复用同一个 Session，未重新创建。

| 次数 | Preprocess ms | Inference ms | Postprocess ms | Process total ms | Save ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Cold inference | 23.670 | 6966.692 | 9.617 | 6999.981 | 17.596 |
| Warm 1 | 19.716 | 6613.173 | 7.005 | 6639.894 | 3.222 |
| Warm 2 | 20.375 | 5750.497 | 8.191 | 5779.062 | 2.712 |
| Warm 3 | 18.613 | 5627.067 | 7.596 | 5653.277 | 2.537 |

Cold Init + 首次 Process ≈ **10.990 s**；warm Process mean **6.024 s**、median **5.779 s**；warm inference mean **5.997 s**。Process total 不含 Decode、用户交互、Save 或 UI render；Save 单列。四次 PNG 都是 1689 bytes，SHA256 同为 `29c1605281cb9ecca2467816c1f120acbcda3255a76bbf3e776a9e0bb4609348`。

### CPU（近似）

进程 `TotalProcessorTime` delta 覆盖整个后台 Process 和探针 UI/采样，不是精准的 native Run 单独计数。平均占用 = CPU delta / wall delta / 16 × 100。wall 包含探针每 20 ms 检查完成的延迟，与 Engine Stopwatch 稍有不同。

| 次数 | wall ms | CPU time delta ms | 16 逻辑处理器归一化平均 CPU | UI 100ms 心跳次数 |
| --- | ---: | ---: | ---: | ---: |
| Cold | 7035.786 | 65703.125 | 58.37% | 56 |
| Warm 1 | 6667.333 | 62718.750 | 58.79% | 52 |
| Warm 2 | 5781.467 | 58046.875 | 62.75% | 45 |
| Warm 3 | 5684.149 | 56265.625 | 61.87% | 45 |

心跳证明本次后台计算时 Dispatcher 得到调度，不等同于所有硬件/输入都无卡顿。没有通过极低线程数人为压低 CPU。

### Memory（bytes）

使用 `Process.WorkingSet64` 和 `GC.GetTotalMemory(false)`；没有强制 GC。每次推理期间约 20 ms 采样，完成后约 500 ms 再取快照。Working Set 是整个探针 Host 进程的 OS 近似指标，不是精确 Module 独占内存，也不是 native allocation 总量。

| 阶段 | Working Set | GC managed estimate |
| --- | ---: | ---: |
| 空 Host | 113,242,112 | 4,065,512 |
| Module loaded | 114,343,936 | 4,393,008 |
| View opened | 116,649,984 | 5,351,552 |
| Session initialized | 460,582,912 | 5,782,984 |
| Cold 完成稳定后 | 7,696,920,576 | 19,327,288 |
| Warm 1 完成稳定后 | 12,541,972,480 | 37,329,024 |
| Warm 2 完成稳定后 | 12,559,802,368 | 55,445,544 |
| Warm 3 完成稳定后 | 12,577,976,320 | 7,633,856 |

逐次推理附近采样峰值：7,695,384,576 / 12,561,166,336 / 12,580,839,424 / **12,598,906,880 bytes**。最大约 **12.60 GB（11.73 GiB）**。

**重要限制：模型磁盘大小不是运行内存需求。** 此 CPU FP32 Session 在运行后保留大量 native/工作集内存；最后三次工作集接近，但四次样本不足以证明长时间无增长或所有图像尺寸安全，也不能仅凭这些指标断言泄漏。V1 缓存 View/Session 到进程退出，低内存设备未验收。未来重型模组生命周期和 GPU 方案必须据此单独评审，不能把本记录写成“轻量推理”。

GPU inference: **Not enabled**。WinML / GPU acceleration 为后续候选，未下载第二模型、未启用 CUDA/DirectML、未采集 VRAM。

## 9. 人工功能和质量记录

所有者在本轮回复“通过”，作为前一轮人工验收清单的总体确认：Import、Open、真实图片处理与 Save、Restart、Duplicate / Invalid Folder、Pan / Zoom / Drag、Lock。此前选中 `Models` 子目录造成 Missing module.json，已通过文件结构检查明确正确 Package 应选 `win-x64`；没有自动向上猜目录或修改导入语义。

用户明确跳过逐图材料，因此下表是证据缺口，不是“全部图像质量优秀”的声明：

| 类别 | 尺寸 / 格式 | Init / Inference / Post / Total | 主体完整性 / 边缘 / 毛发 / 漏背景 / 误删 | 用户评分 |
| --- | --- | --- | --- | --- |
| 人像 | 未提供 | 未提供 | 未提供逐图评价 | 未提供 |
| 商品/物体 | 未提供 | 未提供 | 未提供逐图评价 | 未提供 |
| 动物/毛发 | 未提供 | 未提供 | 未提供逐图评价 | 未提供 |

没有读取或提交私人图片、文件名、源路径。合成图只用于 Pipeline Smoke，不代替上述质量评价。未来标准质量集应单独建立，本次没有下载素材来代替用户验收。

## 10. 错误矩阵

11 类错误在隔离夹具中实际触发；探针捕获边界异常后隐藏 Host 均仍 `IsLoaded=true`，继续泵送 Dispatcher。以下不是 11 次生产 UI 自动点击：弹窗文本/错误入口由已读代码确认，手工 Import 清单由所有者总体验收。ALC 不提供对恶意代码或 native crash 的隔离。

| Case | 触发与实际异常 | 用户可见边界 | 生产安装残留 |
| --- | --- | --- | --- |
| Missing module.json | 缺文件，FileNotFoundException | 导入模组失败 | 无复制发生 |
| Invalid JSON | 单个 `{`，JsonReaderException | 导入模组失败 | 无复制发生 |
| Duplicate id | 已存在目标目录，IOException | 该模组已安装，不支持覆盖/更新 | 原安装保留 |
| Path traversal | `../escape.dll`，InvalidDataException | Package 路径不得逃出 Module Root | 无复制发生 |
| Missing entry assembly | 缺 DLL，FileNotFoundException | 导入模组失败 | 无复制发生 |
| Wrong entry type | `Does.Not.Exist`，TypeLoadException | 模组加载失败 | 原子 Move 后若加载失败，完整 Package 保留并明示；不是半安装 |
| Missing model | 模型不存在，FileNotFoundException | 去背景失败 / 模型初始化失败 | 安装不变 |
| Corrupt model | 3-byte 假模型，InvalidDataException | 模型 SHA256 不匹配 | 安装不变 |
| Unsupported image | `.webp`，NotSupportedException | 图片加载失败 / 支持格式提示 | 无输出 |
| Inference exception | 探针先 dispose Session 再调用 Process；实际 NullReferenceException | OnProcessClick 捕获并显示去背景失败 | 安装不变；此人为故障不是正常 Session 使用路径 |
| Save failure | 独占锁住临时目标，IOException | 保存失败 | 原文件不受覆盖；测试占位文件 0 bytes |

前轮另测：路径绝对地址和多级 `../` 被拒绝；重复安装不留下 `.importing-*`。复制/校验异常会清理临时目录；清理异常不吞掉，会返回 AggregateException 和具体路径。错误后的可恢复性不等于进程级安全沙箱。

**本轮临时夹具清理状态：** 合成 PNG 和完整安装复制已由探针删除。执行策略拦截了随后对 `artifacts/module-v1-reference/fixtures` 的递归清理，未换工具绕过；其中留有 17 个错误测试文件（16,927,702 bytes，含 0-byte `occupied.png`、3-byte 假模型和测试 DLL）。它们被 Git 忽略，不属于用户安装目录，不进入提交。探针已退出；没有后台推理任务。测量 Harness 同在 artifacts 下，不作为正式 SDK 测试框架发布。

## 11. 依赖、SDK 足够性与 Host 耦合

| 引用/资产 | 归属 | 验证 |
| --- | --- | --- |
| Host → Core | 原 ToolDefinition，保持 | 无 Core 本轮修改 |
| Host → SDK | Host-shared，Default ALC | 编译及类型身份检查 |
| Module → SDK | 唯一 Host ProjectReference；Private=false / ExcludeAssets=runtime | Package 无 SDK DLL |
| .NET / WPF framework | Default context | Module View 是 Host FrameworkElement |
| ONNX Runtime managed/native | Module-private | 实际 ALC 名称与安装路径核实 |
| System.Numerics.Tensors 9.0.0 | ONNX Managed 1.29.0 的传递依赖 | `.deps.json` 与 Package 核实 |
| FP32 ONNX 模型 | Module resource | 固定 SHA / 准备脚本 / package copy |

SDK 实际只使用 **ToolViewType**，V1 单入口宿主展示需求足够。Metadata 只来自 manifest，不增加运行时 Id/Version；ToolDefinition 是不可变投影，不是第二套业务权威。

| Module 是否必须知道/访问 | 结论 |
| --- | --- |
| App internal / MainWindow | NO |
| Host Resource Key | NO，View 使用本地小型 Style |
| World Canvas 内部 | NO |
| ToolDefinition / Core | NO |
| UI / Infrastructure 项目 | NO |

当前没有发现为完成 V1 必须补充 SDK 的功能成员。已暴露的后续设计议题是重型 View/Session 内存生命周期、可信模块的异常/UI边界和更新/移除流程，不在本轮提前创建复杂 Host Service API。ALC collectible 不保证 WPF/Native 资源立即卸载。

## 12. 下一个 Module 的最小模板与 Checklist

最小模板：独立 csproj、module.json、Module Entry、WPF View、README；有第三方依赖时加 THIRD_PARTY_NOTICES/许可证。只有存在当前计算需求才加入 Engine；大型外部资源才需要 Models 与显式 prepare script。不要复制去背景专属业务逻辑到 SDK。

DO：独立 build，Manifest SSOT，私有依赖自包含，SDK/framework 共享，耗时工作离开 UI 线程，资源路径相对 Module Assembly，记录 license，实际测量 cold/warm 和 memory。

DON'T：引用 App/Core、依赖 Host 私有资源、把模型或用户图片提交 Git、build 自动联网下载模型、每次 Open 重新加载模型、静默吞异常、宣称 ALC 是 Sandbox、只凭 build 声称手感/质量通过。

- [ ] Id 唯一且安全，Manifest 严格验证通过
- [ ] 独立 build；不加入 Host solution；Host 无 Module 源码引用
- [ ] SDK-only Host contract，Package 无私有 SDK DLL
- [ ] managed/native 依赖自包含；实际解析位置可验证
- [ ] 第三方来源、版本、模型 SHA 和许可证齐全
- [ ] 人工 Import、Restart、Duplicate、Invalid Folder
- [ ] View 首次创建 / Back / Warm reopen
- [ ] Pan / Zoom / Drag / Lock / Reset 不回归
- [ ] 缺资源、坏资源、业务错误、Save failure 的可见边界
- [ ] 真实图片/业务质量由用户判断；缺失证据明确留空
- [ ] Cold / ≥3 Warm、Package bytes/files、Import/load time
- [ ] Working Set / GC 辅助 / CPU 时间差；说明测量近似性
- [ ] 不把 GPU 未启用写成 VRAM 已测为零
- [ ] CHANGELOG、必要 PROJECT_HISTORY、Reference 同步且事实一致
- [ ] 逐文件暂存；不提交模型、私人数据、bin/obj、临时 Harness

## 13. 实际验证命令与后续边界

```powershell
dotnet build .\AddToolBox.sln
dotnet build .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj
dotnet build -c Release .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj
dotnet run --project .\artifacts\module-v1-smoke\ModuleSmoke.csproj -- <repo-root>
dotnet run --project .\artifacts\module-v1-smoke\ModuleSmoke.csproj -- <repo-root> --host
dotnet run --project .\artifacts\module-v1-reference\ReferenceProbe.csproj -- <repo-root> measure
dotnet run --no-build --project .\artifacts\module-v1-reference\ReferenceProbe.csproj -- <repo-root> errors
git diff --check
```

Smoke 命令是前轮实际执行记录，其中 `--host` 当时运行在未安装模组的空目录条件；不是声称当前重复运行仍使用同一外部状态。Reference 还分别以 `startup-empty` / `startup-installed` 启动新探针进程各 3 次。Harness 为本地忽略资产，不随 commit 发布；上文完整保留方法、输入条件、输出指标和局限，不承诺 clone 仓库后拥有这些临时探针。

V0.2 的 Batch、Auto/GPU/CPU、DirectML、性能开关、标准真实图片集尚未实现，也未混入 V1。V1 契约可复用不等于 V0.2 已验收；后续必须先满足独立基线与该阶段的授权/验证条件。

## 14. V0.2 backend gate

**2026-09-03 / Research / Blocked / Uncommitted。** 本节不是 V0.2 已实现或通过验收的记录。开始前 `git status --short` 为空；HEAD、本地 origin/main、`git ls-remote origin refs/heads/main` 均为 `88efae8083c4e173b727bbe048854e939f9ddaaa`。生产代码/模型/依赖和已安装 V0.1 均未更改。

### 检查方法

先完成后端可行性准入，而不是先把批量/UI 接到未知的 GPU 路径。两个独立 Release / net10.0-windows / win-x64 控制台探针直接 Compile-link 当前未改动的 `BackgroundRemovalEngine.cs`。探针自行计时创建 Session，经反射赋给 Engine 的 `_session` 后调用原 `Process`；这是隔离测试装配，不是生产 fallback 或改动初始化逻辑。读取模型前验证原 SHA，实际 Metadata 均为 `input_image float32 [1,3,1024,1024]` 与 `output_image float32 [1,1,1024,1024]`。预处理/后处理静态复核保持 RGB、bilinear 1024、/255、ImageNet mean/std、NCHW、stable sigmoid、mask resize 和原 Alpha 相乘；尚未重跑全部 V0.1 pipeline smoke，也未进行真实图片质量复核。

两个探针分别引用：

- CPU baseline：`Microsoft.ML.OnnxRuntime 1.29.0`，ORT_ENABLE_ALL，默认 CPU arena / Memory Pattern / 线程策略，没有降低线程数。
- GPU gate：`Microsoft.ML.OnnxRuntime.DirectML 1.24.4`，实际传递依赖 `Microsoft.AI.DirectML 1.15.4`、Managed 1.24.4。实时 NuGet 版本列表最高稳定版本为 1.24.4，不存在所查询的 DirectML 1.29.0；未将它替换进生产 csproj。设置 ORT_ENABLE_ALL、EnableMemoryPattern=false、ORT_SEQUENTIAL、AppendExecutionProvider_DML(0)，单 Session / 串行 Run，未做自动 CPU 回退。

硬件仍为 i7-12650H / 16 logical processors、约 31.74 GiB RAM、RTX 4060 Laptop GPU / driver 32.0.16.1088。用 DXGI EnumAdapters1 只读核实 index 0 为 NVIDIA 硬件适配器，非 Microsoft Basic Render Driver；没有 GPU 温度/利用率/VRAM 轮询。

输入是同一程序生成的 320×256 冻结 BGRA32 Bitmap：白背景；满足 `80 < x < 240 && 50 < y < 220` 的矩形 BGRA=(30,70,210,255)，(0,0) 原 Alpha=0。每次使用同一 Bitmap，未读私人图片、未保存输出 PNG。此合成输入只用于后端与内存诊断，不替代标准真实图片集；也不是 V0.1 原合成图的逐字节副本。

WorkingSet64 约每 100ms 采样；每次完成读取进程 Working Set 和 GC.GetTotalMemory(false)，输出原始 BGRA 像素 SHA256。CPU approximate = TotalProcessorTime delta / Process wall / Environment.ProcessorCount。无强制 GC、无清缓存/调线程/关闭用户应用。探针不是完整 Host，因此未测 UI heartbeat；原已运行 Host 仅另行只读检查窗口句柄及 Responding，不能代替 V0.2 UI/Batch 验收。与 V0.1 的 20ms Host 探针采样方式不同，峰值不是严格同条件比较。

### GPU：触发停止条件

| 项目 | 结果 |
| --- | --- |
| Providers / runtime | DmlExecutionProvider、CPUExecutionProvider；1.24.4 |
| Session init（含 SHA） | 4361.362 ms |
| 第 1 次 Pre / Inference / Post / Process total | 21.520 / 18298.253 / 14.999 / 18336.148 ms |
| 第 1 次平均进程 CPU | 0.325%（不是 GPU 利用率） |
| 第 1 次完成时 Working Set | 15892824064 bytes |
| 截至第 1 次完成已采样峰值 | 17556086784 bytes，约 17.56 GB；**不是第二次失败时整个进程的最终峰值** |
| 第 1 次 BGRA SHA256 | E0C6DAC5AE684DD87447F5EB961484D0DE370FD7AAF567972879D51A629E0EB2 |
| 第 2 次 Run | 失败；未启动后续 Run，进程 exit 1 |

原始关键报错：

```text
Non-zero status code returned while running DmlFusedNode_13_45 node.
DmlGraphFusionHelper.cpp(1078): 8007000E
OnnxRuntimeException [ErrorCode:Fail]
DmlCommandRecorder.cpp(342): 80004005
```

核对相同 rel-1.24.4 的 [DmlGraphFusionHelper.cpp](https://github.com/microsoft/onnxruntime/blob/rel-1.24.4/onnxruntime/core/providers/dml/DmlExecutionProvider/src/DmlGraphFusionHelper.cpp#L1069)：1078 行检查 ExecuteCommandList 的 HRESULT；随后 [DmlCommandRecorder.cpp](https://github.com/microsoft/onnxruntime/blob/rel-1.24.4/onnxruntime/core/providers/dml/DmlExecutionProvider/src/DmlCommandRecorder.cpp#L342) 为命令列表 Close 失败。微软将 [0x8007000E 定义为 E_OUTOFMEMORY](https://learn.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values)，表示必要内存分配失败。

**可以确认：** 当前 FP32 / DirectML / 本机组合未能稳定完成重复 Run，不能作为 V0.2 GPU 后端交付。**不能确认：** 具体哪种 RAM/设备资源分配耗尽、融合节点对应的单一原始算子、驱动与执行提供程序各自责任、是否长期泄漏。不能把融合节点错误写成“模型算子不受支持”，也不能仅凭高 Working Set 宣称泄漏。没有修改图、换 FP16、换模型、升级驱动、调整优化或增加重试来绕过本轮停止规则。GPU 只有 1 次成功，不能给出 GPU warm mean/P95 或完整性能/质量比较；与 CPU 原始像素 SHA 不同也不能直接推断画质好坏。

### CPU：重新测量约 12.60 GB 风险

原 CPU 1.29.0 新进程，Session init（含 SHA）3350.412ms；**1 cold + 19 warm，共 20 次真实 Process 均成功，exit 0**。Total 是 Process 的墙钟时间，不含 init/decode/save/UI。warm mean 5482.513ms；nearest-rank P95=6617.137ms（n=19，因此对应最大样本，统计置信度有限）。warm Pre / Inference / Post mean=23.230 / 5452.250 / 7.009ms。20 次平均 CPU approximate=61.509%；最大的单次平均值=62.215%，**这不是高频 CPU peak 测量**。

| Run（1=cold） | Process ms | 平均 CPU % | 完成时 Working Set bytes | GC estimate bytes |
| --- | ---: | ---: | ---: | ---: |
| 1 | 6256.036 | 57.87 | 7620255744 | 17751984 |
| 2 | 6617.137 | 61.29 | 12465266688 | 35193728 |
| 3 | 5760.796 | 61.93 | 12482871296 | 52531112 |
| 4 | 5464.358 | 61.85 | 12500557824 | 69859696 |
| 5 | 5416.359 | 61.73 | 12518060032 | 17765032 |
| 6 | 5379.254 | 62.21 | 12504330240 | 17755304 |
| 7 | 5394.872 | 61.24 | 12492275712 | 18354416 |
| 8 | 5386.678 | 62.13 | 12479541248 | 18354472 |
| 9 | 5404.843 | 61.49 | 12471574528 | 18354208 |
| 10 | 5418.017 | 61.86 | 12458745856 | 18353952 |
| 11 | 5425.803 | 61.48 | 12472500224 | 18374016 |
| 12 | 5350.782 | 61.94 | 12459741184 | 18353096 |
| 13 | 5373.842 | 62.11 | 12472659968 | 18349160 |
| 14 | 5377.088 | 61.89 | 12455579648 | 18364192 |
| 15 | 5399.809 | 62.00 | 12468379648 | 18359848 |
| 16 | 5405.080 | 61.36 | 12455809024 | 18371824 |
| 17 | 5418.553 | 61.20 | 12468592640 | 18372904 |
| 18 | 5456.996 | 60.95 | 12455796736 | 18373848 |
| 19 | 5360.733 | 62.10 | 12468600832 | 18375464 |
| 20 | 5356.740 | 61.55 | 12455849984 | 18376840 |

20 次 BGRA 输出 SHA 相同：`45BC3915AAAAC5E6DC91E69E42603FBD9C444F9C34CC5ADF1B77948E140D69CF`；不是 PNG SHA，与 V0.1 文件哈希不作直接比较。

- 全程采样峰值 **12543774720 bytes = 12.54 GB（约 11.68 GiB）**，重新证实 V0.1 约 12.60 GB 量级的资源风险仍存在，并没有因“模型只有 224 MB”而消失。
- 第 3–20 次完成时 Working Set 在 **12455579648–12518060032 bytes** 波动。本次观察不到随已处理图片数线性增长；不能外推为 100/1000 张批量稳定性或所有分辨率安全。
- 第 20 次后调用现有 Engine.Dispose（释放同一 Session），等待 500ms、不强制 GC：Working Set **108851200 bytes（108.85 MB）**，GC estimate **18422424 bytes**。释放前最后一次 Working Set=12455849984 bytes，GC estimate=18376840 bytes。
- 工作集显著随 Session Dispose 下降，而托管估计量基本相同，支持主要占用是 Session 生命周期关联的 native/runtime 资源，而非保留 20 个结果 Bitmap。[ORT arena 文档](https://onnxruntime.ai/docs/get-started/with-c.html#features) 说明默认 arena 会保留已申请区域用于复用；这与本次现象一致，但本次没有 native heap allocation trace，不能证明 12.54 GB 全部属于同一种 arena 分配。
- **性能风险保留，不定性为内存泄漏。** 此处 Dispose 仅为探针观察；生产 V0.1 仍缓存 Session 到进程退出，本轮没有悄悄改变生命周期。

### 未实施项目与清理/验证边界

GPU gate 已触发停止，生产仍为 V0.1：没有 Batch、Folder 递归输入、新桌面输出/失败记录、Auto/GPU/CPU UI、性能面板或有界批处理实现；没有 100/1000 任务测试、真实图片 CPU/GPU 比较、面板 ON/OFF 开销或新人工验收。公开图片只做来源候选查询，标准集尚未建立，下载图片数为 0，不虚报 20 张集/6 张性能集完成。

本轮临时工程位于已忽略的 `artifacts/background-v02-backend`、`artifacts/background-v02-cpu-baseline`；没有复制/修改用户安装模组，也没有读取私人图片。两个探针均已结束。原 V0.1 已忽略夹具不属于本轮清理范围，不动它们。

实际执行的探针命令：

```powershell
dotnet build -c Release .\artifacts\background-v02-backend\BackendProbe.csproj
dotnet .\artifacts\background-v02-backend\bin\Release\net10.0-windows\win-x64\BackendProbe.dll .\modules\AddToolBox.BackgroundRemover\Models\model.onnx gpu 4
dotnet build -c Release .\artifacts\background-v02-cpu-baseline\CpuBaselineProbe.csproj
dotnet .\artifacts\background-v02-cpu-baseline\bin\Release\net10.0-windows\win-x64\CpuBaselineProbe.dll .\modules\AddToolBox.BackgroundRemover\Models\model.onnx cpu 20
```
两个探针 build 都为 0 warnings / 0 errors。GPU 命令虽请求 4 次，实际第 2 次失败，仅完成 1 次；不得报告为 4 次通过。没有进行生产代码修复尝试、Git 写操作或新一轮 GPU Run。

后续构建检查（生产仍为 V0.1）：

| 实际命令 | warnings / errors | 验证范围 |
| --- | --- | --- |
| `dotnet build .\AddToolBox.sln` | **20 / 4，失败** | 原已运行的 AddToolBox.App（PID 26696）锁住 Debug 下 Core/SDK DLL；MSB3026 重试后 MSB3027/MSB3021 复制失败。未关闭用户窗口，也未修改源码或屏蔽 warning |
| `dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\v02-gate-host-build` | **0 / 0，通过** | 同一完整 solution 使用独立输出路径，验证编译与构建，不是运行/UI 验收；不能据此抹去上一条默认输出失败 |
| `dotnet build -c Release .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj` | **0 / 0，通过** | 原独立 V0.1 Module Release，不含新的 GPU/Batch 实现 |

**清理未完成：** 已验证三个本轮专用 artifacts 目录的绝对路径、文件清单和无 reparse point；随后递归删除被执行策略在进程启动前拦截，未换工具绕过、未删除任何已有夹具。保留：`background-v02-backend` 38 文件 / 51636288 bytes，`background-v02-cpu-baseline` 34 文件 / 17353348 bytes，`v02-gate-host-build` 120 文件 / 1338911 bytes；共 **192 文件 / 70328547 bytes**，均被 Git 忽略。它们是探针源码/构建输出，不含用户图片或新下载模型；NuGet 标准缓存亦未清理。原 V0.1 临时夹具继续不动。后续不得把这次状态写成“临时文件已全部清理”。

## 15. V0.2-P0 resource investigation

**2026-09-03 / Research / Uncommitted；不是 V0.2 生产实现或人工验收。** 按所有者新的 P0 授权调查资源，不延续上一轮生产开发。开始时 HEAD 与本地 origin/main 均为 `88efae8083c4e173b727bbe048854e939f9ddaaa`；只有本 Reference、CHANGELOG 和 PROJECT_HISTORY 三个预期文档修改。本节保留第 14 节的历史结果，不覆盖旧证据。本次临时源码、独立 csproj、模型和结果全部放在被忽略的 `artifacts/background-v02-p0`，未改 Host、SDK、生产 Engine/csproj、正式模型或已安装模组。

### 配置与测量边界

| 检查项 | 本次核实 |
| --- | --- |
| DirectML package / native runtime | Microsoft.ML.OnnxRuntime.DirectML 1.24.4；Microsoft.AI.DirectML 1.15.4；实际 ORT 1.24.4 |
| deviceId / DXGI adapter 0 | 0；NVIDIA GeForce RTX 4060 Laptop GPU，非 software adapter |
| GPU / driver | nvidia-smi：总显存 8188 MiB，driver 610.88；未升级驱动 |
| CPU / RAM | i7-12650H，10 cores / 16 logical processors；34078588928 bytes RAM |
| GraphOptimizationLevel | ORT_ENABLE_ALL |
| DirectML Memory Pattern / ExecutionMode | false / ORT_SEQUENTIAL，原探针已满足官方要求，无需修正 |
| Session / concurrency | 每个新进程仅一个 Session；所有 Run 复用它；并发 1；各实验依次执行 |
| CPU provider | 同时存在；ORT 默认隐式注册 CPU EP，可能承接图分区。不等于 GPU 失败后重新在 CPU 跑整张图；本探针没有这种重试，也未统计各节点的 EP 归属 |
| 输入/输出生命周期 | 输入是复用的托管 DenseTensor / NamedOnnxValue；Run 内部 finally 释放临时 native input OrtValue 包装；每次返回的 IDisposable 输出集合在 using 内读取后释放；最终 finally Dispose Session |
| CPU matrix | Microsoft.ML.OnnxRuntime 1.29.0；A/B/C/D 各新进程，1 cold + 5 warm；默认线程数 0/0，未调线程 |

官方依据：[DirectML 必要配置](https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html)、[默认 CPU EP 注册实现](https://github.com/microsoft/onnxruntime/blob/rel-1.24.4/onnxruntime/core/session/inference_session.cc)、[C# Run 的输入/输出释放实现](https://github.com/microsoft/onnxruntime/blob/rel-1.24.4/csharp/src/Microsoft.ML.OnnxRuntime/InferenceSession.shared.cs)。`GetAvailableProviders` 是可用列表，不冒充节点分区分析；CPU 隐式注册另由相同版本源码核实。旧 Engine 的输入包装不实现 IDisposable，不应凭这一点判为泄漏。

固定使用第 14 节定义的同一 320×256 合成 BGRA 图；反射调用未修改的生产 `Preprocess` 一次并复用 float32 `[1,3,1024,1024]` 输入。P0 直接计时 `Session.Run`，不含预处理、后处理、保存、监控或模型 SHA；Session init 同样不含 SHA，但包含 DML provider 创建。与旧 Process 总耗时、含 SHA 的 init **不可直接当作同口径提速**。模型输入尺寸没有改变；FP32 与公开 FP16 文件的实测 I/O 都是 float32，shape/name 与 V1 相同，不能因文件名推定全部算子都以 FP16 运行。

Working Set、Private Bytes、GC estimate、DXGI local/nonlocal CurrentUsage 约每 100ms 采样；峰值是采样下界。每次 Run 前后另外执行 nvidia-smi。WDDM 下本进程显存列均为 **N/A**，不是零；全卡 memory.used 包括其他程序，不能冒充本进程显存。DXGI CurrentUsage 单列为进程预算记账指标，尤其超出物理显存时，**不将其写成同等大小的物理驻留 VRAM**；它与 Private Bytes/Working Set 也不能相加当作总内存。

没有强制 GC、清缓存、调电源模式或关闭用户程序。每组只有一个进程序列，未随机化执行次序或控制温度，细小速度差不能推广；CPU 与 DML 的 ORT 版本不同，跨后端差异也包含版本因素。

### DirectML FP32：第二次再次失败，但错误码不同

相同生产 FP32 模型：224005088 bytes，SHA256 `5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333`。进程 PID 32044，init **2937.407 ms**；请求最多 10 次，只完成第 1 次，第 2 次失败后立即退出，未执行第 3 次、未重试或切 CPU。

下表 GB/MB 均为十进制；DXGI local 不是物理驻留 VRAM。

| 采样点 | Run elapsed ms | WS GB | Private GB | DXGI local GB | local Budget GB | 全卡 used MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Session 前 | — | 0.0604 | 0.0334 | 0 | 7.5372 | 1099 |
| Session 后 | — | 0.6888 | 1.1165 | 0.4855 | 7.5372 | 1564 |
| Run 1 前 | — | 0.6894 | 1.1168 | 0.4855 | 7.5372 | 1564 |
| Run 1 成功后 | 5369.285 | 17.8801 | 24.4094 | 23.6952 | 6.4958 | 7765 |
| Run 2 前 | — | 17.8802 | 24.4094 | 23.6952 | 6.8895 | 7765 |
| Run 2 失败后 | 11.353（失败耗时，非 warm 成功速度） | 17.8831 | 24.4125 | 23.6952 | 6.5248 | 7765 |
| Session Dispose + 500ms | — | 0.4440 | 0.4430 | 0.0730 | 7.5372 | 1106 |

全程采样 peak：WS **17883164672** bytes，Private **24414855168** bytes，DXGI local **23695249408** bytes，nonlocal **318328832** bytes。本进程 nvidia-smi 驻留显存未知；全卡采样点最高 7765 MiB，不伪称高频显存峰值。

本次第 2 次错误：`DmlFusedNode_14_49` / `DmlGraphFusionHelper.cpp(1075)` / **0x887A0005 DXGI_ERROR_DEVICE_REMOVED**。与第 14 节的 **0x8007000E E_OUTOFMEMORY** 分开记录：重复 Run 失败复现，**相同 OOM HRESULT 未复现**。近期 System 日志对 Display / nvlddmkm 的只读查询返回 NoMatchingEventsFound；没有 GetDeviceRemovedReason / DRED / 设备分配 trace，不能确认是 TDR、驱动缺陷、某个算子或哪笔分配。

当前证据支持显著资源超预算：DXGI local CurrentUsage 23.70 GB，Budget 约 6.50–7.54 GB；微软说明超预算可能导致停顿或资源创建失败（[DXGI budget](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/ns-dxgi1_4-dxgi_query_video_memory_info)、[D3D12 residency](https://learn.microsoft.com/en-us/windows/win32/direct3d12/residency)）。这使内存压力成为有证据的解释方向，**不等于已定位旧 18.3 秒和 OOM 的唯一因果链**。本次 Run 1 为 5.369 秒，并未复现 18.3 秒，不能提供可靠 GPU warm 数据。错误码含义见 [DXGI_ERROR](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-error)。

### CPU FP32 Memory Matrix

各组 6/6 成功；4 个新进程，PID A=31928、B=34892、C=8660、D=23292。内存 GB=10^9 bytes，Dispose MB=10^6 bytes。

| 组 | CPU Arena | Memory Pattern | init ms | cold Run ms | warm median ms（n=5） | peak WS GB | peak Private GB | Dispose WS MB |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A 生产默认 | on | on | 2939.122 | 5692.323 | 5317.749 | 12.4855 | 18.1519 | 120.386 |
| B | off | on | 3024.028 | 6815.849 | 5816.997 | 6.4973 | 6.5834 | 119.599 |
| C | on | off | 2947.935 | 5555.012 | 5129.077 | 7.6593 | 9.2751 | 122.282 |
| D | off | off | 2978.652 | 6935.317 | 6821.960 | 6.2659 | 6.3045 | 122.237 |

逐次完成时 WS GB（不是每次 Run 内部峰值）：

| 组 | Run 1 | Run 2 | Run 3 | Run 4 | Run 5 | Run 6 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 7.6198 | 12.4512 | 12.4498 | 12.4549 | 12.4595 | 12.4641 |
| B | 0.4001 | 0.4060 | 0.4105 | 0.4073 | 0.4139 | 0.4190 |
| C | 7.6175 | 7.6235 | 7.6203 | 7.6283 | 7.6332 | 7.6379 |
| D | 0.3985 | 0.4029 | 0.4018 | 0.4065 | 0.4108 | 0.4164 |

可支持的结论：

- **12.5 GB 主要受到 CPU Arena 保留与 Memory Pattern 分配策略的共同放大。** A→C 仅关 Pattern，peak WS 少约 4.83 GB、peak Private 少约 8.88 GB；A 第 2 次跃升而 C 无相应跃升，与首 Run 记录后复用分配 pattern 的机制一致（[SessionOptions 文档](https://onnxruntime.ai/docs/api/csharp/api/Microsoft.ML.OnnxRuntime.SessionOptions.html)）。尚无 allocation trace，不能精确给所有 native 分配贴标签。
- 关闭 Arena 后 B/D 的 Run 后 WS 仅约 0.40–0.42 GB，说明数 GB 常驻保留不是输出图片列表导致。**但是推理中 peak 仍约 6.27–6.50 GB**：模型/内核中间计算与分配存在真实高峰，不能说 12.5 GB 全是“多余缓存”，也不能把剩余峰值全等同于模型 tensor 的理论下界。
- A 第 3–6 次 WS 增加约 14.28 MB，同时 managed estimate 增加约 13.69 MB。每次复制一个约 4 MiB alpha 缓冲；旧数组已无权威引用但不强制 GC。这类短序列小幅增长与托管分配、采样波动应保留，不写成“内存完全不增长”。结合第 14 节 20-run 平台期，**未观察到 GB 级随图片数线性累积；不证明长期无泄漏或批处理安全**。
- A/B/C/D Dispose + 500ms 后 WS 都约 120 MB，Private 约 78–83 MB；释放前 managed estimate 约 40.5–40.9 MB，释放后约 40.7–41.0 MB。主占用随 Session 生命周期释放，不是强制 GC 制造的下降。
- 本次每组最后成功结果的 float32 alpha 全部逐字节相同，SHA256 `9C94A7A10EE3F65866EA0AA16DDC4EB4AA49D59653BA6DE26C90643A13BD640A`。仅证明此合成输入下矩阵选项未改输出，不是完整质量回归。

CPU 默认 A 应标为 **Functionally Stable but Resource Heavy**。B/C/D 是资源归因实验，不是已批准的生产配置调整；不能只因某一组数字最小就更改正式 Module。

### FP16 诊断：文件更小不等于所有后端更快

仅下载公开的 [onnx-community/BiRefNet_lite-ONNX](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX) FP16 文件到 artifacts；模型卡为 MIT。固定 revision `de15b22ba131738a16dff04aab8bdf8dc32e3ac1`，路径 `onnx/model_fp16.onnx`，**114538221 bytes**（FP32=224005088 bytes），SHA256 **`d39b897ceb16ae654c1731f3dba0cf9b368d9cae74b5a57459b455cc8bfec402`**。下载后与仓库 LFS SHA/size 核对；未转换、重导出、改图、改输入尺寸或替换生产文件。

**CPU FP16：停止于首张，不能报成功或低内存。** PID 11404，ORT 1.29.0，init=3547.337ms。原请求 6 次，但进程运行 **196.738 秒** 时仍在 Run 1，成功数 **0**。按所有者“CPU 性能无意义则记录即可”的边界，核实 PID 与完整探针参数后终止该临时进程，未重启；196.738 秒是进程存活时间，**不是完成一张的推理耗时**。终止时 WS=1571618816 bytes，OS lifetime peak WS=1609195520 bytes，Private 点值=2588372992 bytes，CPU time=310.75秒。没有成功输出、warm median、完整单张 peak Private 或 Session Dispose 测量；不得拿未完成计算的约 1.61 GB 与完成 FP32 的峰值宣称节省内存。详细终止快照保留为 `results/cpu-fp16-A-interruption.json`。

CPU FP16 与 DML FP16 的初始化各出现 **16 条运行时 warning**：8 个 `/bb/layers.* /Sub` 节点在两轮优化中无法以 CPU kernel 做 constant folding。这不是 build warning，也不直接等同于 Run 不支持该模型；DML 的 10 次成功与此区别相符。没有屏蔽 warning，也没有通过 graph surgery 处理。

**DirectML FP16：10/10 成功，有潜力但尚未达到 V0.2 准入条件。** PID 20992，ORT 1.24.4，init=4100.379ms，同一 Session、并发 1、同一 float32 输入；没有 CPU 整图失败重试。

| Run | elapsed ms | 完成 WS GB | 完成 Private GB | DXGI local GB | DXGI nonlocal GB |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 2570.561 | 6.5583 | 12.4549 | 11.8492 | 0.1916 |
| 2 | 2067.901 | 6.5676 | 12.4649 | 11.8502 | 0.1961 |
| 3 | 2047.862 | 6.5743 | 12.4723 | 11.8502 | 0.1992 |
| 4 | 2079.891 | 6.4930 | 12.4767 | 11.8502 | 0.1992 |
| 5 | 2039.156 | 6.3062 | 12.4815 | 11.8502 | 0.1992 |
| 6 | 2083.059 | 6.1861 | 12.4860 | 11.8502 | 0.1992 |
| 7 | 2050.281 | 6.0222 | 12.4904 | 11.8502 | 0.1992 |
| 8 | 2066.329 | 5.9078 | 12.4949 | 11.8502 | 0.1992 |
| 9 | 2040.718 | 5.7962 | 12.4993 | 11.8502 | 0.1992 |
| 10 | 2094.018 | 5.6861 | 12.5038 | 11.8502 | 0.1992 |

- warm median **2066.329ms**（n=9），本机约为 CPU FP32 默认 A 的 2.57 倍吞吐；只对应同一小合成图固定 1024 模型输入，不是 Batch 性能承诺。
- 全程 peak WS **6574669824** bytes，Private **12503818240** bytes，DXGI local **11850223616** bytes、nonlocal **199221248** bytes。相比失败的 FP32 DML，WS 少约 63.2%、Private 少约 48.8%、DXGI local 少约 50.0%；因此 **FP16 显著降低此 DML 组合资源量** 的诊断问题得到支持。
- nvidia-smi 全卡 used：Session 前 1108 MiB、Session 后 1333 MiB、Run 后 7661–7668 MiB、Dispose 后 1168 MiB。进程显存仍为 N/A。**11.85 GB DXGI local 仍超出约 6.49–6.94 GB 运行期 Budget**，全卡 used 也接近 8188 MiB 总量；不能判为本机资源合理，更不能将 11.85 GB 当作物理驻留显存。
- DXGI local 在 Run 2 后、nonlocal 在 Run 3 后进入平台；WS 下降。Private 在 Run 3→10 增加约 31.47 MB，同期 managed estimate 增加约 31.16 MB，与本探针逐次分配 alpha 数组相符；并非“所有内存完全不涨”，也未观察到 GB 级 native 占用逐图增加。没有长期、多输入或压力并发验证。
- Dispose + 500ms：WS **513421312** bytes，Private **510746624** bytes，DXGI local **71839744** bytes、nonlocal **67952640** bytes。未强制 GC；Session 大量资源释放，但不能声称释放到零。

### 输出差异与证据缺口

比较最后成功 alpha（1024×1024，stable sigmoid 后）与 CPU FP32 A：

| 比较对象 | Alpha MAE | RMSE | 最大绝对差 | Alpha 差 >0.05 的比例 | 0.5 阈值 foreground IoU |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU FP32 B/C/D | 0 | 0 | 0 | 0 | 1 |
| DML FP32（仅 Run 1 成功） | 7.18519e-8 | 1.36534e-6 | 0.0000514984 | 0 | 1 |
| DML FP16（Run 10） | 0.0000357225 | 0.000592470 | 0.0378762 | 0 | 0.999866806 |

FP16 alpha SHA256=`85D266065E0FDDDF8B7E76644E976AAA38321F18341C1D5FDDA522B60BFD390D`。还将两份 alpha 按生产相同插值/原透明度规则还原为 320×256 PNG 并逐像素读取：**RGB 81920/81920 相同**；350 像素 Alpha 有差异，最大 5/255、全图平均差 0.00687256/255。矩形中心仍为 BGRA=(30,70,210,255)，原透明角点 Alpha=0，没有从数值中发现整体前景丢失。预览工具对 FP16 PNG 的显示与像素读取不一致，故不据此声称可靠视觉验收；以以上文件与像素对照为准。

这些是**合成图数值/像素检查，不是人像、商品、毛发的视觉质量验收**；没有创建正式 Test Set 或读取私人图片。CPU FP16 无输出可比。FP16 DML 的完整质量门槛仍然未验证；不能因为该矩形差异小就写成“真实输出无明显退化”。

### Backend 决策与建议

下表结论针对当前目标机/当前用途；PROMISING 仅指诊断价值，不表示已进入 V0.2 候选或获得生产批准。Peak RAM 默认指 WS；CPU FP16 为被中止的不完整运行，不能与完整计算直接比较。GPU 表列的全卡值不是本进程物理显存峰值。

| Backend | 模型 | Warm 速度 | Peak RAM | Peak VRAM | 10-run 稳定性 | 结论 |
| --- | --- | --- | --- | --- | --- | --- |
| CPU | FP32，默认 A | 5.318 s | 12.49 GB | 未启用 GPU | 本次 6/6；前轮同默认 20/20，非本次重跑 10 次 | REJECTED |
| DirectML | FP32 | 无成功 warm；cold 5.369 s | 17.88 GB | 本进程驻留 N/A；全卡采样最高 7765 MiB；DXGI local 23.70 GB | Run 2 失败，1/10 完成 | REJECTED |
| CPU | FP16 | N/A；进程 196.738 s 时首张未完成 | ≥1.61 GB，不完整运行 OS peak | 未启用 GPU | 0/6，人工终止该探针 | REJECTED |
| DirectML | FP16 | 2.066 s | 6.57 GB | 本进程驻留 N/A；全卡采样最高 7668 MiB；DXGI local 11.85 GB | 10/10；资源仍超预算，真实质量未验收 | PROMISING |

CPU FP32 的 REJECTED 指**不适合作为低占用、通用本地默认方案**，不是功能失败；仍可按 A 方向保留为明确展示资源代价的 CPU fallback。当前默认确属 **Functionally Stable but Resource Heavy**。DML FP32 V0.2 Candidate=**REJECTED**。DML FP16 已证明能跑及显著降资源，但资源合理性、真实质量门槛未过，**不准入 V0.2**，也不开始硬修图。

建议选择 **A + D**：保留现有 FP32 CPU 作为明确的后备能力；下一轮优先 Model Comparison，寻找符合本地资源预算的方案。B（FP16 DML 直接进入 V0.2）证据不足；C（整体放弃 DirectML）也不应仅凭 FP32 失败作结，因为 FP16 已有 10-run 成功证据。本轮不实施任何建议，不更换后端、正式配置、模型或 SDK。

### 本次实际验证与保留资产

执行命令（repo 参数为本仓库绝对路径）：

```powershell
dotnet build .\artifacts\background-v02-p0\cpu\CpuP0.csproj -c Release
dotnet build .\artifacts\background-v02-p0\gpu\GpuP0.csproj -c Release
dotnet .\artifacts\background-v02-p0\gpu\bin\Release\net10.0-windows\win-x64\GpuP0.dll <repo> fp32 gpu A 10
# A/B/C/D 顺序各一个新进程，均成功：
dotnet .\artifacts\background-v02-p0\cpu\bin\Release\net10.0-windows\win-x64\CpuP0.dll <repo> fp32 cpu <A|B|C|D> 6
# 首次 Run 长时间未完成，PID 核实后终止，未重跑：
dotnet .\artifacts\background-v02-p0\cpu\bin\Release\net10.0-windows\win-x64\CpuP0.dll <repo> fp16 cpu A 6
dotnet .\artifacts\background-v02-p0\gpu\bin\Release\net10.0-windows\win-x64\GpuP0.dll <repo> fp16 gpu A 10
.\artifacts\background-v02-p0\analyze.ps1
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v02-p0\host-build
```

CPU/GPU 探针首次 build 各有一个 CS9191 警告（QueryInterface 的 ref/in 签名），仅修正临时采样器参数后，两项目重新 build 均 **0 warnings / 0 errors**。未修改推理逻辑去重试失败 GPU。最终完整 solution 独立输出 build **0 warnings / 0 errors**，仅验证 Host solution 编译，不冒充独立 Module 集成、UI 或 Batch 验收。FP16 runtime 的 16+16 条 constant-folding warning 与这项 build 结果分开保留。

探针数据包括每组 JSONL、CPU FP16 终止记录、DML FP16 console 日志、最终成功 alpha/PNG 和派生分析；全部留在被忽略的 artifacts，未纳入 Git。临时 FP16 模型也留在该目录便于证据复核，未动第 14 节遗留 artifacts 或用户安装目录。P0 未执行生产代码修复、Git add/commit/push 或任何其他 Git 写操作。

最终只读检查：`git diff --check` 通过（有 LF 将转 CRLF 的行尾提示，不是 build warning）；`git status --short --untracked-files=all` 仍只有上述三个 M 文档，暂存区为空；`git diff --exit-code -- src modules AGENTS.md ARCHITECTURE.md` 为空。HEAD / origin/main 仍为 `88efae8`，正式 FP32 SHA 未变。按进程名和探针命令行核对，本轮及此前相关 Probe 在后台的数量为 **0**；未关闭用户 Host 窗口。
