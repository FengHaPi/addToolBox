using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AddToolBox.BackgroundRemover;

public partial class BackgroundRemoverView : UserControl
{
    private readonly BackgroundRemovalEngine _engine = new();
    private BitmapSource? _original;
    private BitmapSource? _result;
    private string? _inputPath;
    private bool _busy;

    public BackgroundRemoverView() => InitializeComponent();

    private async void OnChooseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "选择图片", Filter = "图片 (PNG / JPEG / BMP)|*.png;*.jpg;*.jpeg;*.bmp", Multiselect = false };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            await LoadImageAsync(dialog.FileName);
    }

    private async Task LoadImageAsync(string path)
    {
        if (_busy)
            return;
        SetBusy(true);
        StatusText.Text = "正在加载图片…";
        try
        {
            var image = await Task.Run(() => BackgroundRemovalEngine.LoadImage(path));
            _original = image;
            _inputPath = Path.GetFullPath(path);
            _result = null;
            OriginalPreview.Source = image;
            ResultPreview.Source = null;
            ImageDetails.Text = $"{Path.GetFileName(path)} · {image.PixelWidth} × {image.PixelHeight}";
            StatusText.Text = "图片已就绪。点击“去除背景”开始本地处理。";
        }
        catch (Exception error) { ShowError("图片加载失败", error); }
        finally { SetBusy(false); }
    }

    private async void OnProcessClick(object sender, RoutedEventArgs e)
    {
        if (_busy || _original is null)
            return;
        SetBusy(true);
        _result = null;
        ResultPreview.Source = null;
        try
        {
            var progress = new Progress<string>(message => StatusText.Text = message);
            var result = await Task.Run(() => _engine.Process(_original, progress));
            _result = result.Image;
            ResultPreview.Source = _result;
            StatusText.Text = $"完成 · 总计 {result.Total.TotalSeconds:F2}s · 模型初始化 {result.ModelInit.TotalSeconds:F2}s · " +
                $"预处理 {result.Preprocess.TotalSeconds:F2}s · 推理 {result.Inference.TotalSeconds:F2}s · 后处理 {result.Postprocess.TotalSeconds:F2}s";
        }
        catch (Exception error) { ShowError("去背景失败", error); }
        finally { SetBusy(false); }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_busy || _result is null || _inputPath is null)
            return;
        var dialog = new SaveFileDialog
        {
            Title = "保存透明 PNG", Filter = "PNG 图片|*.png", DefaultExt = ".png",
            AddExtension = true, OverwritePrompt = true,
            FileName = Path.GetFileNameWithoutExtension(_inputPath) + "-no-bg.png"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;
        SetBusy(true);
        try
        {
            if (Path.GetFullPath(dialog.FileName).Equals(_inputPath, StringComparison.OrdinalIgnoreCase))
                throw new IOException("请另选文件名，不能覆盖当前原图。");
            await Task.Run(() => BackgroundRemovalEngine.SavePng(_result, dialog.FileName));
            StatusText.Text = "透明 PNG 已保存。";
        }
        catch (Exception error) { ShowError("保存失败", error); }
        finally { SetBusy(false); }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !_busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (_busy)
            return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files
            && files.FirstOrDefault(BackgroundRemovalEngine.IsSupportedImage) is { } path)
            await LoadImageAsync(path);
        else
            StatusText.Text = "未找到可用图片。V0.1 支持 PNG、JPEG/JPG、BMP。";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        ChooseButton.IsEnabled = !busy;
        ProcessButton.IsEnabled = !busy && _original is not null;
        SaveButton.IsEnabled = !busy && _result is not null;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowError(string title, Exception error)
    {
        StatusText.Text = $"{title}：{error.Message}";
        MessageBox.Show(Window.GetWindow(this), error.ToString(), title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
