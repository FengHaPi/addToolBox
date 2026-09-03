# Model preparation

Run `powershell -ExecutionPolicy Bypass -File tools/prepare-model.ps1` from this module directory, then build again.
Build never downloads the model. A missing or corrupt model is reported on the first processing request.

- Model export: [CoderViking/birefnet-lite-onnx](https://huggingface.co/CoderViking/birefnet-lite-onnx/tree/dc06453148f01ef4131f17e9b791345e32e8ee78)
- Base: [ZhengPeng7/BiRefNet_lite](https://huggingface.co/ZhengPeng7/BiRefNet_lite)
- Source file: `birefnet-lite-1024.onnx`, FP32, 199681624 bytes, MIT/upstream BiRefNet.
- Revision: `dc06453148f01ef4131f17e9b791345e32e8ee78`.
- Local/package file: `Models/model.onnx`.
- SHA256: `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`.

Both revision and SHA256 pin the accepted B artifact. Package verification rejects a missing or mismatched model.
The external model was not trained by addToolBox. Do not commit ONNX binaries.
