namespace AddToolBox.BackgroundRemover;

// Separable antialiased bilinear RGB resize, matching the accepted P1 input convention.
// Downsampling widens the triangle filter; each axis rounds to 8 bits before normalization.
// This is input resampling, not alpha feathering or edge/quality enhancement.
internal static class ImagePreprocessing
{
    private const int Side = 1024;
    private const int Precision = 22;
    private sealed record Kernel(int Start, int[] Weights);

    internal static void Fill(byte[] bgra, int width, int height, float[] output)
    {
        if (output.Length != 3 * Side * Side || bgra.Length != checked(width * height * 4))
            throw new ArgumentException("Invalid image/tensor dimensions.");
        var horizontal = BuildKernels(width);
        var vertical = BuildKernels(height);
        var rows = new byte[checked(Side * height * 3)];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < Side; x++)
            {
                var kernel = horizontal[x];
                for (var channel = 0; channel < 3; channel++)
                {
                    var sum = 1 << (Precision - 1);
                    for (var k = 0; k < kernel.Weights.Length; k++)
                        sum += bgra[(y * width + kernel.Start + k) * 4 + 2 - channel] * kernel.Weights[k];
                    rows[(y * Side + x) * 3 + channel] = (byte)Math.Clamp(sum >> Precision, 0, 255);
                }
            }
        ReadOnlySpan<float> mean = [0.485f, 0.456f, 0.406f];
        ReadOnlySpan<float> std = [0.229f, 0.224f, 0.225f];
        for (var y = 0; y < Side; y++)
        {
            var kernel = vertical[y];
            for (var x = 0; x < Side; x++)
                for (var channel = 0; channel < 3; channel++)
                {
                    var sum = 1 << (Precision - 1);
                    for (var k = 0; k < kernel.Weights.Length; k++)
                        sum += rows[((kernel.Start + k) * Side + x) * 3 + channel] * kernel.Weights[k];
                    var value = Math.Clamp(sum >> Precision, 0, 255);
                    output[channel * Side * Side + y * Side + x] = (value / 255f - mean[channel]) / std[channel];
                }
        }
    }

    private static Kernel[] BuildKernels(int length)
    {
        var scale = (double)length / Side;
        var support = Math.Max(1, scale);
        var kernels = new Kernel[Side];
        for (var i = 0; i < Side; i++)
        {
            var center = (i + 0.5) * scale;
            var start = Math.Max(0, (int)(center - support + 0.5));
            var end = Math.Min(length, (int)(center + support + 0.5));
            var weights = new double[end - start];
            double total = 0;
            for (var k = 0; k < weights.Length; k++)
                total += weights[k] = Math.Max(0, 1 - Math.Abs((start + k - center + 0.5) / support));
            var normalized = new int[weights.Length];
            for (var k = 0; k < weights.Length; k++)
                normalized[k] = (int)(weights[k] / total * (1 << Precision) + 0.5);
            kernels[i] = new Kernel(start, normalized);
        }
        return kernels;
    }
}
