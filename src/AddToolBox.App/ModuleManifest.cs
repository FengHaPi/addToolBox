using System.IO;
using System.Text.Json;

namespace AddToolBox.App;

// The manifest is the authority; ToolDefinition is only its immutable UI projection.
internal sealed record ModuleManifest(
    string SchemaVersion, string Id, string DisplayName, string Version,
    string Kind, string EntryAssembly, string EntryType)
{
    public static ModuleManifest Read(string moduleRoot)
    {
        var root = Path.GetFullPath(moduleRoot);
        RejectLink(root);
        var manifestPath = ResolvePackagePath(root, "module.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] fields = ["schemaVersion", "id", "displayName", "version", "kind", "entryAssembly", "entryType"];
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("module.json 必须是 JSON 对象。");
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!fields.Contains(property.Name, StringComparer.Ordinal)
                || property.Value.ValueKind != JsonValueKind.String
                || !properties.TryAdd(property.Name, property.Value.GetString()!))
                throw new InvalidDataException($"Manifest 字段无效、重复或不受 V1 支持：{property.Name}");
        }
        foreach (var field in fields)
        {
            if (!properties.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Manifest 缺少非空字段：{field}");
        }
        var result = new ModuleManifest(properties[fields[0]], properties[fields[1]],
            properties[fields[2]], properties[fields[3]], properties[fields[4]],
            properties[fields[5]], properties[fields[6]]);
        if (result.SchemaVersion != "addtoolbox-module-v1" || result.Kind != "tool")
            throw new InvalidDataException("仅支持 addtoolbox-module-v1 / kind=tool。");
        var id = result.Id;
        var deviceName = id.Split('.')[0];
        if (id.Contains("..", StringComparison.Ordinal) || id.Contains('/') || id.Contains('\\')
            || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || id != id.Trim() || id.EndsWith('.') || id.StartsWith(".importing-", StringComparison.OrdinalIgnoreCase)
            || new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(deviceName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Module Id 不是安全的 Windows 目录名称。");
        ResolvePackagePath(root, result.EntryAssembly);
        return result;
    }

    public static string ResolvePackagePath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':'))
            throw new InvalidDataException("Package 路径必须是相对路径。");
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var path = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package 路径不得逃出 Module Root。");
        // Reject junctions/symlinks as well as lexical traversal, including intermediate directories.
        RejectLink(fullRoot);
        var current = fullRoot;
        foreach (var part in Path.GetRelativePath(fullRoot, path).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            RejectLink(current);
        }
        if (!File.Exists(path))
            throw new FileNotFoundException("Package 文件不存在。", path);
        return path;
    }

    internal static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException($"Module Package 不接受符号链接或 Junction：{path}");
    }
}
