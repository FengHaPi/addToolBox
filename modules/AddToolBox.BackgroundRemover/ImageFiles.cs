using System.IO;
using System.Windows.Media.Imaging;

namespace AddToolBox.BackgroundRemover;

internal static class ImageFiles
{
    public static string[] Collect(IEnumerable<string> inputs)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            var path = Path.GetFullPath(input);
            if (Directory.Exists(path))
            {
                // Reparse directories can point back into their ancestors or outside the dropped tree.
                var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = false,
                    AttributesToSkip = FileAttributes.ReparsePoint };
                foreach (var file in Directory.EnumerateFiles(path, "*", options))
                    if (BackgroundRemovalEngine.IsSupportedImage(file)) paths.Add(Path.GetFullPath(file));
            }
            else if (BackgroundRemovalEngine.IsSupportedImage(path)) paths.Add(path);
        }
        return paths.Order(StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray();
    }

    public static string Desktop()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(path)) throw new IOException("无法确定桌面目录，请检查 Windows 桌面配置。");
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("桌面目录不存在。");
        return path;
    }

    public static string CreateBatchFolder()
    {
        var desktop = Desktop();
        var stem = "去背景_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        for (var number = 0; ; number++)
        {
            var path = Path.Combine(desktop, stem + (number == 0 ? "" : $" ({number})"));
            if (Directory.Exists(path) || File.Exists(path)) continue;
            return Directory.CreateDirectory(path).FullName;
        }
    }

    public static string SaveUnique(BitmapSource bitmap, string folder, string inputPath)
    {
        var stem = Path.GetFileNameWithoutExtension(inputPath) + "-no-bg";
        for (var number = 0; ; number++)
        {
            var path = Path.Combine(folder, stem + (number == 0 ? "" : $" ({number})") + ".png");
            if (File.Exists(path) || Directory.Exists(path)) continue;
            try { BackgroundRemovalEngine.SavePng(bitmap, path); return path; }
            // Only a competing creator gets another suffix. Other IO failures remain failures.
            catch (IOException error) when ((error.HResult & 0xFFFF) is 80 or 183)
            {
                System.Diagnostics.Trace.TraceInformation("Background Remover output name occupied; trying the next suffix.");
            }
        }
    }
}
