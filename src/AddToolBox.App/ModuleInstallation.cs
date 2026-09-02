using System.IO;

namespace AddToolBox.App;

internal static class ModuleInstallation
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "addToolBox", "Modules");

    public static void EnsureNotInstalled(string installationRoot, ModuleManifest manifest)
    {
        var destination = Path.Combine(Path.GetFullPath(installationRoot), manifest.Id);
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException("该模组已安装，V0.1 不支持覆盖或更新。");
    }

    public static string Install(string source, ModuleManifest approvedManifest, string installationRoot)
    {
        var root = Path.GetFullPath(installationRoot);
        var sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        if (root.Equals(sourceRoot, StringComparison.OrdinalIgnoreCase)
            || root.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("安装目录不能位于所选 Package 内部。");
        Directory.CreateDirectory(root);
        ModuleManifest.RejectLink(root);
        EnsureNotInstalled(root, approvedManifest);
        var temporary = Path.Combine(root, $".importing-{Guid.NewGuid():N}");
        var destination = Path.Combine(root, approvedManifest.Id);
        try
        {
            CopyDirectory(Path.GetFullPath(source), temporary);
            if (ModuleManifest.Read(temporary) != approvedManifest)
                throw new InvalidDataException("复制期间 Manifest 发生改变；请重新导入并确认。");
            EnsureNotInstalled(root, approvedManifest);
            Directory.Move(temporary, destination);
            return destination;
        }
        catch (Exception importError)
        {
            if (Directory.Exists(temporary))
            {
                try { Directory.Delete(temporary, recursive: true); }
                catch (Exception cleanupError)
                {
                    throw new AggregateException($"导入失败，且临时目录清理失败：{temporary}", importError, cleanupError);
                }
            }
            throw;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        ModuleManifest.RejectLink(source);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            ModuleManifest.RejectLink(file);
            if (Path.GetFileName(file).Equals("AddToolBox.SDK.dll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Package 不得包含私有 AddToolBox.SDK.dll。");
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
