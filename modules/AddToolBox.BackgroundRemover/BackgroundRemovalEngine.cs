using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AddToolBox.BackgroundRemover;

internal sealed record RemovalResult(BitmapSource Image, TimeSpan ModelInit, TimeSpan Preprocess,
    TimeSpan Inference, TimeSpan Postprocess, TimeSpan Total);

internal sealed class BackgroundRemovalEngine : IDisposable
{
    internal const string ModelSha256 = "5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333";
    private const int Side = 1024;
    private const string InputName = "input_image";
    private const string OutputName = "output_image";
    private readonly object _gate = new();
    private InferenceSession? _session;
    private bool _disposed;

    public static bool IsSupportedImage(string path) =>
        new[] { ".png", ".jpg", ".jpeg", ".bmp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static BitmapSource LoadImage(string path)
    {
        if (!IsSupportedImage(path))
            throw new NotSupportedException("V0.1 支持 PNG、JPEG/JPG、BMP；不支持 WebP 或其他格式。");
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
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
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoded.CopyTo(file);
    }

    public RemovalResult Process(BitmapSource original, IProgress<string>? progress = null)
    {
        lock (_gate)
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
                InitializeSession();
                initTime = stage.Elapsed;
            }
            progress?.Report("正在去除背景…本地 CPU 推理中。");
            stage.Restart();
            var width = original.PixelWidth;
            var height = original.PixelHeight;
            var stride = checked(width * 4);
            var pixels = new byte[checked(stride * height)];
            original.CopyPixels(pixels, stride, 0);
            var input = Preprocess(pixels, width, height);
            var preprocessTime = stage.Elapsed;
            stage.Restart();
            using var outputs = _session!.Run([NamedOnnxValue.CreateFromTensor(InputName, input)], [OutputName]);
            var output = outputs.Single().AsTensor<float>();
            if (!output.Dimensions.SequenceEqual(new[] { 1, 1, Side, Side }))
                throw new InvalidDataException("推理输出 Shape 与已验证 Metadata 不一致。");
            var inferenceTime = stage.Elapsed;
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
            for (var y = 0; y < height; y++)
            {
                GetSample(y, height, Side, out var y0, out var y1, out var fy);
                for (var x = 0; x < width; x++)
                {
                    GetSample(x, width, Side, out var x0, out var x1, out var fx);
                    var alpha = Lerp(Lerp(logits[y0 * Side + x0], logits[y0 * Side + x1], fx),
                        Lerp(logits[y1 * Side + x0], logits[y1 * Side + x1], fx), fy);
                    var offset = y * stride + x * 4;
                    pixels[offset + 3] = (byte)Math.Clamp((int)MathF.Round(alpha * pixels[offset + 3]), 0, 255);
                }
            }
            var result = BitmapSource.Create(width, height, original.DpiX, original.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return new RemovalResult(result, initTime, preprocessTime, inferenceTime, stage.Elapsed, total.Elapsed);
        }
    }

    private void InitializeSession()
    {
        var root = Path.GetDirectoryName(typeof(BackgroundRemovalEngine).Assembly.Location)!;
        var path = Path.Combine(root, "Models", "model.onnx");
        try
        {
            using (var stream = File.OpenRead(path))
            {
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                if (!hash.Equals(ModelSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"模型 SHA256 不匹配：{path}");
            }
            using var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
            // The approved CPU package's default provider; no GPU providers or silent provider fallback.
            var session = new InferenceSession(path, options);
            try
            {
                ValidateMetadata(session.InputMetadata, InputName, [1, 3, Side, Side]);
                ValidateMetadata(session.OutputMetadata, OutputName, [1, 1, Side, Side]);
                _session = session;
            }
            catch { session.Dispose(); throw; }
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"去背景模型初始化失败。\n模型位置：{path}\n{error.Message}", error);
        }
    }

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

    private static DenseTensor<float> Preprocess(byte[] pixels, int width, int height)
    {
        var values = new float[3 * Side * Side];
        float[] mean = [0.485f, 0.456f, 0.406f];
        float[] std = [0.229f, 0.224f, 0.225f];
        for (var y = 0; y < Side; y++)
        {
            GetSample(y, Side, height, out var y0, out var y1, out var fy);
            for (var x = 0; x < Side; x++)
            {
                GetSample(x, Side, width, out var x0, out var x1, out var fx);
                for (var channel = 0; channel < 3; channel++)
                {
                    var offset = 2 - channel; // BGRA storage -> RGB NCHW, unpremultiplied color.
                    var top = Lerp(pixels[(y0 * width + x0) * 4 + offset], pixels[(y0 * width + x1) * 4 + offset], fx);
                    var bottom = Lerp(pixels[(y1 * width + x0) * 4 + offset], pixels[(y1 * width + x1) * 4 + offset], fx);
                    values[channel * Side * Side + y * Side + x] = (Lerp(top, bottom, fy) / 255f - mean[channel]) / std[channel];
                }
            }
        }
        return new DenseTensor<float>(values, [1, 3, Side, Side]);
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
            _disposed = true;
        }
    }
}
