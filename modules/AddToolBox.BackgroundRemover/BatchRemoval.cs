using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace AddToolBox.BackgroundRemover;

internal sealed record BatchUpdate(int Index, int Total, int Success, int Failed,
    BitmapSource? Original, RemovalResult? Result, double CurrentSeconds, double PerSecond, string? Message);
internal sealed record BatchSummary(int Success, int Failed, int Unprocessed, string OutputFolder,
    TimeSpan Elapsed, bool BackendStopped, string? FailureLogError);

// One sequential worker: load -> process -> save -> awaited UI acknowledgement -> next.
// Only paths and short failure records scale with task count. No bitmap/result collection.
internal static class BatchRemoval
{
    public static async Task<BatchSummary> RunAsync(IReadOnlyList<string> paths, string outputFolder,
        BackgroundRemovalEngine engine, CancellationToken stop, Func<BatchUpdate, Task> report,
        IProgress<string>? engineProgress = null)
    {
        var total = Stopwatch.StartNew();
        var success = 0;
        var failed = 0;
        var backendStopped = false;
        var failures = new List<string>();
        for (var i = 0; i < paths.Count && !stop.IsCancellationRequested; i++)
        {
            var stage = "解码";
            var current = Stopwatch.StartNew();
            try
            {
                var original = BackgroundRemovalEngine.LoadImage(paths[i]);
                await report(new(i + 1, paths.Count, success, failed, original, null, 0,
                    success / Math.Max(total.Elapsed.TotalSeconds, 0.001), null)).ConfigureAwait(false);
                if (stop.IsCancellationRequested) break;
                stage = "推理";
                var result = engine.Process(original, engineProgress);
                stage = "保存";
                ImageFiles.SaveUnique(result.Image, outputFolder, paths[i]);
                success++;
                await report(new(i + 1, paths.Count, success, failed, null, result, current.Elapsed.TotalSeconds,
                    success / Math.Max(total.Elapsed.TotalSeconds, 0.001), null)).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failed++;
                if (error is ImageProcessingException processing) stage = processing.Stage;
                backendStopped = error is BackendFailureException;
                Trace.TraceError("Background Remover item {0}, stage {1}: {2}", i + 1, stage, error);
                var message = ShortError(error);
                // Index distinguishes repeated names without publishing private absolute input paths.
                failures.Add($"[{i + 1}] {Path.GetFileName(paths[i])} | {stage} | {message}");
                await report(new(i + 1, paths.Count, success, failed, null, null, current.Elapsed.TotalSeconds,
                    success / Math.Max(total.Elapsed.TotalSeconds, 0.001), message)).ConfigureAwait(false);
                if (backendStopped) break;
            }
        }
        string? logError = null;
        if (failures.Count > 0)
        {
            try
            {
                using var file = new FileStream(Path.Combine(outputFolder, "失败记录.txt"), FileMode.CreateNew, FileAccess.Write);
                using var writer = new StreamWriter(file, new UTF8Encoding(false));
                foreach (var failure in failures) await writer.WriteLineAsync(failure).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Trace.TraceError("Background Remover failure record write failed: {0}", error);
                logError = "失败记录无法写入：" + ShortError(error);
            }
        }
        return new(success, failed, paths.Count - success - failed, outputFolder, total.Elapsed, backendStopped, logError);
    }

    private static string ShortError(Exception error)
    {
        if (error is BackendFailureException) return "GPU 后端不可用，批处理已停止";
        var cause = error is ImageProcessingException { InnerException: { } inner } ? inner : error;
        return cause switch
        {
            UnauthorizedAccessException => "没有文件读写权限",
            NotSupportedException => "不支持的图片格式或损坏文件",
            FileFormatException => "图片格式无效或文件损坏",
            IOException => "文件读取或保存失败",
            _ => $"处理失败（{cause.GetType().Name}）"
        };
    }
}
