# Module Development Reference V1

第一个正式模组的工程样板与测量记录：**Module System V0.1 + 去背景 / Background Remover 0.1.0**。

第1–13节保留V0.1验收快照，第14–16节保留后端与模型研究，第17–19节保留V0.2/V0.2.1实施与验证快照，第20节为 **DEFERRED / FUTURE QUALITY RESEARCH**。当前生产状态为 **V0.2.1 FROZEN / ACCEPTED WITH KNOWN LIMITATIONS**，见第21节。旧阶段的“未提交/等待验收/尚未实现”仅描述当时状态，不覆盖Owner最终产品决定。

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

记录状态更新：本节与 P0 记录在 P1 前置收口中由 `635bb60 docs: record background remover backend investigation` 独立提交并推送。以下保留原调查时点的工作树与命令记录，不代表 P1 仍停留在 `88efae8`。

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

记录状态更新：本节已随 `635bb60` 封存并推送；以下的 Uncommitted / HEAD 描述是 **P0 调查结束快照**。P1 是后续独立研究，尚无生产模型或后端变更。

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

## 16. V0.2-P1 Model / ONNX Export Comparison

**2026-09-03 / Research / Owner visual acceptance PASSED；收口已独立提交并推送。** P1 提交为 `ba199c72dd18e7d5e5e2f6e7c01a7c1253fb5bed docs: establish background removal model benchmark`，Commit Date **2026-09-03 05:42:58 +08:00**。8 个获批文件逐一暂存，提交后 HEAD、origin/main 和远端 main 一致、工作树干净，再进入第 17 节生产开发。P0 此前提交为 `635bb60361de9be7ca014dea516e8f006b0b275e docs: record background remover backend investigation`，2026-09-03 04:49:35 +08:00。仅使用命令级临时 Git 身份，未写 local/global 配置；P1 未修改生产代码。以下 P1 数据仍是研究探针结果，不是生产 Integration 测量。

### 16.1 模型身份、来源与图

