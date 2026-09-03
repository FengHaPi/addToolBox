# Changelog

记录对用户和开发者有意义的变化，不逐条复制 Git Log。详细原因、研究数据和证据边界见 [PROJECT_HISTORY](docs/PROJECT_HISTORY.md)。

截至 **2026-09-03**，World Canvas Host Baseline 之后的 Module System V0.1 与首个去背景模组已通过 build、自动检查与所有者人工功能验收，并完成 [Module Development Reference V1](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md) 测量记录。逐图质量明细按所有者要求跳过；这不是正式用户 Release，不创建 Tag。

当前生产基线为 **Background Remover V0.2.1 / FROZEN / ACCEPTED WITH KNOWN LIMITATIONS**。Owner于2026-09-03确认速度可接受、Batch缩略图符合预期、EdgeRefinement相比之前有改善，并接受当前边缘与主体完整性限制。保持 **KEEP MODEL / KEEP STATIC EXPORT**；这是现阶段产品验收，不是完美抠图承诺。V0.3研究正式Deferred，本轮不创建Tag或GitHub Release。

## Unreleased

### Fixed

- Background Remover V0.2.1：批量选择后显示原图主预览和轻量横向缩略图列表，支持点击切换、当前项高亮与等待/处理中/成功/失败状态；可见项低分辨率解码、有限缓存，修正多图已选但主区域空白的体验。
- Background Remover V0.2.1：补充默认半透明边缘Alpha精修与RGB去污染，覆盖白/灰/彩色/深色背景混色；保留不透明主体、已有非零透明输入和细线Alpha峰值，完全透明像素清除隐藏RGB。预览与导出PNG共用结果，不新增档位、模型或第二次推理。项链/细结构及部分长发边缘有所改善；动物绒毛、卷发、逆光及透明困难样本仍有残留，不宣称全部白边或脏边消除。

### Added

