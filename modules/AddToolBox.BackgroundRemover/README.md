# 去背景 / Background Remover V0.2.1

Local Windows x64 WPF background removal: Auto / GPU / CPU, one-image review and bounded sequential batches. Images stay on the computer. V0.2.1 adds batch thumbnails and conservative alpha/RGB edge refinement. Status: **FROZEN / ACCEPTED WITH KNOWN LIMITATIONS**, confirmed by the owner on 2026-09-03. Measured validation is recorded in the repository's Module Development Reference.

The owner accepts the current speed, batch thumbnails and observed reduction in white/gray fringe and edge contamination. **KEEP MODEL / KEEP STATIC EXPORT**: B Static BiRefNet Lite remains the only production model. EdgeRefinement remains automatically enabled; alpha limits, thresholds, radius, RGB rules and protection logic are frozen. Acceptance covers the current product with the limitations below; no numerical quality scores were invented.

## Known quality limitations

V0.2.1 is not an Adobe / remove.bg-level professional cutout solution. Hair and animal fur may retain gray haze, color fringe or background contamination. Backlit halos and flyaway hair remain difficult. Transparent or translucent objects do not have guaranteed physically correct alpha, and very thin structures may be partially lost.

Most significantly, objects close to the background in appearance, complex product structures or ambiguous subjects can lose real foreground regions. The owner's three wooden chests example has a missing part of the lower chest's left top surface already present in the raw Lite alpha. This is a **MODEL CAPABILITY LIMITATION**, not an EdgeRefinement bug. Postprocessing cannot recover foreground already classified as background. The owner reports a more complete Adobe result and minor loss with remove.bg, both better overall on this case; those services were not independently retested here.

V0.3 quality research is **DEFERRED / FUTURE QUALITY RESEARCH**. Existing evidence is retained separately; it is not a current production plan. addToolBox is a general modular toolbox, and the first module has validated packaging, local inference, GPU/CPU, batch processing, error isolation, instrumentation and owner acceptance. Further quality development requires a separate authorized task.

## Prepare, build and package

Run the model preparation script explicitly. Build and package never download models or test images.

    .\tools\prepare-model.ps1
    dotnet restore .\AddToolBox.BackgroundRemover.csproj --artifacts-path <build-root>
    dotnet build .\AddToolBox.BackgroundRemover.csproj -c Release --no-restore --artifacts-path <build-root>
    .\tools\package.ps1 -BuildRoot <build-root> -OutputDirectory <new-package-folder>

Preparation pins revision and verifies SHA256 and bytes. An existing mismatched model is left untouched. Preserve the old model in a separate development backup before an explicitly approved replacement. The package script uses PowerShell 7 and built-in .NET compression, requires the verified model, builds Release without restore, and accepts only the module's explicit release file list. PDB/import libraries are excluded; unexpected assets fail packaging.

### Single-file distribution (.atbmod)

**Packaging Support: IMPLEMENTED. Host .atbmod Import: NOT YET IMPLEMENTED.** The transport file is a standard ZIP archive with an `.atbmod` extension. The folder package remains the development artifact for inspection, diffing and local loading; `.atbmod` is the distribution artifact. The future user-facing goal is receiving one file, but the current Host still accepts complete folders only.

From the repository root, after explicitly restoring/building the module for the chosen build root:

    .\modules\AddToolBox.BackgroundRemover\tools\package.ps1 -BuildRoot .\artifacts\background-remover-package\module-build -OutputDirectory .\artifacts\background-remover-package\package-0.2.1

The script retains the staging folder and creates these siblings in its parent directory:

- `AddToolBox.BackgroundRemover-0.2.1.atbmod`
- `AddToolBox.BackgroundRemover-0.2.1.atbmod.sha256` containing `<sha256>  <archive filename>`
- `roundtrip/`, a retained extraction for file-by-file verification

All four output paths must be unused. Existing output is never overwritten or deleted; reruns use a fresh parent directory. Failure preserves partial artifacts for diagnosis and does not report success or produce a checksum before validation completes.

`module.json` and the entry DLL are at archive root, without a wrapper folder. Paths use `/`, entries are written in ordinal order with `CompressionLevel.Optimal`, and all entries use the fixed ZIP timestamp `2000-01-01 00:00:00` (created with UTC offset zero). The archive is reopened to check paths, duplicates, the exact release file set, manifest and every entry's size/SHA, including the model's decompressed bytes. Roundtrip extraction must match staging paths, count, sizes and hashes. Stable order/timestamps support repeatability for identical content in the same compression environment; this does not promise identical builds across SDK or compression runtime versions.

Runtime layout, manifest SSOT, SDK ABI and loading remain unchanged. Native dependencies stay in their existing locations; this package currently has root-level native DLLs and no `runtimes/` subfolder. Nothing loads directly from ZIP. The container is not a sandbox or signature: installed modules still run with the Host's user permissions and process boundary. Only import trusted modules. No installed directory is changed by packaging.

