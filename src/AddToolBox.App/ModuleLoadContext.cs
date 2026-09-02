using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AddToolBox.SDK;

namespace AddToolBox.App;

internal sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _root;
    private static readonly Assembly SharedSdk = typeof(IAddToolBoxModuleV1).Assembly;
    private static readonly HashSet<string> FrameworkNames = GetFrameworkNames();

    public ModuleLoadContext(string id, string root, string entryPath) : base($"Module:{id}", isCollectible: true)
    {
        _root = root;
        _resolver = new AssemblyDependencyResolver(entryPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == SharedSdk.GetName().Name)
            return SharedSdk;
        if (assemblyName.Name is not null && FrameworkNames.Contains(assemblyName.Name))
            return null; // Deliberate framework/SDK sharing; never load private WPF copies.
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is not null)
            return LoadFromAssemblyPath(ValidateResolvedPath(path));
        if (assemblyName.Name?.EndsWith(".resources", StringComparison.Ordinal) == true)
            return null; // Normal ResourceManager culture probing.
        throw new FileNotFoundException($"模组私有依赖无法解析：{assemblyName.FullName}");
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        // Unresolved names retain OS native-library resolution (e.g. kernel32).
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(ValidateResolvedPath(path));
    }

    private string ValidateResolvedPath(string path) =>
        ModuleManifest.ResolvePackagePath(_root, Path.GetRelativePath(_root, path));

    private static HashSet<string> GetFrameworkNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var wpfDirectory = Path.GetDirectoryName(typeof(System.Windows.FrameworkElement).Assembly.Location)!;
        foreach (var directory in new[] { runtimeDirectory, wpfDirectory })
            foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
                names.Add(Path.GetFileNameWithoutExtension(path));
        return names;
    }
}