| Candidate | 来源 / 固定 revision | bytes | SHA256 |
| --- | --- | ---: | --- |
| A：现有BiRefNet Lite FP32 | [onnx-community/BiRefNet_lite-ONNX](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX/tree/de15b22ba131738a16dff04aab8bdf8dc32e3ac1)，`de15b22ba131738a16dff04aab8bdf8dc32e3ac1`，onnx/model.onnx | 224005088 | `5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333` |
| B：同模型不同导出 | [CoderViking/birefnet-lite-onnx](https://huggingface.co/CoderViking/birefnet-lite-onnx/tree/dc06453148f01ef4131f17e9b791345e32e8ee78)，`dc06453148f01ef4131f17e9b791345e32e8ee78`，birefnet-lite-1024.onnx | 199681624 | `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67` |
| C：官方BEN2 Base | [PramaLLC/BEN2](https://huggingface.co/PramaLLC/BEN2/tree/e48a20765fb421d19dcdb0bf3cc61e802ca5ec8f)，`e48a20765fb421d19dcdb0bf3cc61e802ca5ec8f`，BEN2_Base.onnx | 222932053 | `22cea62108ff53b7ccc20f7a008bf30494228d84b1687f29ecbe76936a998101` |

三者来源声明MIT。B exporter记录的上游是ZhengPeng7/BiRefNet_lite revision `7838f1c3472f827cd8ce13ab5ccc2ce48077360f`，model.safetensors SHA=`4417d89795250e698c3cb0ae8df15743810065f646f48a694fdfa7ca052d0815`，已与固定revision API核对。未运行exporter、未下载PyTorch权重、未自行转换图。B/C ONNX仅进入被忽略的artifacts，正式A文件SHA未变。

| 序列化图实测 | A | B | C |
| --- | ---: | ---: | ---: |
| Node / Initializer | 16400 / 437 | 6129 / 969 | 13328 / 513 |
| Opset / Producer PyTorch | 17 / 2.0.1 | 17 / 2.8.0 | 15 / 2.5.1 |
| GatherND / ScatterND | 80 / 72 | 0 / 0 | 0 / 36 |
| GridSample | 0 | 300 | 0 |
| Shape / Constant | 412 / 7296 | 11 / 0 | 569 / 5286 |

A/B外部输入**原本都是静态**`input_image FP32 [1,3,1024,1024]`；输出都是`output_image FP32 [1,1,1024,1024]` logits。B的关键变化是GridSample-based deformable-convolution decomposition、折叠/精简/去重和内部图结构，不是把A的动态输入改静态。268个同名initializer原始值全同；忽略名称比较，A的437个initializer中377个raw hash在B出现，共158787400/178489160 bytes。折叠/分解后其余项不能据此证明全图等价。只读protobuf检查负责统计，实际ORT Session加载另验证可执行性。

### 16.2 Pipeline 与数值门槛

合成A/B固定输入直接反射未改的生产Preprocess：320×256 BGRA矩形→half-pixel bilinear→RGB NCHW /255 /ImageNet→1024²。输入SHA=`7AF96DA3AD0380DCEEA5D14715D9BE3F048B430FF68C139D0831A58F2D9A957C`。当前生产后处理仍是稳定sigmoid、双线性恢复、乘原alpha。

真实标准图A/B均使用上游推荐的PIL bilinear resize + ImageNet mean/std；二者tensor完全一致，但PIL antialias与生产自写resize并非逐bit一致。因此真实图是受控导出比较，不声称是生产UI原样回放。输出共同做sigmoid、双线性恢复、原alpha乘法，未加入阈值/形态学/去色边。

C实测输入`input.1 FP32 [1,3,1024,1024]`，输出`17728 FP16 [1,1,1024,1024]`。[官方ONNX脚本](https://huggingface.co/PramaLLC/BEN2/blob/e48a20765fb421d19dcdb0bf3cc61e802ca5ec8f/onnx_run.py)使用PIL Resize /255，不用ImageNet normalization或外加sigmoid；输出先F.interpolate到脚本传入的`[width,height]`、min-max、uint8截断，再PIL resize回原尺寸并替换alpha。本次保留其特殊宽高顺序，未擅自修正。临时NumPy实现保留FP16中间舍入，通过常量/恒等/中心采样检查；未安装PyTorch，**没有验证与Torch逐bit一致**，是质量比较限制。

合成A/B alpha：MAE=**0.00010149898**，RMSE=**0.00185693271**，max=**0.12861445546**；P50=4.15686e-7，P90=1.31130e-6，P95=1.89030e-6，P99=0.0003078965，P99.9=0.0338496104。>1/255像素比例0.5046%。差异图x100保存，非主观评分。

| 真实图A/B | MAE | RMSE | Max | P99 |
| --- | ---: | ---: | ---: | ---: |
| 普通人像 | 0.000254340 | 0.001719809 | 0.091227800 | 0.008481503 |
| 发丝 | 0.000490728 | 0.006776193 | 0.641342804 | 0.012153313 |
| 相机商品 | 0.000106372 | 0.002497309 | 0.555283770 | 0.000194974 |

16图MAE范围0.0000656–0.0021615，最大MAE出现在自行车细结构；透明难例局部max=0.956769。**低均差不代表边缘完全等价**，主体/细节/残背景/Halo的1–5数值评分仍未填写；所有者另已提供16.5节的定性替换验收。B CPU/DML六图alpha MAE6.28e-8–2.71e-7、max≤0.000133693；四个重复GPU输入B原始输出bit-identical，BEN2则不一致（raw FP16 max差0.0078125–0.0124512），仅作为数值稳定性注记，不冒充肉眼缺陷结论。

### 16.3 性能与资源

与P0同一RTX4060 Laptop / 8188MiB / 驱动610.88。CPU ORT1.29.0；DML ORT1.24.4 + Microsoft.AI.DirectML1.15.4，版本不相同。单Session/单并发；DML关闭MemoryPattern、ORT_SEQUENTIAL。各推理进程不重叠；约100ms采样WS/Private/DXGI，峰值可能漏掉更短尖峰。Session.Run计时不含预处理、后处理、SHA、保存或输入读盘；init不含SHA和此前provider append。不是完整Host延迟，也未调线程/强制GC/trim。

| CPU合成对照 | Init / Cold s | Warm median / max s | 次数 | Peak WS / Private GB | Dispose WS / Private GB |
| --- | --- | --- | --- | --- | --- |
| A | 3.4875 / 5.8087 | 5.9564 / 5.9572 | 1 cold +3 warm | 12.5663 / 18.2346 | 0.1667 / 0.1296 |
| B | 2.1341 / 2.6837 | 2.6213 / 2.6485 | 1 cold +5 warm | 3.4729 / 4.8776 | 0.1646 / 0.1254 |
| C，实际为真实人像首图 | 1.7702 / 未完成 | 无 | 0成功，60.0927秒停止 | ≥3.0781 / ≥3.6849，未完成下界 | 无，探针退出 |

标准6图固定为portrait-normal、hair-fine、animal-fur、product-hard-edge、complex-background、low-contrast（6000×4000高分辨率角色）。各候选同序，不换图绕过失败。

| 固定六图 | Cold +5 Warm，每图ms | Warm median ms |
| --- | --- | ---: |
| A CPU | 5728.768, 5776.774, 5667.159, 5640.846, 5357.064, 5549.507 | 5640.846 |
| B CPU | 2631.012, 2544.172, 2540.188, 2609.345, 2497.106, 2526.566 | 2540.188 |
| B DML | 462.054, 342.841, 330.531, 325.832, 325.188, 321.716 | 325.832 |
| C CPU | 首图超过60秒，后五图NOT RUN | N/A |
| C DML | 659.323, 554.799, 555.827, 551.793, 548.477, 554.372 | 554.372 |

A/B CPU还各完成其余10张不同质量图：16图全过程peak WS 12.5635/3.4726GB，Dispose后0.1570/0.1503GB。不是重跑20次合成扫描。以下GPU各至多10 Run（六图+前四图重复），不使用额外Run做质量补齐：

| DML | Init / Cold s | Warm median / max s | 成功 | Peak WS / Private GB | DXGI Local peak / budget GB | Dispose WS / Private GB |
| --- | --- | --- | --- | --- | --- | --- |
| B | 4.2168 / 0.4621 | 0.3232 / 0.3428 | 10/10 | 1.3025 / 7.2142 | 5.9618 / 6.7915–7.5372 | 0.8612 / 0.8734 |
| C | 2.5369 / 0.6593 | 0.5548 / 0.5646 | 10/10 | 1.0437 / 4.7860 | 3.7819 / 7.5372 | 0.6190 / 0.6141 |

每次Run的时间/WS/Private/DXGI记录在本地JSONL，未只留汇总。安全监控定义显著GPU预算超限为local>budget×1.10且超出>256MiB；另有GPU WS>8GB停止线。CPU BEN2>60s或>8GB即退出本探针，不重试、不进入后续Run；该CPU停止已实际发生。B/C GPU没有触发预算线、OOM或Device Removed。C有两行运行时warning提示部分节点未分配首选EP；未做节点分区profiling，**不能宣称全图GPU执行**。没有错误后转CPU整图重试。

WS不是总资源：B DML的Private达7.21GB、DXGI local达5.96GB，距本机预算不远。DXGI是WDDM进程记账，nvidia-smi的本进程物理显存N/A不是0；全卡使用量也不是本进程值。不可把这些口径直接相加或把WS1.30GB说成总内存只有1.30GB。Dispose仍有进程级资源，未做heap trace，不能直接认定泄漏。A危险FP32 DML不重跑，引用P0第二次device-removed/peak WS17.88GB历史证据，无本轮GPUwarm值。

### 16.4 标准集、质量资产与Package

[testset.json](../dev-assets/background-removal-testset/testset.json)是图片SSOT；16张（15照片+1插画），覆盖16类（category + additionalCategories），2 Public Domain、1 CC0、1 CC BY2.0、12 CC BY-SA。逐张记录作者、许可、源页/下载URL、SHA、真实尺寸、预期检查重点。Commons原图端点限流并明确建议预设缩略图后，3张选择官方1920px版本并标明；其余13张保持原图，固定性能高分辨率角色24MP、发丝36.15MP。原图总览发现项链实际是灰渐变背景，已修正分类；白底商品由Nikon相机覆盖，不按旧采集ID猜分类。

可入Git仅README、testset.json、prepare-testset.ps1、results-template.md；images/加入.gitignore。准备脚本只负责显式下载/校验，正确文件跳过，错误立即报告，保留损坏旧文件和失败partial，不自动重试/改许可/绕过TLS；普通build不下载测试图。本次通过16/16 VerifyOnly和普通调用skip分支，**未重新在空目录完整跑最终下载脚本**。

本地 `artifacts/background-v02-p1/` 包含模型、隔离源码/csproj、graph JSON、逐Run JSONL、原始输出、对比报告与数值差异图。`results/baseline`16张、`results/birefnet-static`16张、`results/ben2`6张透明PNG；每张另有深灰/白底合成，总计76张完整尺寸Halo预览。`quality-comparison/`有32张横向对比（每case深/白两张）；C未运行的10个case标NO OUTPUT，未复制A/B充数。透明PNG尺寸、RGB不变和两种背景精确合成自动检查通过；人工评分留空。Agent抽看普通人像/发丝/商品不等于所有者验收。

模型均≤300MB；B模型比A小24.323464MB，C小1.073035MB。按本地现有19文件Release目录241320225bytes只替换模型估算，CPU Package分别约216.997MB/240.247MB，不是候选真实打包结果。已有缓存的DML运行DLL组合比CPU约多19.702MB，debug/PDB/notices/最终打包规则另计；未决定同时打包两套ORT，也未改ALC/Module契约。

### 16.5 决策、验证与未完成门槛

- **A：Resource Heavy；默认低资源候选REJECTED（WS>8GB），现有生产保留不动。** 本次再次确认约12.6GB风险，不依据高WS称为泄漏。
- **B：Production Approved Candidate；Owner visual acceptance = PASSED。** 所有者2026-09-03明确确认：普通人物、发丝、商品硬边通过；逆光发丝、transparent hard case、thin structure未见相对A的明显新增退化。未发现足以拒绝生产替换的明显视觉退化，批准KEEP MODEL / CHANGE EXPORT。这是qualitative owner acceptance，未提供或虚构1–5评分；不承诺所有透明物体、发丝、细结构完美，不代表尚未执行的V0.2集成已验收。
- **C：GPU研究可行，CPU后备门槛失败。** 六图质量和官方后处理逐bit验证不完整，不能声称BEN2细节更好或STRONG CANDIDATE。不得仅凭较低GPU记账直接替换。
- **D未测试：** B已有合理结果，没有触发InSPyReNet备用条件；未扩大模型范围。
- **归因：** 同CPU/runtime/固定输入的WS降低约72.4%，加上同权重来源及图差异，支持ONNX export/执行结构是重要因素；不是证明某一算子是全部根因或整个模型家族不可用。
- **Future Candidate：Simple-background Fast Path。** 高置信度纯色/白底未来可评估传统算法，复杂图再跑模型；本轮仅记录，不实现。

完整本地对比表与原研究缺口：`artifacts/background-v02-p1/MODEL_COMPARISON.md`；研究资产被忽略，历史报告不反写成生产数据，不在Git中虚构可随clone获得的原始测试结果。定性验收另记录于本节和标准集评分模板。C的剩余10张未运行，Torch后处理bit-exact、低显存设备/长时批处理稳定性、V0.2 Host集成/UI仍未验收。获准的后续工作先验证单一ORT runtime与B模型的CPU/GPU Runtime Gate，再做生产回归与Batch；达到停止条件则停止，不能用P1成绩替代。

实际关键命令：

```powershell
dotnet build .\artifacts\background-v02-p1\cpu\CpuP1.csproj -c Release
dotnet build .\artifacts\background-v02-p1\gpu\GpuP1.csproj -c Release
# CPU：A 4次合成、B 6次合成、A/B各16张不同图片、C首图安全终止；每job独立进程。
& .\artifacts\background-v02-p1\cpu\bin\Release\net10.0-windows\win-x64\CpuP1.exe <repo> <job.json>
# B/C各一个10-run DML job；A不重跑。
& .\artifacts\background-v02-p1\gpu\bin\Release\net10.0-windows\win-x64\GpuP1.exe <repo> <job.json>
.\dev-assets\background-removal-testset\prepare-testset.ps1 -VerifyOnly
.\dev-assets\background-removal-testset\prepare-testset.ps1
& <bundled-python> .\artifacts\background-v02-p1\validate-results.py
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v02-p1\host-build
git diff --check
git status --short --untracked-files=all
```

两临时项目的默认正常build及最终五项目Host solution独立build均**0 warnings / 0 errors**。最初临时项目`--no-restore`因无assets.json出现NETSDK1004，正常已有缓存restore后通过；没有生产代码修复。BEN2的两行native EP warning与build分开记录。输出自动检查为38透明PNG/76合成PNG、16类覆盖；没有新增测试框架或冒称UI测试。原P1研究结束时所有推理/生成/检查进程已退出，生产目录和正式模型未改，仅留下三份文档、.gitignore及四个测试集文件的未提交修改，暂存区为空。

2026-09-03 P1收口复核：`dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v02-production\p1-closeout-host-build`通过，**0 warnings / 0 errors**；`prepare-testset.ps1 -VerifyOnly`重新核实16/16 SHA，`git diff --check`通过。此次仅更新所有者定性验收记录，没有重跑P1模型或补BEN2缺失图。Git提示的LF→CRLF工作树转换告知不属于编译warning。按授权仅逐文件暂存8个Git-eligible文件后独立提交，模型、图片、输出和artifacts不纳入。

## 17. V0.2 Production Integration

**2026-09-03 / Implemented / Uncommitted / 自动验证通过，等待所有者人工 UI 验收。** 这不是正式 Release，不创建 Tag，不提交或推送 V0.2。P1 已独立提交推送为 `ba199c7`，与本节生产修改分开。下述原始证据在被忽略的 `artifacts/background-v02-production/`，不宣称可随 Git clone 获得。第 14–16 节的历史测试、失败和未实现状态继续保留。

### 17.1 实现范围与依赖

模组 version **0.2.0**，id `addtoolbox.background-remover`、显示名称、kind 和 entry contract 不变。生产修改/新增共 **11 个文件**，均在 `modules/AddToolBox.BackgroundRemover/`：

| 文件 | 最终职责 |
| --- | --- |
| `AddToolBox.BackgroundRemover.csproj` | 单一 DirectML 发行包、Release 输出与许可证复制 |
| `BackgroundRemovalEngine.cs` | B SHA、lazy/reused Session、显式后端恢复、原图解码/后处理与安全 PNG 保存 |
| `ImagePreprocessing.cs`（新增） | 与 P1 一致的抗锯齿 bilinear RGB 重采样与 normalization |
| `ImageFiles.cs`（新增） | 文件夹递归、路径去重排序、桌面目录与不覆盖命名 |
| `BatchRemoval.cs`（新增） | 单 worker 顺序执行、逐项错误记录、停止与有界 UI 更新 |
| `BackgroundRemoverView.xaml` | 保留原预览与三按钮；右上设备下拉/性能按钮和浮层 |
| `BackgroundRemoverView.xaml.cs` | Single/Batch 状态、后台工作与进度/预览、桌面保存 |
| `BackgroundRemoverView.Performance.cs`（新增） | 可选的 1 秒进程采样与生命周期 |
| `module.json` | 仅升级 version |
| `tools/prepare-model.ps1` | 显式准备固定 revision / SHA / bytes 的 B |
| `tools/package.ps1`（新增） | 无模型下载的 Release 打包、模型/依赖/文件复制校验 |

另维护 7 份必要文档：根 `README.md`、`CHANGELOG.md`、`docs/PROJECT_HISTORY.md`、本 Reference、模组 `README.md`、`Models/README.md`、`THIRD_PARTY_NOTICES.md`。**SDK / Host / Core / UI / Infrastructure / World Canvas / MainWindow / AGENTS / ARCHITECTURE 修改均为 NO**；没有新公共契约、生产 Python/图像库、第二个模组或 Fast Path。

正式模型为 BiRefNet Lite FP32，来源 [CoderViking 固定 revision](https://huggingface.co/CoderViking/birefnet-lite-onnx/tree/dc06453148f01ef4131f17e9b791345e32e8ee78)，源文件 `birefnet-lite-1024.onnx`，包内 `Models/model.onnx`。revision **`dc06453148f01ef4131f17e9b791345e32e8ee78`**；SHA256 **`50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`**；**199681624 bytes**。仅使用已有已验证 B，不研究新模型；旧 A 在 ignored artifacts 保留备份，A/B 均不进入 Git。

采用 **Microsoft.ML.OnnxRuntime.DirectML 1.24.4 + Managed 1.24.4 + Microsoft.AI.DirectML 1.15.4**，传递依赖 System.Numerics.Tensors 9.0.0。DirectML 发行包的同一 native ORT 同时提供 CPU EP 与 DML EP，不携带 CPU 1.29.0，不改 PATH/ALC、不动态偷换 DLL。[ORT DirectML 文档](https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html) 要求的 MemoryPattern=false 和 ORT_SEQUENTIAL 已应用；GPU 使用设备0，表示选择DML，不承诺所有节点都在GPU上执行。真实隔离进程模块清单各只有一个 `onnxruntime.dll`，native文件版本 `1.24.20260316.9.2d92497`；DirectML为 `1.15.4+241025-1615.1.dml-1.15.fac7597`。许可证随包复制，DirectML使用独立Microsoft许可，不将其误称MIT。

### 17.2 Session、恢复和处理边界

打开 View 不创建 Session 或约12MB输入数组；首次处理才 lazy init。同后端整个 View 生命周期复用 Session，CPU/DML inference concurrency **1**；锁保护 Engine，设备切换仅在空闲可用，释放旧 Session，新 Session 下次处理才创建。输入 `float[3*1024*1024]` 由单 worker 复用，没有池/调度框架或并行推理。

Auto 首先尝试 DML。初始化或已识别的 DML 后端 Run 失败时释放旧 Session，记录 GPU unavailable，显示“GPU 不可用，已自动切换 CPU”，创建 CPU Session，当前项至多 CPU 重试一次，后续 Auto 直接 CPU，不每张重试GPU。强制GPU的初始化/后端Run失败抛明确后端异常并终止批次；不自动切CPU。ORT 1.24.4托管异常没有ErrorCode属性，Run分类依据该固定运行时的DML/HRESULT诊断；其他单项异常不会引发后端切换。**未安全模拟设备故障，因此恢复分支仅静态检查**；没有增加可触发的隐藏故障开关。显式CPU成功不冒充Auto故障切换实测。

PNG/JPEG/BMP能力保留；原V0.1不支持TIFF，未新增WebP等依赖。多选/拖入文件或文件夹，递归跳过reparse目录，路径OrdinalIgnoreCase去重并稳定排序。批次只提前保存路径，执行 **Load → Preprocess → Run → Postprocess → Save → awaited UI update → Next**；每个原图/结果更新都等待Dispatcher处理，无解码队列、全量Bitmap/结果集合或1000行UI。原图显示当前项，结果显示最近成功项。

单张Save直接使用DesktopDirectory，输出 `<stem>-no-bg.png`、`<stem>-no-bg (1).png` 等；桌面不可用明确报错。PNG先编码到当前项缓冲，再写本次独占临时文件并以不覆盖方式移动到最终名称；失败清理仅属于该次保存的临时文件。批次新建桌面 `去背景_yyyy-MM-dd_HHmmss` 文件夹，每成功一张自动保存，编号避免同名覆盖。解码/预处理/推理/后处理/保存异常按项隔离；UTF-8 `失败记录.txt`仅含索引、basename、阶段和短错误，详细异常仅Trace。停止禁止后续项启动，已经进入native Run的当前项允许完成。

UI状态明确为Idle、Loading、ReadySingle/Batch、ProcessingSingle/Batch、Stopping、Saving、ChangingBackend、Completed、Error。所有模型/解码/重采样/保存工作在后台，UI更新通过Dispatcher；Batch时选择/设备/保存禁用，“去除背景”变“停止”。只新增右上两个控件与小型性能浮层，没有改变两个预览的布局尺寸。

### 17.3 前置 Runtime Gate 与生产 Engine 测量

单位：时间ms，资源bytes；表中GB为十进制。均在本机RTX4060 Laptop环境运行，未改驱动、清缓存或调CPU线程。Gate直接使用既有P1输入张量与最终依赖，随后Engine探针反射调用真实构建DLL，使用固定6图轮转；这不是另一套生产实现。资源采样约100ms，峰值包含该进程的解码/图片/运行时占用。Cold/Warm只计Run，Engine init含SHA与Session创建；不把Run时间当作含解码、保存、UI的端到端时间。

| 测量 | Init | Cold Run | Warm median | warm成功 | Peak WS | Peak Private |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 前置Gate CPU | 2044.701 | 3997.821 | 3895.528 | 5/5 | 3506229248 | 4848230400 |
| 前置Gate GPU | 3875.709 | 420.790 | 288.366 | 10/10 | 1340764160 | 7254327296 |
| 生产Engine CPU | 2268.472 | 3965.853 | 3892.419 | 5/5 | 3916804096 | 5291122688 |
| 生产Engine GPU | 4572.377 | 422.348 | 289.281 | 10/10 | 1714524160 | 7638319104 |

CPU **未达3.5秒理想目标，但低于4秒停止线**，Gate WS低于6GB停止线。GPU明显低于1秒，无OOM/Device Removed；10-run有分配后的平台和瞬时图片缓冲波动，未观察持续native增长。生产GPU DXGI local峰值 **5961756672**、nonlocal **649818112** bytes，样本预算约7.25–7.27GB。WorkingSet不是总GPU内存，DXGI是WDDM记账，不与Private/WS相加；本进程物理显存未独立测得。

生产Engine Dispose后CPU WS373121024 / Private325365760；GPU WS827158528 / Private858374144、DXGI local/nonlocal0。Dispose不保证进程所有资源立即归零，也未通过heap trace证明长期无泄漏。证据：`runtime-gate-cpu.jsonl`、`runtime-gate-gpu.jsonl`、`cpu-production/results.jsonl`、`gpu-production/results.jsonl`。

### 17.4 标准集与生产输出回归

最终 `prepare-testset.ps1 -VerifyOnly` **16/16 SHA通过**，没有下载/重跑P1/BEN2。生产CPU选普通人像、发丝、商品、逆光、透明和细结构6类对照P1 B。

初次对照有两个输入不一致。模型无关诊断确认：重新按Pillow处理仍与原P1张量逐位一致，WPF仅在普通人物/商品默认应用ICC时改变RGB。使用 [IgnoreColorProfile](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.bitmapcreateoptions?view=windowsdesktop-10.0) 和与P1一致的抗锯齿bilinear后，六图输入张量全部0差异。只重跑受影响的2图，保留初始失败证据，不为其余4图制造重复结果。PNG均保持完整原尺寸、原RGB，Alpha最大差 **1/255**。

| Case | Alpha变化像素 | 最大差（8bit） | 全图Alpha MAE（8bit） | 最终证据目录 |
| --- | ---: | ---: | ---: | --- |
| portrait-normal | 8 | 1 | 0.0000012654 | quality-color-fixed |
| hair-fine | 86 | 1 | 0.0000023788 | quality-six |
| product-hard-edge | 10 | 1 | 0.0000007813 | quality-color-fixed |
| backlight-hair | 5 | 1 | 0.0000004815 | quality-six |
| transparent-hard-case | 6 | 1 | 0.0000024414 | quality-six |
| thin-structure | 222 | 1 | 0.0000182075 | quality-six |

全部RGB MAE=0。图像预处理仅RGB→1024×1024→/255→ImageNet mean/std→NCHW FP32；后处理stable sigmoid、mask bilinear回原尺寸、乘原Alpha，未加入threshold/erosion/dilation/feather/edge cleanup/color decontamination。这是生产数值回归，不伪造逐图1–5评分或V0.2人工验收。PNG数值比较不受PNG压缩文件SHA不同影响。

### 17.5 实际 Host Batch、错误和压力验证

临时 `HostIntegration` 引用未修改Host工程，启动真实WPF App，从真实安装目录由现有ALC发现0.2.0，调用其实际View/Engine。100-task夹具只轮转标准集路径，不复制100份已解码Bitmap；输出编号均由生产实现生成。测试程序的100ms资源/Dispatcher观测在两组面板对照中一致，不属于产品OFF时的采样器。

| 验证 | 真实Inference | 成功 / 失败 / 未处理 | 批次总秒数 | 成功张/s | Peak WS bytes |
| --- | ---: | --- | ---: | ---: | ---: |
| GPU100 | 100 | 100 / 0 / 0 | 119.671696 | 0.835619 | 2395836416 |
| CPU16 | 16 | 16 / 0 / 0 | 81.857857 | 0.195461 | 4555145216 |
| 混合坏图6项 | 3 | 3 / 3 / 0 | 6.803440 | 0.440953 | 1545408512 |
| 停止20项 | 3 | 3 / 0 / 17 | 6.706751 | 0.447310 | 1528856576 |
| 停止后重启2项 | 2 | 2 / 0 / 0 | 未另计时 | — | 未另采样 |

GPU100结果PNG数100；原图/结果各更新100次；Dispatcher100ms探针1091次回调，最大间隔171.625ms；进度单调、Session对象未逐图创建、完成后控件恢复。WS每10项约1.60、2.07、1.78、1.91、1.78、1.68、2.02、1.66、1.61、2.03GB，有回落，未观察到随张数线性增长。Peak Private8375476224、DXGI local6020038656 / nonlocal654348288；不据这一约2分钟批次宣称长期无泄漏。CPU16原图/结果各更新16次，最大Dispatcher间隔172.632ms，Peak Private5958963200；其4.555GB WS包含Host与大尺寸图像，不是前置Gate的纯运行时峰值。

坏图：empty.png、corrupt.jpg、not-image.bmp各Failed，夹在其中的3张正常图全部继续成功；失败记录3行均为解码阶段，未出现私人绝对路径/StackTrace。损坏JPEG测试出现WIC原生诊断“extraneous bytes / contains no image”，这是预期坏输入诊断，未屏蔽，和build warning分开。停止于已开始的第3张完成后，后17张没有启动；重新加载2张并处理通过。

1000项检查：2000条大小写重复路径→1000条，加载时 **0 decode / 0 inference**、原图为null、Session尚未创建；路径/命名阶段managed增长约7836264 bytes。用单一2×2位图做1000个重名输出，1000个文件全数保留，无覆盖。没有1000个结果集合/Row，**绝不称1000张真实推理成功**。这是实际100张推理加1000任务调度/路径/命名检查。

单张另有1次真实Auto处理，尺寸文本、结果、桌面连续保存2份与已有文件hash不变通过；空闲切CPU释放旧Session、Back/重进使用缓存View通过。性能浮层ON/OFF前后原图布局Rect相等。探针开发曾因语法、WPF StartupUri/ResourceAssembly和Host私有字段名各在推理前失败；依次据实际API/源码修正临时探针，未改Host/测试断言或生产逻辑迎合错误。最终7个Host job均exit0，失败日志保留。

### 17.6 Performance Panel 开销

产品默认OFF：无DispatcherTimer、无Process采样对象、无CPU/内存后台采样；Stage Stopwatch与批次进度不受OFF限制。ON创建一个1秒timer，CPU公式为进程CPU时间增量 / 墙钟增量 / 逻辑处理器数，内存为WorkingSet64；OFF解绑timer、释放采样Process。Back触发Unloaded停止采样，缓存View重开且此前ON才恢复。截图观察到实际GPU、CPU%、WS、当前耗时、吞吐和进度更新，浮层不推动预览。

相同30条GPU任务顺序，各新进程/Session，均包含初始化、解码、保存和UI，30/30成功：

| 面板 | 总耗时s | 吞吐张/s | Peak WS bytes |
| --- | ---: | ---: | ---: |
| OFF | 39.5349552 | 0.7588221575 | 2338357248 |
| ON（1秒） | 39.5552932 | 0.7584319967 | 2423685120 |

ON吞吐较OFF下降 **0.0514166%**，远低于10%停止阈值：**no material overhead observed**。这只有一组配对测量，不称zero overhead；WS差异可能受当前图像/GC峰值采样影响，未归因为timer泄漏。

### 17.7 Package 与开发安装升级

最终验收Package：`artifacts/background-v02-production/package-0.2.0-rc1/`，**19文件 / 236705882 bytes（236.706MB）**，打包后和真实安装目录逐文件size/SHA一致。`deployment-rc1.json`是19项清单，`validation-summary.json`为最终只读审计汇总。

| 文件/类别 | bytes |
| --- | ---: |
| Models/model.onnx（唯一模型B） | 199681624 |
| AddToolBox.BackgroundRemover.dll | 62976 |
| Microsoft.ML.OnnxRuntime.dll（managed） | 235592 |
| System.Numerics.Tensors.dll（managed） | 410936 |
| onnxruntime.dll（native） | 17328152 |
| onnxruntime_providers_shared.dll（native） | 22040 |
| DirectML.dll | 18527776 |
| manifest/deps/runtimeconfig/README/notices/licenses | 436786 |

无PDB、测试图片、benchmark日志、artifacts、SDK私有副本、旧A、BEN2、FP16临时模型、Debug DirectML或第二份native ORT。打包脚本只接受已有正确B SHA、拒绝已有输出目录、逐文件验证复制；普通build/package不下载模型。模型准备脚本下载分支未在本轮联网重跑，已有文件校验和固定参数已核实。

现有Host导入按duplicate ID拒绝更新，不修改Host。按所有者本轮明确授权关闭Host，先建立item-level SHA清单并保留V0.1于 `installed-v0.1-backup/` 与 `installed-v0.1-move-verification/`，再用完整Package做development-only本地替换。第一次Move后目标出现原V0.1内容，原因未证实；立即停止该次复制并只读核实两份均完整，未强制覆盖。核对Move-Item为原生命令后，后续单步移动、确认源目录不存在、创建空安装目录、独占复制与19项SHA校验完成。没有收到执行策略拒绝、没有策略绕过或删除备份。最终安装 `%LOCALAPPDATA%/addToolBox/Modules/addtoolbox.background-remover` 是已验证的0.2.0，Host实测发现版本一致；这不是正式Update UI或安装器。

### 17.8 构建、证据与尚未验证

实际关键命令如下，Host solution与独立Module分开。所有最终build均 **0 warnings / 0 errors**；无新增测试框架，没有将临时集成探针冒充全仓库dotnet test。

```powershell
# P1干净断点之前：0 warnings / 0 errors，8文件单独提交并push。
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v02-production\p1-closeout-host-build
# 独立Module Release和最终Package均0 / 0。
dotnet build .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj -c Release --no-restore --artifacts-path .\artifacts\background-v02-production\engine-build
.\modules\AddToolBox.BackgroundRemover\tools\package.ps1 -BuildRoot .\artifacts\background-v02-production\engine-build -OutputDirectory .\artifacts\background-v02-production\package-0.2.0-rc1
# 模型/CPU/GPU独立工程与真实DLL探针；每项实际执行于不同结果目录。
dotnet build .\artifacts\background-v02-production\runtime-gate\RuntimeGate.csproj -c Release
dotnet build .\artifacts\background-v02-production\engine-check\EngineCheck.csproj -c Release
& .\artifacts\background-v02-production\engine-check\bin\Release\net10.0-windows\win-x64\EngineCheck.exe <repo> cpu-production
& .\artifacts\background-v02-production\engine-check\bin\Release\net10.0-windows\win-x64\EngineCheck.exe <repo> gpu-production
# Host集成探针：checks / gpu100 / cpu16 / failure / stop / gpu30-off / gpu30-on，顺序运行。
dotnet build .\artifacts\background-v02-production\host-integration\HostIntegration.csproj -c Release --no-restore --artifacts-path .\artifacts\background-v02-production\integration-build
& .\artifacts\background-v02-production\integration-build\bin\HostIntegration\release\HostIntegration.exe <repo> <job> <unique-result-label>
.\dev-assets\background-removal-testset\prepare-testset.ps1 -VerifyOnly
# 最终五项目Host solution：0 warnings / 0 errors。
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v02-production\final-host-build
git diff --check
git status --short
```

原始测量：前置Gate两份JSONL，生产Engine CPU/GPU JSONL，质量初始与2图修正目录，`input-color-probe/`，`host-integration-results/`七个最终job、失败开发日志，package build日志与19项部署清单。最终审计只读取已有结果及核对包，不运行推理。所有源/模型/日志均留在对应ignored artifacts，没有BEN2补测、A危险DML重跑、驱动/系统依赖变更或V0.2 Git写操作。

剩余边界：Owner尚未做V0.2完整鼠标/拖放/Resize/UI人工验收；Auto故障重试与强制GPU后端失败未安全注入；低显存/其他设备、长时间大批次和所有读写权限/磁盘耗尽情形未实机覆盖。CPU约3.89秒仍未达到3.5秒理想目标。现有Host的缓存View无Hot Unload/正式Update UI，已按现有契约保留。以上不通过隐藏fallback、增加第三方依赖或改Host/SDK规避。

### 17.9 Owner人工验收清单

完成自动工作后启动空闲addToolBox，停止自动操作；所有者确认前V0.2不提交、不推送。

1. 选择一张人物图片。
2. 拖入一张图片。
3. Auto模式处理。
4. GPU模式处理。
5. CPU模式处理。
6. 查看去背景结果。
7. 保存PNG。
8. 检查桌面路径。
9. 重名保存自动编号且不覆盖。
10. 多选10张以上图片。
11. 拖入多个文件。
12. 拖入文件夹，确认递归收集。
13. 查看Batch进度。
14. 查看当前原图Preview。
15. 查看最近成功Result Preview。
16. 检查桌面新批次文件夹。
17. 确认逐项自动保存。
18. 停止批次。
19. 停止后重新处理。
20. 性能面板默认关闭。
21. 打开后右上Card位置合适。
22. CPU数据更新。
23. 内存数据更新。
24. 当前耗时更新。
25. 平均吞吐更新。
26. 关闭后Card消失。
27. 控件数量仍简洁。
28. 页面与预览没有被挤变形。
29. Resize正常。
30. Back返回Host正常。

## 18. V0.2.1 Batch Preview and Edge Correction

### 18.1 状态与范围

2026-09-03：**Uncommitted / awaiting owner acceptance**。第17节保留V0.2原始验证快照；本节记录其后的缩略图与边缘修正。只改5个生产文件：模组Engine、View XAML、View code-behind、module.json及package.ps1；同步5份相关说明。没有改变Host、Core、SDK、MainWindow、World Canvas、治理规则、模型、依赖、Batch执行架构或性能面板。版本号和打包默认输出改为0.2.1。

### 18.2 批量缩略图

根因是V0.2多图选择分支主动清空原图且没有列表。现在多图路径收集完成即显示底部横向列表，并默认加载第一项主预览；各项显示缩略图、截断文件名和等待/处理中/成功/失败。处理中项自动滚入可见区域、高亮并同步原图；点击其他项可查看该项原图，下一处理项开始时继续跟随。结果区仍显示最近成功结果，不保留整批全尺寸结果集合。坏图显示明确的预览失败占位，处理失败独立标记。

缩略图长边最多160，选择主预览长边最多1280；冻结位图、关闭输入流、单一异步解码任务、虚拟化回收列表和32项缓存淘汰阈值。JPEG/PNG使用WIC解码尺寸；其他编解码器可能先解码再缩放，不能把160像素保留尺寸说成所有格式的解码峰值上限。批次结束释放不可见缓存，切换文件集、返回单张或View卸载清空缓存；快速点选丢弃旧预览请求的结果。

首次1000条UI压力检查发现虚拟化回收时Loaded/Unloaded不能可靠代表可见项：4个实际容器对应34个陈旧可见条目。一次修正改为以实际生成容器及视口相交为依据，原断言不变，复测4个容器/13张缓存通过；失败日志保留。1000条仅为UI投影，**没有1000张真实推理**。

### 18.3 白边诊断与修正

现有PNG保留原图RGB，仅用预测Alpha替换透明度，半透明边缘中的亮背景混色因此进入导出文件。独立深色合成可复现发白。WPF的256级Alpha探针对比标准合成公式，最大通道差1/255，未发现预乘Alpha错误；没有修改预览合成器。参考：[Pbgra32预乘定义](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.pixelformats.pbgra32?view=windowsdesktop-10.0)、[BitmapImage解码尺寸](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.bitmapimage.decodepixelwidth?view=windowsdesktop-10.0)。

Engine在构造透明结果前默认执行保守RGB去污染，仅考虑Alpha 5–249的边缘。要求附近同时存在近不透明前景和近透明的中性亮背景，且边缘颜色符合两者的混色方向；保护亮色前景和输入自带透明度的像素。去污染强度受估算混色比例、Alpha及每通道最多32/255限制。**Alpha完全不变，不收缩蒙版，不新增开关**；同一结果供预览和PNG保存，因此两者同时受益。

真实C#方法处理6份既有PNG的检查如下，全部Alpha逐字节一致、近不透明区域不变、无RGB增亮、最大通道降低32。这里复用旧推理结果，只运行本次后处理。

| case id | RGB改变像素数 |
| --- | ---: |
| portrait-normal | 0 |
| portrait-short-hair | 4123 |
| hair-fine | 35 |
| backlight-hair | 6003 |
| thin-structure | 21456 |
| transparent-hard-case | 2851 |

合成白底污染样例的前景颜色误差总和由62400降至59328；输入透明像素保护断言通过。深色背景前后图包括[短发人像](../artifacts/background-v021/edge-results/portrait-short-hair-dark-comparison.jpg)及[逆光人像](../artifacts/background-v021/edge-results/backlight-hair-dark-comparison.jpg)，全图与变化最多处局部均明确标注。观察到部分亮边减轻，仍有残留；像素改变数量不代表质量评分，也不能据此认定所有亮边均为错误。

### 18.4 自动验证与交付边界

实际执行：

```powershell
dotnet run --project .\artifacts\background-v021\preview-probe\PreviewProbe.csproj -c Release
dotnet run --project .\artifacts\background-v021\edge-check\EdgeCheck.csproj -c Release -- (Get-Location).Path
dotnet build .\artifacts\background-v021\ui-check\UiCheck.csproj -c Release --no-restore --artifacts-path .\artifacts\background-v021\ui-build
& .\artifacts\background-v021\ui-build\bin\UiCheck\release\UiCheck.exe <repo> auto <module-build-output> ui-results-fixed
.\modules\AddToolBox.BackgroundRemover\tools\package.ps1 -BuildRoot .\artifacts\background-v021\module-build -OutputDirectory .\artifacts\background-v021\package-0.2.1
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v021\host-build
git diff --check
git status --short
```

Solution、独立Module及UI探针最终build均 **0 warnings / 0 errors**。没有新增测试框架，没有运行全仓库dotnet test。实际使用现有Host的LoadedModule加载器，在仓库内独立验收窗口装载真实模组DLL：8张选择立即有列表与默认预览，点击切换、160/1280尺寸检查通过；1000条虚拟化投影不创建推理Session。单张实际处理、预览、桌面PNG保存及旧文件hash不变通过；批次8张正常图加1张坏图得到8成功/1失败、8份PNG和1行失败记录，观察到8个当前项高亮和匹配的原图预览。批次结束缓存4张，切回单张缓存为0。总计 **9次真实推理**，不是无推理验证。原始记录：`artifacts/background-v021/ui-results-fixed/events.jsonl`。

最终Package `artifacts/background-v021/package-0.2.1/`：19文件、236719039 bytes；模型仍为Static BiRefNet，199681624 bytes，SHA-256 `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`。没有额外模型、SDK私有副本、PDB或测试数据进入包。本轮不修改仓库外的已安装0.2.0目录，提供仓库内0.2.1验收窗口；这不是已安装Host升级或Owner人工验收通过。

最终包另通过实际Windows文件对话框多选8张图片，观察到默认原图及横向缩略图；实际点击逆光缩略图后，主预览与选择一致。此阶段没有再次推理，保留“去背景 V0.2.1 · 模组验收”窗口供Owner操作。以上是代理执行的UI检查，不是Owner人工验收。

限制：保守修正不能消除宽范围白雾，也可能跳过无可靠前景/背景参照的细发丝；自然逆光与亮色细节仍需Owner确认。真实鼠标拖放、其他设备和长时间大批量未在本轮覆盖。没有BEN2补测、模型切换、Git暂存、提交或推送。

## 19. V0.2.1 Edge Quality Refinement

### 19.1 本轮起点、调用链与范围

2026-09-03，**Implemented / Uncommitted / awaiting owner quality acceptance**。Owner确认缩略图基本可用，授权继续补齐Alpha与RGB边缘处理。本节替代第18节RGB-only方法作为当前质量行为；第18节仍保留当时测试事实，不能把当时的“Alpha不变”当成本轮行为。

起点分支main，HEAD `ba199c72dd18e7d5e5e2f6e7c01a7c1253fb5bed`；已有13个修改文件、5个未跟踪文件，均为前轮开发内容。完整逐文件起点SHA和Git状态保存在 `artifacts/background-v021-edge/start-state.json`。本轮仅2个生产文件：修改 `BackgroundRemovalEngine.cs`、新增 `EdgeRefinement.cs`；同步CHANGELOG、PROJECT_HISTORY、本Reference、根README和模组README。未改View、BatchRemoval、ImageFiles、Performance、manifest、csproj、模型/依赖、Host/Core/SDK/Infrastructure/MainWindow/World Canvas或治理文件。

入口仍为BackgroundRemoverModule → BackgroundRemoverView。Single和BatchRemoval均调用Engine.Process；其内部顺序仍为预处理→一次模型Run→sigmoid与双线性Alpha回原尺寸→乘原Alpha→边缘处理→冻结BGRA结果。单图Save和Batch均经ImageFiles.SaveUnique/Engine.SavePng输出桌面，ResultPreview直接显示相同结果。性能面板仍为原有默认关闭、开启后1秒采样，未扩展。产品只有棋盘格结果预览；黑/白预览属于本轮离线验收材料，没有新增产品控件。

前置检查确认前一版已有半透明边缘RGB去污染，没有Alpha精修、阈值裁切、羽化或腐蚀；只接受中性亮背景且最多减色32/255。这解释灰/彩色/暗色背景覆盖不足。当前架构文档的Module实施状态仍是历史快照，与已存在的Module代码/Reference不完全同步；本轮沿用现有Module内部职责和契约，不借质量修正改写架构。

### 19.2 默认后处理

不是叠加第二套旧算法：以EdgeRefinement替换旧DecontaminateEdges。输入/输出是straight BGRA，颜色推断以[Alpha合成关系](https://www.w3.org/TR/compositing-1/#simplealphacompositing)为出发点，但采用编码RGB下的局部保守启发式，不宣称物理正确matting。

- 仅考察Alpha 5–249；在有上限4–12px半径的8方向搜索中，要求近不透明前景（Alpha≥250）、近透明背景（≤4）、足够颜色差异，以及当前边缘符合两者混色方向。颜色不一致或没有可靠参照时保留。
- Alpha：当混色估计提示覆盖度偏高，轻微下降，浮点上限为min(6, 0.06×Alpha)，最后8bit舍入，最大实际下降6。相对两侧邻居更高的局部Alpha峰值保留；不清除任何原非零Alpha像素，不扩展背景、不做整体腐蚀、模糊或羽化。因量化舍入，低Alpha的相对百分比可略高于6%，不能宣称逐像素严格≤6%。
- RGB：沿局部背景到前景方向做有正负号的校正，每通道最多48/255，可处理白/灰/彩色/黑色混色；不统一扣白，不改近不透明主体。源图自带Alpha<255的像素不参与边缘调整；最终Alpha=0的隐藏RGB统一清零。
- 先租用一个byte/像素的不可变Alpha快照，所有邻域从快照读取，前景/背景参照RGB在采样过程中不改，最后清零透明RGB；避免扫描方向传播。ArrayPool在finally归还，无第二次推理、无每像素对象、无批量并行。池可能暂时保留已租用容量，不声称归还后WorkingSet立即归零。

一次生产实现即通过本轮边界与功能检查，没有修改测试预期迎合实现。临时测量程序最初未保留LoadedModule强引用，导致ALC卸载后依赖加载失败，发生在首个模型Run输入转换阶段；修正探针生命周期后通过，未改Host或生产加载器。该失败日志保留，不计成功推理。

### 19.3 九类质量对照

为隔离后处理，使用原图编码RGB与既有P1 B Alpha，分别调用上一版真实DLL的RGB-only方法和本轮真实DLL的EdgeRefinement。两边使用同一Alpha，不重跑模型来制造差异。P1与生产Alpha此前核实最大差1/255；本次固定Alpha对照不能等同于逐图重新推理的质量打分。真实功能/性能推理另列。

每例保存before/after全尺寸透明PNG、纯黑/纯白对照图。图中包含全图与按预乘颜色变化量选出的3个256px局部，选区规则明确标注；不是只展示有利裁剪。所有RGBA数值检查、18张黑白对比图及PNG本体已检查。索引：[`comparisons/index.html`](../artifacts/background-v021-edge/comparisons/index.html)，全尺寸文件在 `quality/before/`、`quality/after/`。

| 真实case id | Alpha改变像素 | 可见RGB改变像素（相对上一版） | 代理观察，非Owner验收 |
| --- | ---: | ---: | --- |
| portrait-normal | 3581 | 5005 | 头发外缘及衣肩局部灰边轻微减轻，整体变化小 |
| hair-fine | 25844 | 37343 | 衣袖/裤边较易观察，复杂卷发改善有限 |
| portrait-long-hair | 22822 | 37120 | 发顶和肩部轮廓灰边有所减轻，宽雾状残留仍在 |
| backlight-hair | 14115 | 24859 | 边缘略干净，自然金色亮边保留；逆光仍困难 |
| animal-fur | 861 | 1178 | 耳缘局部变化很小，绒毛发脏改善有限 |
| product-hard-edge | 29735 | 62649 | 相机挂环/硬边外侧薄灰边小幅减轻，高光与残留仍在 |
| product-white-background | 45311 | 70330 | 项链局部灰轮廓减轻，反射与原有残留仍在 |
| thin-structure | 68891 | 151216 | 辐条周围污染有减轻，原有断续/锯齿没有恢复 |
| transparent-hard-case | 6148 | 8320 | 轮廓局部修正，透明语义及内区黑白残留没有解决 |

九图全部：Alpha下降≤6、原非零Alpha支撑集合逐像素不变、不透明区域逐字节不变、最终Alpha=0的RGB全为0。源图既有透明度保护由合成夹具单独验证，不能把JPEG透明困难样本称为源Alpha保护测试。已知白/灰/彩/黑混色夹具的前景RGB误差分别由24000→15840、8240→4400、13500→8040、12020→6560，均有Alpha微调；Alpha为1/24/100/240的单像素孤立细线全部保持。像素变化数、夹具误差不等于主观质量评分。

### 19.4 配对性能与稳定性

RTX4060 Laptop，同一固定6图performanceSet轮转。每组新进程和Session，GPU 1 cold+10 warm、CPU 1 cold+5 warm；推理与后处理来自生产RemovalResult计时。Total为Engine.Process，含初始化（仅cold）、预处理、Run与后处理，**不含文件解码/保存/UI**。

| 路径 | GPU before | GPU after | CPU before | CPU after |
| --- | ---: | ---: | ---: | ---: |
| Cold Run ms | 550.096 | 545.300 | 4273.661 | 4273.069 |
| Cold Total ms | 5015.371 | 4977.237 | 7094.260 | 7142.507 |
| Warm Run median ms | 426.582 | 424.540 | 4258.832 | 4239.066 |
| Warm Postprocess median ms | 136.597 | 149.893 | 160.551 | 166.581 |
| Warm Total median ms | 657.553 | 673.115 | 4558.872 | 4489.758 |
| Peak WorkingSet bytes | 1767796736 | 1946742784 | 3943661568 | 4067852288 |
| Peak Private bytes | 7687532544 | 7905402880 | 5310914560 | 5460045824 |

GPU Total增加2.37%，CPU Total下降1.52%（不据单组波动宣称提速）；Postprocess中位数分别增加13.296ms和6.030ms，包含Alpha放大、边缘处理与Bitmap创建，未在生产新增更细性能面板。九图纯后处理一次调用差值约-76至+56ms，有JIT/池热身差异；配对生产总耗时更适合判断可用性。未观察明显性能倒退。

GPU after WorkingSet在图像轮转中约1.09→1.61→1.79→1.69…→1.93→1.73GB，有回落；CPU after约2.11→3.94→4.07→3.92→3.94→3.89GB。新增池化快照和GC时机带来有限额外占用，本次没有随张数明显线性增长证据；短样本不能证明长期无泄漏。Dispose后GPU/CPU WS约953/540MB，GPU DXGI local/nonlocal均归0，测量进程正常退出。没有故障注入、其他硬件或长时间压力测试。

排除的诊断组：上轮验收窗口已完成8张并保存，但仍持有GPU Session；测量时另一进程DXGI预算约2.1GB，旧版Warm Run达4045ms。检查完成状态后关闭旧窗口，释放Session，再做上述四组。资源竞争组日志在 `baseline-gpu/events.jsonl`，不混入配对统计。共54次成功真实推理：竞争诊断11、正式GPU前后22、CPU前后12、功能9；固定Alpha九图对照为0推理。

### 19.5 构建、功能与交付

实际命令（`<repo>`、`<package>`为本节记录的绝对路径参数）：

```powershell
dotnet build .\AddToolBox.sln
dotnet build .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj -c Release --artifacts-path .\artifacts\background-v021-edge\_work\module-build
dotnet build .\artifacts\background-v021-edge\_work\probe\Probe.csproj -c Release --artifacts-path .\artifacts\background-v021-edge\_work\probe-build
# Probe分别执行quality、clean-before/after-gpu、clean-before/after-cpu。
& .\artifacts\background-v021-edge\_work\probe-build\bin\Probe\release\Probe.exe <repo> <package> <job>
.\modules\AddToolBox.BackgroundRemover\tools\package.ps1 -BuildRoot .\artifacts\background-v021-edge\_work\module-build -OutputDirectory .\artifacts\background-v021-edge\package-0.2.1
& .\artifacts\background-v021\ui-build\bin\UiCheck\release\UiCheck.exe <repo> auto <new-package> <ui-validation-folder>
git diff --check
git status --short
```

Solution、独立Release、临时探针及Package最终build均0 warnings / 0 errors。没有全仓库dotnet test或新增测试框架。复用已有UiCheck，以真实Host加载器加载本轮Package：单张处理/预览/桌面保存/旧文件hash不变通过；8张正常图加1张坏图批次8成功/1失败、8PNG加1行错误记录，耗时10.897秒。8个处理项高亮、状态、原图一致；1000项仅UI虚拟化保持4个容器/13张缓存、0推理；批次结束缓存4张，切回单张为0。错误记录、缩略图与保存逻辑未修改。原始证据 `ui-validation/events.jsonl`。

包 `artifacts/background-v021-edge/package-0.2.1/`，19文件/236719917 bytes；Module DLL SHA256 `31e87f4125d7f70a961743a999c1bead7779ab5c7fc1aa3a9dc70c199f276c3e`。模型SHA/依赖与前轮完全相同，未联网下载模型；未升级安装目录。前后质量PNG、黑白对比、验收包及证据日志作为交付保留，前轮资料和Owner生成的桌面输出不清理。

清理未完成：本轮198个临时文件、334207620 bytes已按绝对路径/大小/SHA256生成不可覆盖的 `cleanup-manifest.json`。执行删除前的命令被自动审批以 `blocked by policy` 拒绝，没有提供更具体原因，未绕过策略再次删除。四个目标仍保留：本节artifact根下 `_work/`、`ui-validation/zz-corrupt.png`，以及桌面的 `portrait-short-hair-no-bg (1).png`、`去背景_2026-09-03_143259/`。记录见 `cleanup-result.json`。自动测量与功能探针均已退出；此清理限制不等同于验证失败。

尚未验证：Owner对黑/白边缘、卷发/绒毛、逆光高光、透明语义和商品硬边的主观接受度；实际鼠标拖入、其他设备、低显存故障恢复和长时间批次。保留的非零Alpha集合不能证明所有细节在感知上完全不变，也无法恢复模型已漏掉的结构。没有新增档位、第二模型、第二次生产推理、Git暂存/提交/推送或治理变更。

## 20. V0.3-P0 Quality and Subject Completeness

### 20.1 研究边界与模型身份

2026-09-03最终状态：**DEFERRED / FUTURE QUALITY RESEARCH**。原始评分仍为PENDING OWNER REVIEW，不补填通过分数；此研究不再是当前生产计划，不启动C ONNX Export、V0.3-P1或自动研究。下文保留研究执行时的事实：起点main / HEAD `ba199c72dd18e7d5e5e2f6e7c01a7c1253fb5bed`，13个修改路径和6个未跟踪生产路径全部保留。当轮只维护CHANGELOG、PROJECT_HISTORY、本Reference；Probe、模型、隔离venv、下载缓存、输入和结果均进入gitignored `artifacts/background-v03-p0/`。未修改生产Background Remover、EdgeRefinement、UI、Host/Core/SDK、契约、依赖、模型或治理，未进行ONNX export、Batch功能或Git写操作。Owner关闭Host后才开始该轮推理。

| Candidate | 来源与固定revision | SHA256 | 实际模型bytes / license |
| --- | --- | --- | --- |
| A 当前Lite control | `CoderViking/birefnet-lite-onnx` / `dc06453148f01ef4131f17e9b791345e32e8ee78` | `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67` | 199681624 / MIT；复用生产文件，未重下载 |
| B Matting ONNX | [emrikol/birefnet-matting-onnx](https://huggingface.co/emrikol/birefnet-matting-onnx/tree/0d58d809b3a360b44c556223d2f5812aeace9ba3) / `0d58d809b3a360b44c556223d2f5812aeace9ba3` | `f0843e38f6a4e88efc8c5fad4178ad7ed6c818346ce12f82e7b579324fe7e0c5` | 940840787 / exporter声明MIT并归属官方BiRefNet-matting |
| C 官方HR-matting | [ZhengPeng7/BiRefNet_HR-matting](https://huggingface.co/ZhengPeng7/BiRefNet_HR-matting/tree/5d6b6f8adcb5b417c871b1d84ceaae9871355b7f) / `5d6b6f8adcb5b417c871b1d84ceaae9871355b7f` | `a5a4de698739ea5e0e8bbab28e1b293dde95092b87a442d566cbc585c53cef55` | 444473596 / 官方MIT；687个F16 tensor、67个I64 tensor |

B模型卡没有披露准确的上游训练权重revision，其仓库也没有单独LICENSE文件；本轮核实其MIT声明与归属，并保存GitHub `ZhengPeng7/BiRefNet` revision `ebcc0bc8ec7fe919cec829f2dea656b3078acddc` 的MIT文本。不把这些记录等同于独立复现export数值等价性。实际图为IR8、opset16、PyTorch2.12.0导出、32453节点、300个GridSample，无外部tensor文件；输入输出均FLOAT，分别`[1,3,1024,1024]`与`[1,1,1024,1024]`，输出端为Conv logits。

D [RMBG-2.0](https://huggingface.co/briaai/RMBG-2.0)官方访问需要账号/接受条款，按允许分支跳过；模型卡license字段为`bria-rmbg-2.0`并说明非商用开放，不作为可自由商用生产候选。没有上传图片、准备D集成或重跑BEN2。元数据、固定README、源代码与SHA证据见本地`sources/`。

### 20.2 固定困难集与方法

8个真实case id：`subject-completeness-hard`、`animal-fur`、`hair-fine`、`backlight-hair`、`portrait-long-hair`、`thin-structure`、`transparent-hard-case`、`product-hard-edge`。木箱是460×460 Owner local regression sample，SHA256 `d8def6b117e980b167ffe03b201e5cde6d39ebfc57e45865d4c382e9917754c4`；原图不进入Git。其余7张复用P1固定测试集，来源、许可、尺寸和逐图SHA在`hard-set.json`。

A/B使用完全相同的保存输入tensor：编码RGB、PIL抗锯齿bilinear到1024、ImageNet normalization、NCHW FP32。使用现有ORT1.24.4 / DirectML device0、Sequential / ALL、GPU MemoryPattern关闭、默认CPU Arena；没有新增生产依赖。输出stable sigmoid、bilinear回原尺寸、乘原Alpha。A另调用只读链接的当前EdgeRefinement，`results/A-raw/`保存其前置对照；B是原RGB与原始Matting Alpha。不同后处理约定明确标注，未为候选另调参数。

C实际执行其官方模型卡2048×2048 FP16流程；官方的ToPILImage量化再resize与A/B顺序不同，不静默改成同一流程。`bb_pretrained=false`、本地safetensors、离线缓存，官方代码不修改；不执行会引用另一模型与前景颜色估计器的handler。使用仓库内venv的PyTorch2.5.1+cu121 / torchvision0.20.1+cu121、timm1.0.12、transformers4.46.3、kornia0.7.4，完整版本见`sources/C-env-freeze.txt`，pip check通过；没有向系统Python安装包。

### 20.3 A/B质量发现与性能门

以下为代理对实际输出的定性检查，不是Owner评分或验收：

| 样本 | 实际发现 |
| --- | --- |
| 木箱 | A在底部箱体左侧顶面挖洞；B恢复该区域。所选`(94,261)-(111,281)`不透明区域A/A-raw平均Alpha 0.49255、45.882%低于0.5；B平均1.0、0%低于0.5。各正面/扣件及右顶面保留。A/B顶部均有背景纸张残留，B更宽。ROI不是完整GT mask。 |
| 猫毛 / 卷发 | B部分胡须/卷发分离不同，仍有发雾和灰/绿原背景污染；不能宣布全部毛发质量通过。 |
| 逆光 | B减少手臂与躯干间亮背景；1:1放大时，A/B均大量损失飞发，B平滑亮边并未形成明确发丝/halo改善。 |
| 细结构 | 主车架保留；B一些前轮辐条更弱，两者都不能保证细线完整。 |
| 透明 | 都保留玻璃球与部分倒影；Alpha连续不等于物理透明正确或折射背景已移除。没有GT，不作透明质量通过结论。 |
| 长发 / 相机 | 主体整体近似；剩余柔边、污染和小结构需要Owner逐图确认。 |

初看整图时准备了B GPU/CPU benchmark job；随后1:1检查否定逆光质量门，**两个job未执行**。`B-quality-gate-final.json`取代整图阶段的暂定判断。只报告8图质量运行实测，不将其描述为10-warm稳定性测试或CPU后备验证。

| Candidate | 实际质量Run | Init | Cold | 7 warm median | Peak WS | Private Bytes | DXGI local |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A | 8/8完成 | 4.3598s | 0.4983s | 0.3508s | 1.305GB | 7.214GB | 5.962GB |
| B | 8/8完成 | 8.1946s | 21.8827s | 20.1342s | 9.351GB | 13.974GB | 11.513GB |

均为模型Run，不含解码、全尺寸后处理、PNG或UI；GB为十进制。本机RTX4060 Laptop 8188MiB / driver610.88，concurrency1。DXGI local是进程记账，不是11.5GB全部物理驻留；B运行预算约6.4–6.9GB，存在资源压力，但未用驻留trace证明唯一根因，也未逐节点确认provider placement。没有后端调参、精度转换或自动fallback。B明显超1.5s目标，未追加GPU 1+10 / CPU 1+3；CPU后备为INCONCLUSIVE，未虚构15s超时。

### 20.4 C状态、体积与后续边界

C完成8/8张质量图，恢复所选木箱左侧顶面，ROI平均Alpha=1.0、低于0.5比例为0%；有更多猫耳细毛、卷发分离、逆光短飞发及前轮辐条。长飞发和halo仍缺失/残留，顶部背景纸张与车下细残留尚在。玻璃内部原黑背景更多变为透明，但淡边和倒影不等于物理透明已正确。以上为代理观察，Owner分数仍未填写。

这些木箱/毛发/逆光观察支持继续小型C性能测量：

| C PyTorch GPU | Load | Cold Run | Warm median | Peak WS | Private | CUDA allocated / reserved peak |
| --- | --- | --- | --- | --- | --- | --- |
| 首次8张质量集 | 4.3248s | 7.2704s | 2.9826s（7 warm） | 6.211GB | 14.390GB | 5.450 / 10.819GB |
| 新进程1 cold + 5 warm | 1.3241s | 3.7623s | 2.9486s（5 warm） | 5.939GB | 14.389GB | 5.450 / 10.819GB |

两组均完成、结果finite。Cold为新进程/模型首Run，不是重启或清空OS/驱动缓存；reserved为PyTorch allocator缓存计数，不是10.819GB全部驻留于8GB显卡，亦不与DXGI local直接等同。没有改allocator/驱动策略。此为官方PyTorch/CUDA速度，**未来ONNX速度、资源、体积与CPU后备均未验证**。

现有包19文件236719917 bytes；仅将199681624-byte A替为940840787-byte B，文件和将为977879080 bytes，约977.88MB / 1GB级，增加741.16MB，约现有4.13倍。未实际制作该包，不预测压缩率。C权重本身已经FP16，444.47MB并不代表未来ONNX或最终包体积。

两阶段只分析：完整图像是当前官方/固定模型的输入约定；局部crop能否保持语义需实验，可能损失尺度和上下文。把crop仍放大到固定1024并不会自动降低B单次图计算，多crop还会增加调用。C代码明确融合全尺寸/半尺寸backbone与decoder输入图信息。uncertain edge band可能遗漏被确信判为背景的真实主体，不能预先保证恢复木箱缺失；没有“两阶段更快”结论或实现。

原研究A在新增主体完整性门下为REJECTED（仅此研究门）；B作为当前硬件默认替换为REJECTED，木箱恢复是PROMISING证据；C为PROMISING候选，生产集成未获批准；D商用生产资格为REJECTED，非质量评分。Owner最终接受Lite现阶段限制并冻结V0.2.1，保留A作为生产模型（即P1中的B Static）。原建议的C单模型ONNX可行性方向正式Deferred，不启动export或两阶段实验。**Future research不等于current production plan。**

### 20.5 验收材料与验证

本地完整报告：`artifacts/background-v03-p0/REPORT.md`；联系表入口`comparisons/index.html`，逐图`<case>-black/dark/white.jpg`；透明PNG为`results/<candidate>/<case>.png`，均原始分辨率，另有全尺寸背景合成与`comparisons/details/`像素放大图。`owner-review-template.csv`六个质量字段全部PENDING OWNER REVIEW。Owner所述Adobe/remove.bg效果未提供结果文件，本轮未独立复测。

实际命令：

```powershell
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v03-p0\host-build
dotnet build .\artifacts\background-v03-p0\probe\ModelProbe.csproj -c Release --artifacts-path .\artifacts\background-v03-p0\build
& .\artifacts\background-v03-p0\build\bin\ModelProbe\release\ModelProbe.exe .\artifacts\background-v03-p0\A-quality.json
& .\artifacts\background-v03-p0\build\bin\ModelProbe\release\ModelProbe.exe .\artifacts\background-v03-p0\B-quality.json
& .\artifacts\background-v03-p0\venv\Scripts\python.exe .\artifacts\background-v03-p0\c_infer.py quality
& .\artifacts\background-v03-p0\venv\Scripts\python.exe .\artifacts\background-v03-p0\c_infer.py benchmark
& .\artifacts\background-v03-p0\venv\Scripts\python.exe -m pip check
& .\artifacts\background-v03-p0\venv\Scripts\python.exe .\artifacts\background-v03-p0\validate_outputs.py
git diff --check
git status --short
```

Solution与研究Probe build均0 warnings / 0 errors。Solution是5个Host项目，不含外部Module，不冒充本轮Module build或UI测试。32个PNG（A/A-raw/B/C各8）全部通过输入SHA、尺寸/RGBA、有效Alpha、原RGB与既有非零Alpha集合检查；有24张整图联系表、27张局部图、24行Owner模板，144个评分均PENDING OWNER REVIEW。起点50个仓库文件仅上述3份研究文档改变，生产源码与正式模型SHA未变，非ignored路径集合未变；研究Python/Probe进程均退出，Host未启动，暂存区为空。证据为本地`output-validation.json`、`final-state.json`与`git-status-short.txt`。没有全仓库dotnet test或人工验收通过结论。研究代码与依赖仅在ignored artifacts，不提交或推送。

## 21. V0.2.1 Frozen Production Baseline

### 21.1 Owner最终决定与范围

2026-09-03：**FROZEN / ACCEPTED WITH KNOWN LIMITATIONS**。来源是Owner本次Finalization请求：当前速度可接受，Batch缩略图符合预期，EdgeRefinement相比之前的白边/灰雾/部分污染有改善，开销低，接受已知质量限制并结束当前阶段研究。不是逐图满分、完美抠图或专业服务等价认证。

起点main / HEAD与live origin main均为`ba199c72dd18e7d5e5e2f6e7c01a7c1253fb5bed`，13 modified + 6 untracked，合计19个授权Git eligible文件。冻结收口只进一步同步5份文档（CHANGELOG、根README、PROJECT_HISTORY、本Reference、模组README），不调整生产实现。待归档的完整生产里程碑共19文件（12生产/构建文件、7文档），使用Owner指定的单个`feat: enhance background remover v0.2.1`提交，不预填自身Hash；不创建Tag或GitHub Release。起点逐文件SHA记录在`artifacts/background-v021-final/start-state.json`。

**SDK / Host / Core / MainWindow / World Canvas / Module System / 治理修改均为NO**。既有架构文档的旧实施状态快照不在本轮改写；现有模块内业务职责与公共契约不变。研究代码、图片、模型、日志和构建产物不进入Commit。

### 21.2 冻结能力

| 能力 | 最终状态与证据边界 |
| --- | --- |
| Model | **KEEP MODEL / KEEP STATIC EXPORT**；P1候选B Static BiRefNet Lite即V0.3研究control A。唯一生产模型为CoderViking/birefnet-lite-onnx，revision `dc06453148f01ef4131f17e9b791345e32e8ee78`，199681624 bytes，SHA256 `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`。 |
| Single | PNG/JPEG/BMP选择/拖放、原图与结果预览；点击“保存PNG”直接写桌面，重名安全编号。单张不在推理结束后自行保存；自动逐项保存属于Batch。 |
| Batch | 多选、多个文件/递归文件夹拖入，等待/处理中/成功/失败，当前项高亮与主预览跟随、点击缩略图导航；一次一图顺序执行，桌面时间戳目录、错误隔离、失败日志、Stop不启动后续项。 |
| Image lifetime | 不全量预解码、不保留全批结果、不并发Inference；可见项160px缩略图、1280px点击预览、32项缓存淘汰阈值，切换/结束/卸载按现有逻辑释放缓存。 |
| Backend | Auto / GPU / CPU，DML device0，concurrency=1；lazy initialization、Session reuse、空闲切换释放Session。Auto故障明确提示并一次CPU恢复，强制GPU后端失败停止；故障分支仍为静态检查，未安全注入设备失败。 |
| Performance | 默认OFF且无采样Timer；ON每1秒采样CPU/Working Set，显示当前耗时、平均吞吐和批次进度。没有扩展面板或新增控件。 |
| EdgeRefinement | 默认始终启用，Single/Batch共用Engine结果，预览和PNG均受益。Alpha最大调整量、阈值、半径、RGB规则和保护逻辑全部保持第19节实现，未调参。 |

### 21.3 既有测量保留，不重跑Benchmark

| 既有验证 | 已验证数据 | 来源 |
| --- | --- | --- |
| V0.2生产Engine GPU | warm Run median **289.281ms**，10/10 warm成功（约0.3秒级） | 第17.3节、`background-v02-production/gpu-production/results.jsonl` |
| V0.2生产Engine CPU | warm Run median **3892.419ms**，5/5 warm成功（约4秒级） | 第17.3节、`cpu-production/results.jsonl` |
| V0.2.1最终边缘实现 | GPU/CPU warm Run **424.540 / 4239.066ms**，Engine Total **673.115 / 4489.758ms** | 第19.4节、`background-v021-edge/clean-after-gpu`与`clean-after-cpu` |
| Edge增量 | 配对Postprocess median增加 **13.296ms GPU / 6.030ms CPU**；这是整体后处理差值，不是单独Edge方法绝对耗时 | 第19.4节，前后完整数据保留 |
| Batch | GPU **100/100**、119.672秒；CPU **16/16**、81.858秒；混合坏图3失败/3成功；停止后17项不启动 | 第17.5节 |
| 调度与缩略图 | 1000项调度/命名为0推理；V0.2.1 1000项UI投影为4容器/13缓存、结束4缓存、切单张0缓存；8正常+1坏图状态与预览检查通过 | 第17.5、18.4、19.5节 |
| 面板ON/OFF | 同30项GPU OFF39.534955s / ON39.555293s，吞吐差 **-0.0514%**；单组无明显开销，不代表零开销 | 第17.6节 |

Run时间不含解码/PNG保存/UI；Engine Total也不含文件解码/保存/UI。不同轮次的环境与图像影响数值，不能将0.3秒解释为所有图片的端到端保证。本轮只读核对已有证据，不重复100张、1000张推理或新模型测试。

### 21.4 已知质量限制与研究关闭

V0.2.1不是Adobe / remove.bg级专业抠图方案：发丝/动物绒毛可能保留灰雾、色边、背景污染；逆光亮边/halo/飞发有限；透明或半透明物体不能保证物理正确Alpha；极细杆线、辐条、网状结构可能局部损失。

最重要的是主体完整性：当商品与背景接近、结构复杂或语义不明确时，Lite可能将真实主体判为背景。三个木箱的左顶面ROI，A-raw与A平均Alpha同为0.49255、45.882%低于0.5，说明缺失在模型原始Alpha里已存在；这是 **MODEL CAPABILITY LIMITATION**，不是EdgeRefinement Bug。后处理无法恢复已经被模型判为背景的主体。Owner报告Adobe相对完整，remove.bg也有轻微缺失但总体更好；未提供这两者结果文件，未独立复测服务或编造对比评分。

V0.3-P0完整证据保留，第20节正式 **DEFERRED / FUTURE QUALITY RESEARCH**：A Lite性能最佳但木箱完整性失败；B Matting ONNX约20.13秒、940840787 bytes及资源代价不适合当前生产；C HR-Matting有木箱/毛发/细结构改善迹象，PROMISING，PyTorch/CUDA warm2.9486秒、444473596-byte权重，生产ONNX未验证。未来研究不等于当前生产计划，不继续下载/测试模型、C ONNX Export、V0.3-P1或两阶段方案。

addToolBox是通用模块化工具箱。第一个Module已完成Module System、独立Package、本地模型加载、GPU/CPU、Batch、错误隔离、性能观测、固定Test Set、开发Reference和真实Owner验收的工程目标；无限追逐局部质量差异会阻塞主项目。因此 **V0.2.1 Freeze / 当前阶段CLOSED**，项目重点回到Module生态。将来若另行授权，质量工作另开`Background Remover V0.3 Quality`。本轮未开始Performance Monitor Module V0.1。

### 21.5 最终Build、Package和最小Smoke

Host关闭，全部使用仓库内独立输出目录；关键命令实际执行如下：

```powershell
dotnet build .\AddToolBox.sln --artifacts-path .\artifacts\background-v021-final\host-build
dotnet build .\modules\AddToolBox.BackgroundRemover\AddToolBox.BackgroundRemover.csproj -c Release --artifacts-path .\artifacts\background-v021-final\module-build
.\modules\AddToolBox.BackgroundRemover\tools\package.ps1 -BuildRoot .\artifacts\background-v021-final\module-build -OutputDirectory .\artifacts\background-v021-final\package-0.2.1
.\artifacts\background-v021-final\verify-package.ps1
dotnet build .\artifacts\background-v021-final\smoke\FinalSmoke.csproj -c Release --artifacts-path .\artifacts\background-v021-final\smoke-build
& .\artifacts\background-v021-final\smoke-build\bin\FinalSmoke\release\FinalSmoke.exe (Get-Location).Path
git diff --check
```

Host solution（5项目）、Module Release、package脚本内部Release及临时smoke build全部 **0 warnings / 0 errors**。没有全仓库`dotnet test`、新增测试框架、系统Python/CUDA环境或依赖升级；已有依赖正常restore，不下载模型。Git可能提示LF→CRLF规范化，这与编译warning分开。

最终包：`artifacts/background-v021-final/package-0.2.1/`，**19文件 / 236721555 bytes（236.721555MB）**。逐文件路径、数量、大小与SHA256对照Release输出全部一致；`package-verification.json`保留完整清单。唯一模型为上述199681624-byte B且SHA一致，manifest version=**0.2.1**。Managed ORT assembly **1.24.4.0**，native ORT **1.24.20260316.9.2d92497**，DirectML **1.15.4+241025-1615.1.dml-1.15.fac7597**。Module DLL为75264 bytes、SHA256 `096089b0ed1ba31359ae5c69bc6cf1951c266e86541d474814e514103bd20d70`。产品版本权威是manifest，未更改原有程序集默认版本策略。

Package无旧A、BEN2、Matting、HR-Matting、临时FP16模型、Python、测试/私人图片、日志、PDB、LIB或私有SDK副本。旧研究与旧验收包完整保留，不清理、不绕过此前删除策略限制；它们在ignored artifacts，不影响Package或Git eligible状态。

最小smoke使用**此次Host构建的真实ModuleManifest.Read / LoadedModule.Load / GetOrCreateView**从最终包加载0.2.1，验证入口契约、View缓存、默认Auto、lazy Session/输入数组、性能OFF无Timer。复用未修改的既有EdgeTests：4种白/灰/彩/暗混色、透明输入保护、Alpha支撑集合和4档孤立细线均通过。探针初次在推理前因路径分隔符字符串比较误判失败；规范化路径后原断言通过，失败日志保留，未改生产实现。

普通`portrait-normal`（SHA与固定测试集一致，2050×3084）强制GPU一次、CPU一次，**共2次推理，2/2生成PNG成功**。模型Run **498.542ms GPU / 4513.509ms CPU**，均为本次后端首次运行，不能称warm benchmark。保存/重读PNG逐像素一致，原尺寸、RGBA、透明/半透明/不透明区域及Alpha=0时RGB=0通过；两文件约8.28MB，保存在`smoke-results-verified/`。切换/Dispose释放Session通过，没有Auto故障切换实测。

本轮构造了真实View但没有启动Host窗口，也未模拟鼠标或重复人工UI验收；桌面保存、Batch与缩略图交互沿用上表已有自动证据及Owner最终验收。低显存/其他硬件、设备故障恢复、磁盘耗尽与长期批次仍未覆盖。后续任务不得将这两次smoke扩写为整套Benchmark或质量研究重跑。
