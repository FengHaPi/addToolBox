using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AddToolBox.Core;
using Microsoft.Win32;

namespace AddToolBox.App;

public partial class MainWindow
{
    private readonly Dictionary<string, LoadedModule> _loadedModules = new(StringComparer.OrdinalIgnoreCase);
    private bool _modulesDiscovered;
    private bool _moduleImportInProgress;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_moduleImportInProgress)
        {
            e.Cancel = true;
            MessageBox.Show(this, "正在复制模组，请等待导入完成后关闭窗口。", "导入模组", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        base.OnClosing(e);
    }

    private void DiscoverInstalledModules()
    {
        if (_modulesDiscovered)
            return;
        _modulesDiscovered = true;
        var failures = new List<string>();
        var candidates = new List<(string Path, ModuleManifest Manifest)>();
        try
        {
            if (!Directory.Exists(ModuleInstallation.Root))
                return;
            foreach (var directory in Directory.EnumerateDirectories(ModuleInstallation.Root))
            {
                if (Path.GetFileName(directory).StartsWith(".importing-", StringComparison.Ordinal))
                    continue;
                try
                {
                    var manifest = ModuleManifest.Read(directory);
                    if (!Path.GetFileName(directory).Equals(manifest.Id, StringComparison.Ordinal))
                        throw new InvalidDataException("安装目录名称与 Module Id 不一致。");
                    candidates.Add((directory, manifest));
                }
                catch (Exception error) { failures.Add($"{Path.GetFileName(directory)}：{error.GetBaseException().Message}"); }
            }
            foreach (var candidate in candidates.OrderBy(item => item.Manifest.Id, StringComparer.Ordinal))
            {
                try { RegisterModule(candidate.Path, candidate.Manifest); }
                catch (Exception error) { failures.Add($"{candidate.Manifest.Id}：{error.GetBaseException().Message}"); }
            }
        }
        catch (Exception error) { failures.Add(error.GetBaseException().Message); }
        if (failures.Count > 0)
            Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(this,
                string.Join(Environment.NewLine, failures), "部分模组加载失败", MessageBoxButton.OK, MessageBoxImage.Warning)));
    }

    private async void OnImportModuleClick(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = "选择 Module Package 文件夹", Multiselect = false };
        if (picker.ShowDialog(this) != true)
            return;
        string? installedPath = null;
        _moduleImportInProgress = true;
        ImportModuleButton.IsEnabled = false;
        try
        {
            var manifest = ModuleManifest.Read(picker.FolderName);
            if (MessageBox.Show(this,
                $"{manifest.DisplayName} ({manifest.Id}) · {manifest.Version}\n\n" +
                "模组将在 addToolBox 进程内运行。\n模组代码可以访问当前用户权限范围内的资源。\n仅导入你信任的模组。",
                "信任并导入模组？", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                return;
            ModuleInstallation.EnsureNotInstalled(ModuleInstallation.Root, manifest);
            installedPath = await Task.Run(() => ModuleInstallation.Install(picker.FolderName, manifest, ModuleInstallation.Root));
            if (!IsLoaded)
                return; // Package is complete; next startup discovers it if the window was closed during copy.
            RegisterModule(installedPath, manifest);
        }
        catch (Exception error)
        {
            var prefix = installedPath is null ? "导入模组失败" : "安装完成，但模组加载失败（完整 Package 已保留）";
            ShowModuleError(prefix, error);
        }
        finally
        {
            _moduleImportInProgress = false;
            ImportModuleButton.IsEnabled = true;
        }
    }

    private void RegisterModule(string path, ModuleManifest manifest)
    {
        if (_loadedModules.ContainsKey(manifest.Id))
            throw new InvalidDataException("重复 Module Id。");
        var position = FindModuleInitialPosition();
        var module = LoadedModule.Load(path, manifest);
        var button = new Button
        {
            Style = (Style)FindResource("ToolButtonStyle"),
            ToolTip = manifest.DisplayName,
            Content = new System.Windows.Shapes.Path
            {
                Width = 27, Height = 27, Stretch = Stretch.Uniform,
                Data = Geometry.Parse("M 1,1 L 10,1 L 10,10 L 1,10 Z M 16,1 L 25,1 L 25,10 L 16,10 Z M 1,16 L 10,16 L 10,25 L 1,25 Z M 16,16 L 25,16 L 25,25 L 16,25 Z"),
                Fill = CreateFrozenBrush(Color.FromRgb(91, 127, 214))
            }
        };
        AutomationProperties.SetName(button, manifest.DisplayName);
        _loadedModules.Add(manifest.Id, module);
        _toolDefinitionsByVisual.Add(button, new ToolDefinition(manifest.Id, manifest.DisplayName, "module"));
        _defaultToolWorldPositions.Add(manifest.Id, position);
        _preferredToolWorldPositions.Add(manifest.Id, position);
        Canvas.SetLeft(button, position.X);
        Canvas.SetTop(button, position.Y);
        WorldLayer.Children.Add(button);
        EnsureToolBuffers();
        WorldLayer.UpdateLayout();
        ValidateToolIdentityMapping();
        _worldCanvas.EnsureExpanded(GetWorkspaceSize(), new Rect(position, new Size(64, 64)));
        ApplyCameraProjection();
    }

    private Point FindModuleInitialPosition()
    {
        var center = _worldCanvas.ViewportCenterWorld;
        // Finite initial placement only. No ongoing layout, snap, or drag constraints.
        for (var ring = 0; ring <= 128; ring++)
        {
            var count = ring == 0 ? 1 : ring * 8;
            for (var index = 0; index < count; index++)
            {
                var angle = 2 * Math.PI * index / count;
                var position = new Point(center.X - 32 + 88 * ring * Math.Cos(angle),
                    center.Y - 32 + 88 * ring * Math.Sin(angle));
                var candidate = new Rect(position, new Size(64, 64));
                if (_preferredToolWorldPositions.Values.All(other => !candidate.IntersectsWith(new Rect(other, new Size(64, 64)))))
                    return position;
            }
        }
        throw new InvalidOperationException("当前中心附近没有可用的初始工具位置。");
    }

    private void ShowModuleError(string title, Exception error) =>
        MessageBox.Show(this, error.ToString(), title, MessageBoxButton.OK, MessageBoxImage.Error);
}
