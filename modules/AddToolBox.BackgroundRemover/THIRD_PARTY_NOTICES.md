# Third-party notices

## Microsoft ONNX Runtime 1.24.4 / DirectML 1.15.4

The module distributes one Microsoft.ML.OnnxRuntime.DirectML 1.24.4 runtime (CPU EP and DML EP) and Managed 1.24.4. Copyright (c) Microsoft Corporation. MIT license: <https://github.com/microsoft/onnxruntime/blob/rel-1.24.4/LICENSE>. Its necessary Microsoft.AI.DirectML 1.15.4 redistributable has Microsoft's separate license; the package includes DirectML-LICENSE.txt and DirectML-ThirdPartyNotices.txt in Licenses/. It is not described as MIT merely because ONNX Runtime is MIT.

## BiRefNet Lite ONNX model

The ONNX Runtime Managed 1.24.4 package transitively includes System.Numerics.Tensors 9.0.0 (MIT, .NET Foundation and Contributors). Its license and third-party notices are also copied into `Licenses/`; it is not an additional direct dependency.

- Model: BiRefNet Lite; export: <https://huggingface.co/CoderViking/birefnet-lite-onnx/tree/dc06453148f01ef4131f17e9b791345e32e8ee78>.
- Base: ZhengPeng7/BiRefNet_lite, <https://huggingface.co/ZhengPeng7/BiRefNet_lite>.
- Source file: `birefnet-lite-1024.onnx`; local package: `Models/model.onnx`; 199681624 bytes.
- Revision: `dc06453148f01ef4131f17e9b791345e32e8ee78`.
- SHA256: `50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67`.
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
