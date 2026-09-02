using System.IO;
using System.Windows;
using AddToolBox.SDK;

namespace AddToolBox.App;

internal sealed class LoadedModule
{
    public ModuleManifest Manifest { get; }
    public string InstallPath { get; }
    public ModuleLoadContext LoadContext { get; }
    public IAddToolBoxModuleV1 Entry { get; }
    public FrameworkElement? View { get; private set; }

    private LoadedModule(ModuleManifest manifest, string installPath,
        ModuleLoadContext loadContext, IAddToolBoxModuleV1 entry)
    {
        Manifest = manifest;
        InstallPath = installPath;
        LoadContext = loadContext;
        Entry = entry;
    }

    public static LoadedModule Load(string installPath, ModuleManifest manifest)
    {
        var path = ModuleManifest.ResolvePackagePath(installPath, manifest.EntryAssembly);
        var context = new ModuleLoadContext(manifest.Id, installPath, path);
        try
        {
            var assembly = context.LoadFromAssemblyPath(path);
            var entryType = assembly.GetType(manifest.EntryType, throwOnError: true)!;
            if (!typeof(IAddToolBoxModuleV1).IsAssignableFrom(entryType) || entryType.IsAbstract)
                throw new InvalidDataException("入口类型未实现共享的 IAddToolBoxModuleV1。");
            var entry = (IAddToolBoxModuleV1)Activator.CreateInstance(entryType)!;
            return new LoadedModule(manifest, installPath, context, entry);
        }
        catch
        {
            context.Unload();
            throw; // Caller is the visible discovery/import error boundary.
        }
    }

    public FrameworkElement GetOrCreateView()
    {
        if (View is not null)
            return View;
        var type = Entry.ToolViewType;
        if (!typeof(FrameworkElement).IsAssignableFrom(type) || typeof(Window).IsAssignableFrom(type) || type.IsAbstract)
            throw new InvalidDataException("ToolViewType 必须是可实例化的 FrameworkElement，不能是 Window。");
        using var scope = LoadContext.EnterContextualReflection();
        if (Activator.CreateInstance(type) is not FrameworkElement view)
            throw new InvalidDataException("模组 View 不是 FrameworkElement。");
        View = view;
        return view;
    }
}
