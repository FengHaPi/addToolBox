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

### 2026-09-03 — Background Remover V0.2.1 Frozen Baseline

状态：**FROZEN / ACCEPTED WITH KNOWN LIMITATIONS / Owner-confirmed product decision**。Owner正式接受V0.2.1作为当前生产基线：速度可接受、Batch缩略图符合预期、EdgeRefinement对白边/灰雾/部分色污染有改善且开销低，接受当前边缘和主体完整性限制。此次收口覆盖自`ba199c7`后同一未提交生产里程碑，按授权以单个`feat: enhance background remover v0.2.1`提交；不预填自身Hash，不创建Tag或GitHub Release。以下各阶段的“未提交/等待验收”保留当时快照，当前验收状态以本节为准。

产品决定 **KEEP MODEL / KEEP STATIC EXPORT**：唯一模型仍为CoderViking BiRefNet Lite，revision `dc06453148f01ef4131f17e9b791345e32e8ee78`，199681624 bytes，SHA256 `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`。Auto/GPU/CPU、DirectML、Batch、缩略图、默认OFF性能面板及默认EdgeRefinement全部保留；后处理参数与保护逻辑冻结，Host/SDK/Core不变。

当前不是Adobe / remove.bg级专业抠图方案。毛发灰雾/色边、逆光halo/飞发、透明Alpha、极细结构以及复杂商品主体误删仍有限制。木箱左顶面缺失在原始模型Alpha中已存在，属于 **MODEL CAPABILITY LIMITATION**，不是EdgeRefinement Bug；后处理不能恢复模型判为背景的主体。Owner观察Adobe更完整、remove.bg轻微缺失但总体更好；没有将此观察写成本地独立服务复测。

V0.3-P0研究与原始证据完整保留，但正式 **DEFERRED / FUTURE QUALITY RESEARCH**。A Lite速度最佳但木箱完整性失败；B Matting约20秒、约941MB及资源代价不可接受；C HR-Matting为PROMISING、PyTorch/CUDA warm约2.95秒、约444MB，尚无生产ONNX验证。这些发现不触发C导出、V0.3-P1或下一次自动研究。

