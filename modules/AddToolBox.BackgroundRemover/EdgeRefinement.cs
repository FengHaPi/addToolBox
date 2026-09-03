using System.Buffers;

namespace AddToolBox.BackgroundRemover;

// Shared single/batch postprocess on straight BGRA, before preview or PNG encoding.
// Local color evidence is deliberately required: this is not a replacement matting model.
internal static class EdgeRefinement
{
    internal static void Apply(byte[] pixels, int width, int height, byte[]? protectedAlpha)
    {
        var count = checked(width * height);
        var alpha = ArrayPool<byte>.Shared.Rent(count);
        try
        {
            // Read an immutable mask so corrections do not propagate with scan order.
            for (var i = 0; i < count; i++) alpha[i] = pixels[i * 4 + 3];
            var radius = Math.Clamp((int)Math.Ceiling(Math.Max(width, height) / 1024.0) * 2, 4, 12);
            ReadOnlySpan<int> dx = [-1, 0, 1, -1, 1, -1, 0, 1];
            ReadOnlySpan<int> dy = [-1, -1, -1, 0, 0, 1, 1, 1];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var a = alpha[index];
                    if (a is <= 4 or >= 250 || protectedAlpha?[index] == 1) continue;
                    var foreground = -1;
                    var background = -1;
                    for (var distance = 1; distance <= radius && (foreground < 0 || background < 0); distance++)
                        for (var direction = 0; direction < dx.Length; direction++)
                        {
                            var sx = x + dx[direction] * distance;
                            var sy = y + dy[direction] * distance;
                            if ((uint)sx >= width || (uint)sy >= height) continue;
                            var sample = sy * width + sx;
                            if (protectedAlpha?[sample] == 1) continue;
                            if (foreground < 0 && alpha[sample] >= 250) foreground = sample * 4;
                            if (background < 0 && alpha[sample] <= 4) background = sample * 4;
                        }
                    if (foreground < 0 || background < 0) continue;
                    var p = index * 4;
                    double norm = 0, dot = 0;
                    for (var c = 0; c < 3; c++)
                    {
                        var direction = pixels[background + c] - pixels[foreground + c];
                        norm += direction * direction;
                        dot += (pixels[p + c] - pixels[foreground + c]) * direction;
                    }
                    if (norm < 3 * 30 * 30) continue;
                    var contamination = dot / norm;
                    if (contamination <= 0.04 || contamination >= 1) continue;
                    double residual = 0;
                    for (var c = 0; c < 3; c++)
                    {
                        var difference = pixels[p + c] - pixels[foreground + c]
                            - contamination * (pixels[background + c] - pixels[foreground + c]);
                        residual += difference * difference;
                    }
                    // Reject colors inconsistent with local foreground/background mixing.
                    // This also avoids pulling distinct jewelry or rim-light colors into the subject.
                    if (residual > 3 * 18 * 18) continue;

                    // A local alpha ridge may be a one-pixel strand. Keep its coverage.
                    var ridge = false;
                    for (var direction = 0; direction < 4; direction++)
                    {
                        var x1 = x + dx[direction]; var y1 = y + dy[direction];
                        var x2 = x - dx[direction]; var y2 = y - dy[direction];
                        if ((uint)x1 >= width || (uint)x2 >= width || (uint)y1 >= height || (uint)y2 >= height) continue;
                        if (a > alpha[y1 * width + x1] + 2 && a > alpha[y2 * width + x2] + 2) ridge = true;
                    }
                    if (!ridge)
                    {
                        var excess = a - (1 - contamination) * 255;
                        var reduction = Math.Min(Math.Min(6, a * 0.06), Math.Max(0, excess * 0.25));
                        // Never remove an existing nonzero pixel or move an opaque/background boundary.
                        pixels[p + 3] = (byte)Math.Max(1, a - (int)Math.Round(reduction));
                    }
                    // Move toward the nearby foreground along the measured background color vector.
                    // Signed correction handles dark/colored fringe as well as a white matte.
                    var amount = 0.85 * Math.Min(contamination, 1 - a / 255.0);
                    for (var c = 0; c < 3; c++)
                    {
                        var correction = Math.Clamp(amount * (pixels[background + c] - pixels[foreground + c]), -48, 48);
                        pixels[p + c] = (byte)Math.Clamp((int)Math.Round(pixels[p + c] - correction), 0, 255);
                    }
                }
            // Delay clearing until all background color samples have been consumed.
            for (var p = 0; p < count * 4; p += 4)
                if (pixels[p + 3] == 0) pixels[p] = pixels[p + 1] = pixels[p + 2] = 0;
        }
        finally { ArrayPool<byte>.Shared.Return(alpha); }
    }
}
