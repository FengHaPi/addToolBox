# Model preparation

Run `powershell -ExecutionPolicy Bypass -File tools/prepare-model.ps1` from this module directory, then build again.
Build never downloads the model. A missing or corrupt model is reported on the first processing request.

- Model: [onnx-community/BiRefNet_lite-ONNX](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX)
- Base: [ZhengPeng7/BiRefNet_lite](https://huggingface.co/ZhengPeng7/BiRefNet_lite)
- Source file: `onnx/model.onnx`, FP32, approximately 224 MB, MIT license.
- Local/package file: `Models/model.onnx`.
- SHA256: `5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333`.

The SHA256 pins the accepted artifact even if the upstream `main` download changes.
The external model was not trained by addToolBox. Do not commit ONNX binaries.
