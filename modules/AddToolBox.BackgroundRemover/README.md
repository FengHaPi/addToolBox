# 去背景 / Background Remover V0.1

Independent, in-process WPF tool for **local CPU** background removal. Images are never uploaded. The owner accepted V0.1 manual functionality on 2026-09-03; detailed per-image quality scores were explicitly skipped. Pipeline checks do not establish visual quality.

## Prepare and build

From this module directory:

```powershell
powershell -ExecutionPolicy Bypass -File tools/prepare-model.ps1
dotnet build -c Release .\AddToolBox.BackgroundRemover.csproj
```

Import the entire `bin/Release/net10.0-windows/win-x64` folder using the host's **导入模组** button. Do not import the source directory or an individual DLL. Installed packages live under `%LOCALAPPDATA%\addToolBox\Modules\addtoolbox.background-remover`.

Requires Windows x64 and .NET 10 WPF host. The module is intentionally outside `AddToolBox.sln`. Its only host project reference is SDK, excluded from private runtime output. Its only direct NuGet is [Microsoft.ML.OnnxRuntime 1.29.0](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime/1.29.0), including its managed/native transitive assets. No GPU provider is enabled.

Build does not download anything beyond normal NuGet restore. Prepare the external model explicitly, then rebuild. Missing/corrupt models produce an error when processing; opening the view does not initialize the model.

## Model and pipeline

- Model: [onnx-community/BiRefNet_lite-ONNX](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX).
- Base: [ZhengPeng7/BiRefNet_lite](https://huggingface.co/ZhengPeng7/BiRefNet_lite).
- File: `onnx/model.onnx`, FP32, approximately 224 MB, MIT license.
- SHA256: `5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333`.
- This is an external pretrained model, not trained by addToolBox.
- WPF decodes PNG, JPEG/JPG or BMP with `OnLoad`, releasing the input file handle.
- Bilinear resize to 1024×1024; RGB / 255; ImageNet mean `[0.485, 0.456, 0.406]`, std `[0.229, 0.224, 0.225]`; float32 NCHW.
- Require `input_image [1,3,1024,1024]` and `output_image [1,1,1024,1024]`; runtime validates actual metadata, not inferred names.
- Stable sigmoid on logits; bilinear resize to original dimensions; predicted alpha multiplied by original alpha, original straight RGB retained.
- CPU inference and image IO run off the UI thread. One cached session, one processing operation at a time. Back retains the view/session until the host exits.

## Use and limits

Choose or drop an image, process, review on the checkerboard, then save a separate `-no-bg.png`. Existing output replacement requires the save dialog's confirmation; saving over the current original is rejected.

Input: PNG, JPEG/JPG, BMP. Output: RGBA PNG. TIFF and WebP are not supported in V0.1. No batch processing, cancellation, mask editing, GPU acceleration, or model selection. Warm processing reuses the first session; retained native memory is a V0.1 lifecycle limitation to measure before extending the contract.

ALC is dependency isolation, **not a sandbox**. Module code has host-process permissions. Only import trusted packages. See `THIRD_PARTY_NOTICES.md` and `Models/README.md`.