Module id, display name, kind and V1 entry contract are unchanged; manifest version is 0.2.1. The only host reference remains SDK with private runtime output excluded. This module is outside the Host solution. V1 import rejects a duplicate ID; it has no update UI. Development replacement requires closing Host and preserving a verified backup of the installed module folder. Do not change Host to bypass this rule.

## Model and runtime

- BiRefNet Lite FP32, CoderViking/birefnet-lite-onnx, source birefnet-lite-1024.onnx.
- Revision: dc06453148f01ef4131f17e9b791345e32e8ee78.
- SHA256: 50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67.
- Size: 199681624 bytes. Package path: Models/model.onnx. MIT/upstream BiRefNet.
- One Microsoft.ML.OnnxRuntime.DirectML 1.24.4 distribution, Managed 1.24.4 and Microsoft.AI.DirectML 1.15.4. CPU EP and DML EP share one native ORT DLL.
- No CPU 1.29.0 runtime, Python, CUDA, OpenCV or extra image library in the package.
- Lazy session initialization only when processing starts. One cached session per view, one inference at a time. An idle backend switch disposes the old session; the next session is lazy.
- DML disables Memory Pattern and uses ORT_SEQUENTIAL. GPU labels mean DML selected, not a claim that every graph node executes on GPU.

## Image contract

PNG, JPEG/JPG and BMP remain supported. WPF decodes with OnLoad, releases the input handle and preserves encoded RGB without applying embedded ICC transforms, matching P1's accepted input. TIFF/WebP are not added.

Antialiased bilinear RGB resize to 1024 x 1024, /255, ImageNet mean/std, NCHW FP32. One worker-owned input float buffer is reused. Metadata must be input_image [1,3,1024,1024] and output_image [1,1,1024,1024]. Stable sigmoid, bilinear alpha resize to original dimensions, predicted alpha multiplied by original alpha.

V0.2.1 automatically refines semi-transparent edges using nearby near-opaque foreground and near-transparent background color references. Consistent local color mixing permits conservative alpha tightening (at most 6/255 and approximately 6% of coverage, rounded to 8-bit) and signed RGB decontamination (at most 48/255 per channel), covering white, gray, colored and dark fringe. Alpha ridges retain their coverage; fully opaque pixels and input pixels with existing nonzero transparency remain unchanged. No positive alpha pixel is removed; zero-alpha RGB is cleared after sampling. Preview and exported PNG share this result. One pooled byte-per-pixel mask snapshot prevents scan-order propagation; no second inference or user quality mode. This is conservative mitigation, not complete matting: wide haze, isolated fur/hair, true backlight, transparency errors and missing model structures may remain. No global erosion/dilation, blur, feathering, model selection or simple-background fast path.

## Use

Choose one file for Single; choose two or more files for Batch. Drop files, folders, or both. Folder discovery is recursive, skips reparse directories to prevent cycles, deduplicates case-insensitively and sorts deterministically. Full-size images are processed one at a time.

Batch selection immediately shows a horizontal virtualized thumbnail strip inside the original panel: thumbnail, truncated filename, and waiting/processing/success/failure. The first item is the default preview; processing items are highlighted and followed automatically. Clicking selects an original preview until the next processing item; the result panel continues to show the latest successful result. Thumbnails decode only for realized visible containers, at longest side 160px, with a 32-item cache. Click previews are limited to 1280px, rapid clicks coalesce, and neither pipeline queues all files for decoding. Batch completion trims offscreen thumbnails; changing input or unloading releases the thumbnail cache. Invalid images show a preview placeholder and receive processing failure status from the existing batch worker.

Single keeps original and transparent result previews. Save PNG writes to Windows DesktopDirectory as <name>-no-bg.png, numbering occupied names. No save dialog or overwrite. Missing desktop configuration is an explicit error.

Batch creates a desktop folder named 去背景_yyyy-MM-dd_HHmmss and automatically saves each success. Processing is load -> preprocess -> inference -> postprocess -> save -> next, with awaited preview updates. No unbounded decode queue or retained collection of results. Stop prevents the next image from starting; active native inference is allowed to finish.

Per-file decode/processing/save errors continue to the next item. A UTF-8 失败记录.txt records index, filename, stage and short error without private absolute paths or large stack traces. Forced GPU backend failure stops the batch. Auto disposes a failed DML session, visibly marks GPU unavailable, retries the current image once on CPU, then stays on CPU for this view's Auto session. Unrelated per-image errors do not trigger backend switching.

Performance is off by default: no CPU/memory timer exists while off. On starts one 1-second DispatcherTimer for process CPU-time delta and WorkingSet64. The small overlay shows device, CPU, memory, current time, throughput and batch progress without resizing the previews. Back stops sampling while the cached view/session remain; returning resumes sampling only if previously enabled.

ALC isolates dependencies, not permissions or native crashes. Only trusted modules should be imported. See THIRD_PARTY_NOTICES.md and Models/README.md.