- Background Remover V0.2：多选/拖入文件与递归文件夹、顺序批处理与停止、桌面批次文件夹自动保存、单项错误隔离和 UTF-8 失败记录；仅保留路径列表、当前输入和最近成功结果。
- Background Remover V0.2：Auto / GPU / CPU，单一 ONNX Runtime DirectML 1.24.4 同时提供 CPU 与 DML 后端；首次处理才创建 Session，空闲切换释放旧 Session。Auto 后端失败明确提示并转 CPU，强制 GPU 后端失败停止批次。
- Background Remover V0.2：默认关闭的轻量性能面板，开启时每秒采样进程 CPU 与 Working Set；关闭后无采样定时器。只新增右上设备下拉和性能按钮，预览布局不移动。
- Module System V0.1：WPF-free SDK View Type Contract、Manifest Metadata SSOT、人工文件夹导入、已安装模组启动发现、独立 managed/native ALC、动态 World Canvas Tile 与缓存 View。
- 第一个正式模组 Background Remover V0.1：ONNX Runtime 1.29.0 CPU、固定 SHA 的外部 BiRefNet Lite、PNG/JPEG/BMP 单张输入、本地后台去背景和透明 PNG 保存；Host 无模组工程引用。
- Module Format V1 与 Module Development Reference V1，记录真实构建/Package/导入/加载/推理/资源数据、错误矩阵和下一 Module Checklist。CPU FP32 探针进程工作集峰值约 12.60 GB，V1 缓存 Session；这是需要后续评估的已知资源限制，不等于模型磁盘大小。
- 建立简洁 CHANGELOG、详细 PROJECT_HISTORY，以及 [AGENTS Change Documentation](AGENTS.md#23-change-documentation) 同任务更新制度，已独立提交：`4b26a0a docs: establish project history and change documentation policy`。
- World Canvas：Tool 世界坐标与 WorldLayer 相机投影；默认空 Workspace 合法。
- Middle Mouse Pan：空白或对象上的中键拖动移动 Viewport。
- Mouse Wheel Zoom：以鼠标位置为锚点，范围 0.25–3.00。
- Reset View：恢复初始 Camera 中心与 1.0 Zoom，不修改 Tool 或 Layout Lock。
- Dynamic WorldExtent：逻辑 Rect 的按需扩张与交互结束后的 Lazy Shrink，保护 Viewport 和全部 Item bounds，不生成空白区域 Visual。

### Changed

- Background Remover V0.2 / V0.2.1：正式采用已验收的 B Static BiRefNet Lite FP32导出（固定revision / SHA，199681624 bytes）；按P1约定使用抗锯齿bilinear和未经ICC转换的编码RGB。单张“保存PNG”默认写桌面、重名编号且不覆盖，Batch逐项自动保存。V0.2.1默认启用EdgeRefinement，Alpha上限、阈值、半径、RGB规则与保护逻辑全部冻结。完整测量与收口状态见[冻结基线](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#21-v021-frozen-production-baseline)。
- Reset Layout 与 Reset View 完全独立：Reset Layout 只恢复 Tool 默认世界位置，保留 Camera 中心和 Zoom，允许默认 Tool 仍在视口外；无 Item 时同样可安全执行。
- Window Resize 只更新 Viewport 可见范围和相机投影，不改 Camera 中心或 Tool WorldPosition。Tool 离屏是合法状态。
- Workspace 方向转为可扩展的 World Canvas。Widget、Note、Information Card 等非 Tool 内容仍为 Planned / Current direction，尚无公共 WorkspaceItem 抽象。

### Removed

- 删除当前产品的 Automatic Compaction / 自动 Resize 重排调用链，以及依赖 Tool 排布的动态最小窗口尺寸更新；保留已提交的 Project1D / Legacy Solve2D 研究基础。
- 删除五个 prototype built-in test tiles（calculator / image / file / text / color）的视觉实例、初始位置、硬编码映射及原型提示，移除 Core 中无运行用途的 BuiltInTools 测试列表；保留 ToolDefinition 与 Tool Host / Back 基础。

### Deprecated / Retired

- **Automatic Compaction — Retired from core Resize behavior。** 自动收拢与长期 World Canvas 语义冲突，当前产品路径已移除，研究基础继续保留。未来用户主动触发的 Arrange / Auto layout 仍只是可评估方向。

### Research / Technical Milestone

- **Background Remover V0.3-P0 — DEFERRED / FUTURE QUALITY RESEARCH（2026-09-03 Owner决定）。** 保留A/B/C固定8张困难图与测量证据。B/C恢复A误删的木箱左侧顶面；B约20.13秒、约941MB模型及资源代价不适合当前默认生产。C HR-matting为PROMISING，PyTorch/CUDA warm约2.95秒、约444MB权重，生产ONNX、CPU后备与最终体积仍未验证。原始逐图评分继续保持未填写；不继续C ONNX Export、V0.3-P1或自动安排下一次实验。addToolBox的重点回到Module生态，后续质量工作须另行授权。详见[研究记录](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#20-v03-p0-quality-and-subject-completeness)。
- **V0.2-P1 Model / ONNX Export Comparison — Research / Owner visual acceptance PASSED（2026-09-03）。** 建立 16 张自由许可标准图与固定 6 图性能集；图片和输出不进入 Git。所有者定性验收普通人物、发丝、商品硬边、逆光、透明难例和细结构，未发现 B 相比 A 足以拒绝生产替换的明显新增退化，批准 B Static BiRefNet 为 Production Approved Candidate，方向为 **KEEP MODEL / CHANGE EXPORT**。未填写或虚构 1–5 评分，不承诺所有透明物体或细节完美，也不表示 V0.2 集成已验收。P1 探针 B CPU peak WS 3.47 GB、DML 10/10、warm median 0.323 秒；BEN2 CPU 后备门槛失败，仅有 6 图结果，不继续补测。详见 [P1 Reference](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#16-v02-p1-model--onnx-export-comparison)。
- **V0.2-P0 resource investigation — Research（2026-09-03；记录已提交 `635bb60`）。** CPU FP32 四组内存矩阵表明默认约 12.49 GB 工作集受 Arena / Memory Pattern 共同放大，仍属 Functionally Stable but Resource Heavy，不定性为泄漏。FP32 DirectML 第二次再失败，本次为设备移除而非同一 OOM HRESULT；临时 FP16 DML 10/10 成功、warm 约 2.07 秒，但资源仍超预算且真实质量未验收，不进入生产。建议保留 CPU 后备并先评估模型，详见 [P0 Reference](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#15-v02-p0-resource-investigation)；正式代码、依赖和模型保持 V0.1。
- **V0.2 backend gate — Blocked（2026-09-03；记录随 P0 提交 `635bb60`）。** 从干净 `88efae8` 开始，现有 FP32 模型的 DirectML 1.24.4 隔离探针在第二次 Run 出现命令列表执行内存分配失败；按任务停止条件未接入生产。原 CPU 1.29.0 的约 12.60 GB Working Set 风险另行复测，方法、结果与证据边界见 [Reference](docs/MODULE_DEVELOPMENT_REFERENCE_V1.md#14-v02-backend-gate)。未实现或验收 V0.2 Batch/GPU/性能面板，不将高工作集直接称为泄漏。

### Known limitations — Background Remover V0.2.1

- 当前不是Adobe / remove.bg级专业抠图方案：发丝/动物绒毛仍可能灰雾、色边和背景污染；逆光halo与飞发能力有限；透明/半透明物体不能保证物理正确Alpha；极细结构可能局部损失。
- 主体与背景接近、结构复杂或语义不明确的商品图可能误删真实主体。Owner三个木箱案例的左侧顶面缺失已存在于原始模型Alpha，属于 **MODEL CAPABILITY LIMITATION**，不是EdgeRefinement Bug；后处理不能恢复已判为背景的主体。Owner观察Adobe更完整、remove.bg有轻微缺失但总体更好；本地未独立复测这两个服务。

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
