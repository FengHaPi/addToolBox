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
