# Third-party notices

## Microsoft ONNX Runtime 1.29.0

The module distributes the CPU runtime from Microsoft.ML.OnnxRuntime and its managed dependency. Copyright (c) Microsoft Corporation. MIT license: <https://github.com/microsoft/onnxruntime/blob/rel-1.29.0/LICENSE>. Upstream third-party notices: <https://github.com/microsoft/onnxruntime/blob/rel-1.29.0/ThirdPartyNotices.txt>.

## BiRefNet Lite ONNX model

The ONNX Runtime Managed 1.29.0 package transitively includes System.Numerics.Tensors 9.0.0 (MIT, .NET Foundation and Contributors). Its license and third-party notices are also copied into `Licenses/`; it is not an additional direct dependency.

- Model: BiRefNet_lite-ONNX; repository: <https://huggingface.co/onnx-community/BiRefNet_lite-ONNX>.
- Base: ZhengPeng7/BiRefNet_lite, <https://huggingface.co/ZhengPeng7/BiRefNet_lite>.
- Source file: `onnx/model.onnx`; local package: `Models/model.onnx`.
- SHA256: `5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333`.
- License: MIT, as declared by the upstream model repositories. This model was not trained by addToolBox.
- BiRefNet project and license: <https://github.com/ZhengPeng7/BiRefNet>.

The license texts and ONNX Runtime's bundled third-party notices must accompany distributed packages. Source attribution does not imply endorsement by the upstream authors.

The package includes ONNX Runtime's license/notices in `Licenses/`. BiRefNet's MIT license follows (Copyright (c) 2024 ZhengPeng):

```text
MIT License

Copyright (c) 2024 ZhengPeng

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