停止原因：addToolBox是通用模块化工具箱，首个Module已完成Module System、独立Package、本地模型、GPU/CPU、Batch、Error isolation、Performance instrumentation、Test Set、开发Reference和真实人工验收的验证。继续无限追逐局部质量差异会阻塞主项目。当前去背景阶段 **CLOSED**，项目重点回到 **addToolBox Module ecosystem**；未来若重启质量开发，另开`Background Remover V0.3 Quality`。预定Performance Monitor Module V0.1本轮未开始。最终构建、Package和smoke见[冻结Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#21-v021-frozen-production-baseline)。

### 2026-09-03 — Background Remover V0.3-P0 主体完整性研究

状态：**DEFERRED / FUTURE QUALITY RESEARCH**。以下保留原研究事实与未填写的Owner评分；当前产品决定见上方Frozen Baseline。Owner提供三个木箱商品图，报告当前Lite误删主体，研究时要求优先级改为主体完整性、matte/毛发、细结构、稳定性，再考虑速度和体积。此发现不推翻P1当时固定样本的定性替换验收，也不把后续发现倒写为此前已知；研究没有修改生产代码、EdgeRefinement、依赖、模型、Host或SDK。

固定8张Hard Set复用当前Lite为A，验证941 MB FP32 Matting ONNX为B。A的木箱左侧顶面缺口在原始模型Alpha中已经存在，既有边缘处理未恢复；所选不透明区域45.882%像素Alpha低于0.5，B降至0%，三块正面和金属扣均保留。B同时保留较多顶部背景纸张，逆光1:1发丝仍缺失，部分自行车辐条更弱，不能以单个木箱改善宣布通用质量通过。A/B各8次质量推理完成；B GPU warm median约20.134秒、WS 9.351 GB / Private 13.974 GB / DXGI local 11.513 GB，后者不是物理显存驻留量。放大检查未满足木箱、毛发、逆光全部质量门槛，因此不追加B GPU/CPU benchmark，CPU后备仍未验证。

C官方HR-matting固定revision与444.47 MB FP16 safetensors已核验，在仓库内隔离PyTorch2.5.1/CUDA12.1环境完成8图质量推理。木箱缺口恢复，猫耳细毛、部分逆光短飞发及前轮辐条比B更完整；长飞发、灰雾、纸张/细背景残留和透明语义仍未解决。小型1 cold + 5 warm测试6/6，warm median 2.9486秒，WS峰值5.939 GB、Private 14.389 GB、CUDA allocated 5.450 GB / reserved 10.819 GB；缓存计数不等于物理驻留，也不等于未来ONNX速度。C为PROMISING研究候选，原研究建议的ONNX可行性方向现已Deferred，不进入当前生产计划。

D因官方权重需要账号/接受条款而跳过，并非可自由商用候选；BEN2未重跑。所有主观评分留为PENDING OWNER REVIEW，原图、透明PNG、黑/灰/白对照、放大图、日志及Owner模板均只保留在ignored artifacts。两阶段Lite→uncertain edge→Matting仅作可行性分析：固定分辨率裁剪不自动减少单次模型计算，也可能遗漏置信度很高的错误背景区域，因此不作为优先下一步。细节和最终验证见[本轮Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#20-v03-p0-quality-and-subject-completeness)。

### 2026-09-03 — Background Remover V0.2.1 边缘质量继续修正

状态：**Implemented / Uncommitted / 等待Owner人工质量验收**。Owner确认多图缩略图基本可用，本轮优先质量；前一版只允许中性亮背景RGB修正且不改Alpha，覆盖不了灰/色/暗背景污染。单图与Batch已经共用Engine，因此无需修改Host契约或重写Batch。实际仅调整Engine并新增模组内EdgeRefinement，替换旧RGB-only方法；没有增加用户质量档位、模型、依赖或第二次推理。

在有近不透明前景、近透明背景和颜色混合一致性证据的Alpha 5–249带内，轻微降低被颜色证据判为偏高的Alpha，最多6/255；局部Alpha峰值保留，所有非零Alpha像素不被清除。不透明主体及输入已有非零透明度的像素保持不变。RGB沿局部背景到前景方向校正，每通道最多48/255，采样结束后将Alpha=0的RGB清零。使用单个池化Alpha快照保持邻域读取稳定，复杂度随像素数线性、搜索半径有上限。

九类现有样本的黑/白背景及全尺寸PNG前后对照中，项链、细结构、部分长发轮廓改善较易观察，普通人像/相机局部小幅改善；绒毛、复杂卷发改善有限，逆光及透明困难仍存在。没有主观高分或Owner通过结论。真实配对测量GPU 1 cold + 10 warm、CPU 1 cold + 5 warm：Engine warm总耗时中位数0.658→0.673秒和4.559→4.490秒；后处理单独计时有小幅增加。先前验收窗口保留Session导致GPU预算竞争的诊断组被明确排除，释放旧窗口后重新配对；不把资源竞争数据当回归。详细样本、数值和验证/清理证据见[本轮Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#19-v021-edge-quality-refinement)。安装目录保留0.2.0，本轮交付仓库内验收包，不提交或推送。

### 2026-09-03 — Background Remover V0.2.1 批量预览与边缘修正

状态：**Implemented / Uncommitted / 自动检查通过，等待Owner人工验收**。所有者反馈速度基本可接受，但多图选择后原图区域为空、深色背景下部分边缘仍发白。按本轮授权仅修改5个模组生产文件：View XAML/代码、Engine、manifest和package脚本；不更换模型/依赖，不改Host/SDK/Core/MainWindow/World Canvas或性能面板，也不重写Batch。

空白来自V0.2多图分支主动清空OriginalPreview且没有条目视图。新增原图区域内的横向虚拟缩略图条，当前项高亮、逐项状态、点击预览；160px可见项缩略图和1280px点击预览均有界加载。压力检查发现WPF容器回收后按Loaded/Unloaded维护的可见集合会残留，改以当前生成容器和视口作为依据；最终1000项仅4个容器、13张缓存、0推理，批次结束仅4张缓存，切到单张后清空。

白边诊断区分了合成与像素：WPF 256档Alpha深色合成误差最大1/255，没有发现预乘错误；现有导出PNG已含亮色半透明边缘。原流程仅预测Alpha并保留原RGB，不能自动移除RGB里混入的原背景色。按本轮新增授权，在半透明边缘有邻近不透明前景、近透明浅色背景和颜色混合一致性证据时保守减色，RGB每通道最多32/255，Alpha不变；预览和保存共用同一结果。六图Alpha/不透明区域均未改变，短发人像部分亮边减轻；并非完整matting或保证所有发丝/耳饰无色差。原有透明输入像素受保护，宽雾状残留仍可能存在。

实际自动处理9次：单张1次并保存，8张批处理成功加1张坏图失败，高亮/状态/主预览一致；单张重名保存不覆盖。原始证据与限制见 [V0.2.1 Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#18-v021-batch-preview-and-edge-correction)。本轮通过仓库内验收程序调用现有Host加载器加载0.2.1，未覆盖用户安装目录；V0.2安装与备份保留。没有人工验收结论或Git提交/推送。

### 2026-09-03 — Background Remover V0.2 Production Integration

状态：**Implemented / Uncommitted / 等待所有者人工 UI 验收；不是 Release**。先以 `ba199c7` 独立封存并推送 P1，确认干净断点，再按所有者明确批准的范围修改去背景模组。Host、SDK、Core、UI、Infrastructure、World Canvas、MainWindow 和治理文件均未修改；模型、测试输入和原始日志仍在忽略目录。

采用已通过定性替换验收的 B Static BiRefNet FP32 导出，固定 revision `dc06453148f01ef4131f17e9b791345e32e8ee78`、SHA256 `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`、199681624 bytes。选择单一 DirectML 1.24.4 发行包同时提供 CPU / DML EP，配套 Managed 1.24.4 和 DirectML 1.15.4，避免两个 native ORT 版本并存。新运行时 CPU warm median 3.892秒，未达3.5秒理想目标但未触发4秒停止线；GPU warm median 0.289秒，10/10 warm成功。资源口径与前置 Runtime Gate 分开记录于 [V0.2 Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#17-v02-production-integration)。

质量回归确认输入约定也是替换的一部分：P1 使用抗锯齿 bilinear 和未做 ICC 转换的编码 RGB；WPF 默认色彩转换会改变普通人物和商品输入。通过只读像素/张量诊断确认原因后，模组使用同样的 resize 与 IgnoreColorProfile，六类输入张量逐位一致，生产 CPU 与 P1 B PNG 的 RGB 相同、Alpha 最大差1/255。没有加入阈值、羽化、边缘清理或新图像库，也没有重新研究 A/BEN2；这项数值回归不代替 V0.2 产品人工验收。

新增单 worker 顺序批处理、路径去重/排序、逐项自动保存与错误隔离；一次只解码/处理当前图片，预览更新被 await，不积压 Bitmap。Auto 在 GPU 后端失败时释放旧 Session、明确提示、当前项 CPU 重试一次并保持 CPU；强制 GPU 后端失败停止批次。右上仅新增设备选择和性能按钮；OFF 不创建采样定时器，ON 每秒采样。单张与批量使用桌面固定输出和不覆盖命名。100张真实GPU批次100/100、119.672秒、peak WS2.396GB；CPU16/16、81.858秒；坏图3失败/3成功；停止后17项未启动，随后2项重启成功。1000项压力只做调度/路径/命名，明确0次推理。

V1 导入不支持重复 ID 更新；按本轮明确授权关闭 Host，保留逐文件 SHA 验证的 V0.1 备份，再将完整 V0.2 Package 做 development-only 本地部署，未修改导入规则。19文件共236705882 bytes，仅B模型、单一ORT与必要DML依赖；包与安装副本逐项一致。最终构建、性能面板对照、自动探针与证据目录见 Reference。设备故障没有安全注入接口，按任务允许条件跳过故障模拟；不把正常GPU/显式CPU通过写成fallback故障路径已验证。低显存设备与更长时间运行尚未验证。V0.2保持未提交、未推送，启动空闲Host交由所有者执行30项人工检查；Fast Path和独立Performance Monitor Module仍未开始。

### 2026-09-03 — Background Remover V0.2-P1 模型与导出比较

状态：**Research / Owner visual acceptance PASSED（2026-09-03）；收口已独立提交并推送**。P1 提交为 `ba199c72dd18e7d5e5e2f6e7c01a7c1253fb5bed docs: establish background removal model benchmark`，Commit Date **2026-09-03 05:42:58 +08:00**。只包含获批的 8 个文档/测试集定义文件；提交后 HEAD、origin/main 与远端 main 一致、工作树干净，再进入 V0.2。P0 三份文档此前独立提交推送为 `635bb60`。P1 研究使用隔离 artifacts，不包含后续生产 Module、模型、Host、SDK、依赖或安装包变更。

方向性证据：A/B 都是静态外部输入 `[1,3,1024,1024]`，并非简单把动态输入固定。B 的 GridSample 导出将节点数 16400→6129，GatherND/ScatterND 80/72→0/0；同一 CPU 1.29.0、相同生产预处理合成输入下，peak WS 12.566→3.473 GB，warm median 5.956→2.621秒。B DML 在 RTX4060 Laptop 上 10/10 成功，warm median 0.323秒，peak WS 1.302 GB / Private 7.214 GB / DXGI local 5.962 GB。这支持导出及执行结构对资源代价有重要影响，不证明某一算子是唯一根因，不把低工作集称作泄漏已修复；GPU资源仍非“只有1.3GB”。

正确性门槛未被内存优势替代：A/B 合成 alpha MAE 0.0001015、max 0.1286；16 张真实图仍有局部明显数值差异，最大局部差可达0.9568。生成对比图、原尺寸深灰/白底 Halo 预览和四项空白评分表；没有伪造人工质量评分。真实图 A/B 采用相同的上游 PIL 推荐预处理，独立于用于归因的生产预处理合成 Probe，不声称完全复现生产 UI 的 resize 数值。

官方 BEN2_Base.onnx 输入 FP32、输出 FP16，按其 ONNX 脚本的 /255、min-max 与特殊宽高传参后处理做诊断；未套用 BiRefNet sigmoid。CPU 首图60.093秒未完成即终止；DML10/10、warm0.555秒，但有部分节点未分配首选EP的两行运行时警告，且四个重复输入的原始输出存在小幅不一致。可靠CPU后备门槛未过；受CPU停止与DML10次上限约束，只产生6张不同图片结果，另10张未运行。不为补齐表格重开Session继续测，不将BEN2宣称为全套质量更优。NumPy后处理遵循官方流程，但PyTorch逐bit一致性未验证。

标准集包含16张公开自由许可图（15张照片、1张插画），覆盖16类；来源、作者、许可、实际SHA256和尺寸见 [testset](../dev-assets/background-removal-testset/README.md)。3张因源站明确要求改用官方1920px版本，已如实标注；固定性能集保留24MP原图，发丝另有36.15MP原图。自动核实38张透明PNG、76张背景合成预览和32张横向对比；这些检查不代替语义质量验收。

所有者于2026-09-03明确提供 qualitative owner acceptance：普通人物、发丝、商品硬边通过；逆光发丝、transparent hard case、thin structure 未见相对 A 的明显新增退化。**B = Production Approved Candidate；KEEP MODEL / CHANGE EXPORT。** 没有逐项 1–5 数值评分；这只是 B 相对 A 的替换验收，不承诺所有透明物体、发丝、细结构完美，不代表 V0.2 生产集成/UI 已实现或验收。BEN2保留研究价值但不替换、不补测缺失案例；未触发备用InSPyReNet。Simple-background Fast Path仍为 Future Candidate。详细方法、固定SHA、资源口径和命令见 [P1 Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#16-v02-p1-model--onnx-export-comparison)。P1 收口先独立 build/commit/push，再开始 V0.2 Runtime Gate；生产改动须另待人工验收，不能并入 P1 提交。

### 2026-09-03 — Background Remover V0.2-P0 资源归因

状态：**Research；记录已提交，V0.2 生产准入未通过**。所有者在前一轮 backend gate 停止后单独批准 P0；从相同 HEAD / origin/main=`88efae8` 和三个预期研究文档修改继续，保留旧记录，不要求或制造干净工作树。只运行被忽略的 artifacts 探针并维护三份既有文档。

P1 开始前按所有者独立授权封存 P0：`635bb60361de9be7ca014dea516e8f006b0b275e docs: record background remover backend investigation`，Commit Date **2026-09-03 04:49:35 +08:00**。仅三份研究文档，已推送 GitHub；推送后 HEAD / origin/main / 远端 main 相同，工作树干净。这次提交不授权 P1 提交或生产改动。下述“本轮”仍指原 P0 调查。

CPU FP32 的 4×6-run 矩阵确认资源代价不仅由模型文件大小决定：默认 peak WS=12.49 GB / warm median=5.318秒；关 Memory Pattern 为7.66 GB，关 Arena 为6.50 GB，两者均关仍有6.27 GB瞬时峰值。Dispose后各组约120 MB；与前轮20-run平台期共同支持 Session关联native分配/保留是主要方向，但无heap trace，未证明长期不存在泄漏。产品判断为 **Functionally Stable but Resource Heavy**，不宜作为低占用通用默认。

FP32 DML 在同一 RTX4060 上再次第二次失败，本次HRESULT为0x887A0005而非前轮0x8007000E；超出DXGI预算是资源压力证据，不把它写成已定位的唯一根因。新授权的公开FP16固定SHA诊断中，CPU首张在进程存活196.738秒后仍未完成，按性能无意义分支终止；DML FP16则10/10成功、warm median约2.066秒，peak WS约6.57 GB、Private约12.50 GB，DXGI local记账11.85 GB仍超预算。后者是 **PROMISING 的研究结果，不是V0.2候选已获准入**；仅合成图数值检查，真实人像/商品/毛发质量未验收。

本轮建议 **A + D：保留明确的FP32 CPU后备，下一轮优先Model Comparison**；不直接将FP16 DML接入生产，也不据FP32失败宣告整个DirectML不可用。原因、全矩阵、版本差异、显存口径、终止记录与证据边界见 [P0 Reference](MODULE_DEVELOPMENT_REFERENCE_V1.md#15-v02-p0-resource-investigation)。Host/SDK/生产Module/依赖/正式模型与安装包均未改；未开始Batch或性能面板。完整Host solution独立输出build 0 warnings / 0 errors，不等于UI/批处理验收；未提交。

### 2026-09-03 — Background Remover V0.2 后端准入检查

状态：**Research / Blocked；本记录后随 P0 提交 `635bb60`，V0.2 产品实现未开始**。开始时只读核实 HEAD、本地 origin/main 和 GitHub main 均为 `88efae8083c4e173b727bbe048854e939f9ddaaa feat: add background remover module v0.1`，工作树干净。

先验证现有模型可用性，再接入批量与 UI。当前 NuGet DirectML 最新稳定包为 1.24.4，不能假定与 V0.1 CPU 1.29.0 同版本。独立探针保持 FP32 模型和生产 Pre/Postprocess，在 RTX 4060 Laptop GPU 上创建 Session 并完成第一次 Run，第二次 Run 于 `DmlFusedNode_13_45` 返回 `0x8007000E`，随后 DirectML 命令列表关闭失败。按所有者规定停止 GPU 实现，没有换模型、调整优化配置试错或通过 CPU fallback 伪装 GPU 成功。该证据是当前模型/后端/硬件组合的资源与执行阻断，不是已证实的算子不支持或内存泄漏。

同时按本轮要求以原 CPU 1.29.0 做连续 20 次合成输入和 Session Dispose 测量，重新检查 V0.1 约 12.60 GB 风险，详见 [V0.2 backend gate](MODULE_DEVELOPMENT_REFERENCE_V1.md#14-v02-backend-gate)。生产代码、依赖、模型、SDK、Host 和已安装模组保持 V0.1；Batch、20 张真实标准集、完整 CPU/GPU 对比及人工验收尚未进行。后续先由所有者决定如何处理后端阻断，不自动进入其他运行时、FP16 或第二模型。

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
