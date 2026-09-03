using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AddToolBox.BackgroundRemover;

internal enum BackendMode { Auto, Gpu, Cpu }
internal enum ProcessingDevice { Gpu, Cpu }

internal sealed class BackendFailureException(string message, Exception inner) : Exception(message, inner);
internal sealed class ImageProcessingException(string stage, Exception inner) : Exception(inner.Message, inner)
{
    public string Stage { get; } = stage;
}

internal sealed record RemovalResult(BitmapSource Image, TimeSpan ModelInit, TimeSpan Preprocess,
    TimeSpan Inference, TimeSpan Postprocess, TimeSpan Total);

internal sealed class BackgroundRemovalEngine : IDisposable
{
    internal const string ModelSha256 = "50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67";
    private const int Side = 1024;
    private const string InputName = "input_image";
    private const string OutputName = "output_image";
    private readonly object _gate = new();
    private InferenceSession? _session;
    private float[]? _inputValues;
    private bool _gpuUnavailable;
    private bool _disposed;
    public BackendMode Mode { get; private set; } = BackendMode.Auto;
    public ProcessingDevice? ActualDevice { get; private set; }
    public bool UsedCpuFallback => _gpuUnavailable && Mode == BackendMode.Auto;

    public void SetBackend(BackendMode mode)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (Mode == mode) return;
            _session?.Dispose();
            _session = null;
            ActualDevice = null;
            Mode = mode;
        }
    }

    public static bool IsSupportedImage(string path) =>
        new[] { ".png", ".jpg", ".jpeg", ".bmp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static BitmapSource LoadImage(string path)
    {
        if (!IsSupportedImage(path))
            throw new NotSupportedException("支持 PNG、JPEG/JPG、BMP；不支持 WebP 或其他格式。");
        using var stream = File.OpenRead(path);
        // P1 consumes encoded RGB values without an ICC conversion. Preserve that input
        // contract; WPF's default profile conversion otherwise changes two accepted fixtures.
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile,
            BitmapCacheOption.OnLoad);
        if (decoder is not PngBitmapDecoder and not JpegBitmapDecoder and not BmpBitmapDecoder)
            throw new NotSupportedException("文件实际格式不是 PNG、JPEG 或 BMP。");
        var bitmap = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        bitmap.Freeze();
        return bitmap;
    }

    public static void SavePng(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        // Encode before opening the destination: encoder failure cannot truncate an existing file.
        using var encoded = new MemoryStream();
        encoder.Save(encoded);
        encoded.Position = 0;
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, $".background-remover-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                encoded.CopyTo(file);
            File.Move(temporary, path, false);
        }
        finally
        {
            // This invocation alone owns the temporary path; never remove an existing output.
            if (File.Exists(temporary))
            {
                try { File.Delete(temporary); }
                catch (Exception error) { Trace.TraceError("Background Remover temporary PNG cleanup failed: {0}", error); }
            }
        }
    }

    public RemovalResult Process(BitmapSource original, IProgress<string>? progress = null)
    {
        lock (_gate)
        {
            var failureStage = "模型初始化";
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (!original.IsFrozen || original.Format != PixelFormats.Bgra32)
                    throw new ArgumentException("输入必须是已冻结的 BGRA32 BitmapSource。", nameof(original));
                var total = Stopwatch.StartNew();
                var stage = Stopwatch.StartNew();
                var initTime = TimeSpan.Zero;
                if (_session is null)
                {
                    progress?.Report("正在加载模型…首次处理可能需要较长时间。");
                    InitializeSession(progress);
                    initTime = stage.Elapsed;
                }
                progress?.Report($"正在去除背景…本地 {(ActualDevice == ProcessingDevice.Gpu ? "GPU" : "CPU")} 推理中。");
                failureStage = "预处理";
                stage.Restart();
                var width = original.PixelWidth;
                var height = original.PixelHeight;
                var stride = checked(width * 4);
                var pixels = new byte[checked(stride * height)];
                original.CopyPixels(pixels, stride, 0);
                _inputValues ??= new float[3 * Side * Side];
                ImagePreprocessing.Fill(pixels, width, height, _inputValues);
                var input = new DenseTensor<float>(_inputValues, [1, 3, Side, Side]);
                var preprocessTime = stage.Elapsed;
                failureStage = "推理";
                stage.Restart();
                var recoveryInitTime = TimeSpan.Zero;
                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Run() =>
                    _session!.Run([NamedOnnxValue.CreateFromTensor(InputName, input)], [OutputName]);
                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs;
                try { outputs = Run(); }
                catch (OnnxRuntimeException error) when (ActualDevice == ProcessingDevice.Gpu && IsDmlFailure(error))
                {
                    _session!.Dispose();
                    _session = null;
                    ActualDevice = null;
                    if (Mode == BackendMode.Gpu)
                        throw new BackendFailureException("GPU 推理失败，处理已停止。可空闲时选择 CPU。", error);
                    MarkGpuUnavailable(error, progress);
                    var recoveryInit = Stopwatch.StartNew();
                    CreateSession(ProcessingDevice.Cpu);
                    recoveryInitTime = recoveryInit.Elapsed;
                    initTime += recoveryInitTime;
                    outputs = Run(); // One explicit retry; CPU failure propagates to the current item.
                }
                using var ownedOutputs = outputs;
                var output = outputs.Single().AsTensor<float>();
                if (!output.Dimensions.SequenceEqual(new[] { 1, 1, Side, Side }))
                    throw new InvalidDataException("推理输出 Shape 与已验证 Metadata 不一致。");
                var inferenceTime = stage.Elapsed - recoveryInitTime;
                failureStage = "后处理";
                stage.Restart();
                var logits = output.ToArray();
                for (var i = 0; i < logits.Length; i++)
                {
                    var value = logits[i];
                    if (!float.IsFinite(value))
                        throw new InvalidDataException("模型输出含非有限数值，未生成结果。");
                    // Stable sigmoid for both signs; avoid exp overflow for large negative logits.
                    var exp = MathF.Exp(value >= 0 ? -value : value);
                    logits[i] = value >= 0 ? 1 / (1 + exp) : exp / (1 + exp);
                }
                byte[]? protectedAlpha = null;
                for (var y = 0; y < height; y++)
                {
                    GetSample(y, height, Side, out var y0, out var y1, out var fy);
                    for (var x = 0; x < width; x++)
                    {
                        GetSample(x, width, Side, out var x0, out var x1, out var fx);
                        var alpha = Lerp(Lerp(logits[y0 * Side + x0], logits[y0 * Side + x1], fx),
                            Lerp(logits[y1 * Side + x0], logits[y1 * Side + x1], fx), fy);
                        var offset = y * stride + x * 4;
                        if (pixels[offset + 3] != 255)
                        {
                            protectedAlpha ??= new byte[checked(width * height)];
                            protectedAlpha[y * width + x] = 1;
                        }
                        pixels[offset + 3] = (byte)Math.Clamp((int)MathF.Round(alpha * pixels[offset + 3]), 0, 255);
                    }
                }
                EdgeRefinement.Apply(pixels, width, height, protectedAlpha);
                var result = BitmapSource.Create(width, height, original.DpiX, original.DpiY,
                    PixelFormats.Bgra32, null, pixels, stride);
                result.Freeze();
                return new RemovalResult(result, initTime, preprocessTime, inferenceTime, stage.Elapsed, total.Elapsed);
            }
            catch (BackendFailureException) { throw; }
            catch (Exception error) { throw new ImageProcessingException(failureStage, error); }
        }
    }

    private void InitializeSession(IProgress<string>? progress)
    {
        var root = Path.GetDirectoryName(typeof(BackgroundRemovalEngine).Assembly.Location)!;
        var path = Path.Combine(root, "Models", "model.onnx");
        using (var stream = File.OpenRead(path))
        {
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            if (!hash.Equals(ModelSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("模型 SHA256 不匹配；请重新准备已批准的模型。");
        }
        var device = Mode == BackendMode.Cpu || (Mode == BackendMode.Auto && _gpuUnavailable)
            ? ProcessingDevice.Cpu : ProcessingDevice.Gpu;
        try { CreateSession(device); }
        catch (Exception error) when (device == ProcessingDevice.Gpu &&
            error is OnnxRuntimeException or DllNotFoundException or EntryPointNotFoundException)
        {
            if (Mode == BackendMode.Gpu)
                throw new BackendFailureException("GPU 初始化失败，处理已停止。可空闲时选择 CPU。", error);
            MarkGpuUnavailable(error, progress);
            CreateSession(ProcessingDevice.Cpu);
        }
    }

    private void CreateSession(ProcessingDevice device)
    {
        var path = Path.Combine(Path.GetDirectoryName(typeof(BackgroundRemovalEngine).Assembly.Location)!, "Models", "model.onnx");
        using var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            EnableMemoryPattern = device == ProcessingDevice.Cpu
        };
        if (device == ProcessingDevice.Gpu) options.AppendExecutionProvider_DML(0);
        // The DirectML distribution also contains CPU EP. Exactly one native ORT and one session.
        var session = new InferenceSession(path, options);
        try
        {
            ValidateMetadata(session.InputMetadata, InputName, [1, 3, Side, Side]);
            ValidateMetadata(session.OutputMetadata, OutputName, [1, 1, Side, Side]);
            _session = session;
            ActualDevice = device;
        }
        catch { session.Dispose(); throw; }
    }

    private void MarkGpuUnavailable(Exception error, IProgress<string>? progress)
    {
        _gpuUnavailable = true;
        Trace.TraceError("Background Remover DirectML unavailable: {0}", error);
        progress?.Report("GPU 不可用，已自动切换 CPU");
    }

    // ORT 1.24.4's managed exception has no ErrorCode property. Match provider diagnostics
    // seen in the pinned runtime; unrelated operator/input failures remain per-image errors.
    private static bool IsDmlFailure(OnnxRuntimeException error) =>
        error.Message.Contains("Dml", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("DirectML", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("887A000", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("8007000E", StringComparison.OrdinalIgnoreCase);

    private static void ValidateMetadata(IReadOnlyDictionary<string, NodeMetadata> metadata, string name, int[] dimensions)
    {
        if (metadata.Count != 1 || !metadata.TryGetValue(name, out var node)
            || !node.IsTensor || node.ElementType != typeof(float) || !node.Dimensions.SequenceEqual(dimensions))
        {
            var actual = string.Join("; ", metadata.Select(item =>
                $"{item.Key}: {item.Value.ElementType}, [{string.Join(",", item.Value.Dimensions)}]"));
            throw new InvalidDataException($"ONNX Metadata 不匹配。预期 {name} float32 [{string.Join(",", dimensions)}]；实际 {actual}");
        }
    }

    private static void GetSample(int destination, int destinationLength, int sourceLength,
        out int first, out int second, out float fraction)
    {
        var coordinate = Math.Clamp((destination + 0.5) * sourceLength / destinationLength - 0.5, 0, sourceLength - 1);
        first = (int)coordinate;
        second = Math.Min(first + 1, sourceLength - 1);
        fraction = (float)(coordinate - first);
    }

    private static float Lerp(float left, float right, float fraction) => left + (right - left) * fraction;

    public void Dispose()
    {
        lock (_gate)
        {
            _session?.Dispose();
            _session = null;
            _inputValues = null;
            ActualDevice = null;
            _disposed = true;
        }
    }
}
