using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AddToolBox.BackgroundRemover;

public partial class BackgroundRemoverView : UserControl
{
    private enum ViewState { Idle, Loading, ReadySingle, ReadyBatch, ProcessingSingle, ProcessingBatch,
        Stopping, Saving, ChangingBackend, Completed, Error }
    private readonly BackgroundRemovalEngine _engine = new();
    private ViewState _state;
    private string[] _paths = [];
    private BitmapSource? _original;
    private BitmapSource? _result;
    private CancellationTokenSource? _stop;
    private double _currentSeconds;
    private double _perSecond;
    private int _completed;
    private BatchPreviewItem[] _batchItems = [];
    private readonly LinkedList<BatchPreviewItem> _thumbnailCache = [];
    private int _batchGeneration;
    private int _previewVersion;
    private bool _thumbnailLoading;
    private bool _previewLoading;
    private bool _selectingCurrent;
    private BatchPreviewItem? _pendingPreview;
    private Task _previewWork = Task.CompletedTask;
    private bool IsBusy => _state is ViewState.Loading or ViewState.ProcessingSingle or ViewState.ProcessingBatch
        or ViewState.Stopping or ViewState.Saving or ViewState.ChangingBackend;

    public BackgroundRemoverView()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (_performanceEnabled) StartSampling(); _ = LoadVisibleThumbnailsAsync(); };
        Unloaded += (_, _) => { StopSampling(); ClearThumbnailCache(); _previewVersion++; _pendingPreview = null; };
    }

    private async void OnChooseClick(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        var dialog = new OpenFileDialog { Title = "选择图片", Filter = "图片 (PNG / JPEG / BMP)|*.png;*.jpg;*.jpeg;*.bmp", Multiselect = true };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) await LoadPathsAsync(dialog.FileNames);
    }

    private async Task LoadPathsAsync(IEnumerable<string> inputs)
    {
        if (IsBusy) return;
        SetState(ViewState.Loading);
        SetStatus("正在读取图片列表…");
        try
        {
            var paths = await Task.Run(() => ImageFiles.Collect(inputs));
            if (paths.Length == 0) throw new NotSupportedException("未找到 PNG、JPEG/JPG 或 BMP 图片。");
            var original = paths.Length == 1 ? await Task.Run(() => BackgroundRemovalEngine.LoadImage(paths[0])) : null;
            _paths = paths;
            ResetBatchPreview(paths);
            _original = original;
            _result = null;
            OriginalPreview.Source = original;
            ResultPreview.Source = null;
            _completed = 0;
            _currentSeconds = _perSecond = 0;
            ImageDetails.Text = original is not null
                ? $"{Path.GetFileName(paths[0])} · {original.PixelWidth} × {original.PixelHeight}"
                : $"已选择 {paths.Length} 张";
            SetState(paths.Length == 1 ? ViewState.ReadySingle : ViewState.ReadyBatch);
            if (paths.Length > 1)
            {
                ThumbnailList.SelectedIndex = 0;
                await _previewWork;
            }
            SetStatus(paths.Length == 1 ? "图片已就绪。点击“去除背景”开始。" : "点击“去除背景”开始，结果将自动保存到桌面新文件夹。");
            UpdatePerformanceValues();
        }
        catch (Exception error) { ShowError("读取图片失败", error); }
    }

    private async void OnProcessClick(object sender, RoutedEventArgs e)
    {
        if (_state == ViewState.ProcessingBatch)
        {
            _stop!.Cancel();
            SetState(ViewState.Stopping);
            SetStatus("正在停止…当前已开始的推理完成后停止后续任务。");
            return;
        }
        if (IsBusy || _paths.Length == 0) return;
        if (_paths.Length > 1) await ProcessBatchAsync();
        else await ProcessSingleAsync();
    }

    private async Task ProcessSingleAsync()
    {
        if (_original is null) return;
        SetState(ViewState.ProcessingSingle);
        _result = null;
        ResultPreview.Source = null;
        try
        {
            var progress = new Progress<string>(SetStatus);
            var result = await Task.Run(() => _engine.Process(_original, progress));
            _result = result.Image;
            ResultPreview.Source = _result;
            _currentSeconds = result.Total.TotalSeconds;
            _perSecond = 1 / Math.Max(_currentSeconds, 0.001);
            _completed = 1;
            SetState(ViewState.Completed);
            SetStatus($"完成 · {_currentSeconds:F2} s");
            UpdatePerformanceValues();
        }
        catch (Exception error) { ShowError("去背景失败", error); }
    }

    private async Task ProcessBatchAsync()
    {
        SetState(ViewState.ProcessingBatch);
        _stop = new CancellationTokenSource();
        _original = _result = null;
        ResultPreview.Source = null;
        _previewVersion++;
        _pendingPreview = null;
        foreach (var item in _batchItems) item.Status = "等待";
        _completed = 0;
        _currentSeconds = _perSecond = 0;
        try
        {
            var folder = await Task.Run(ImageFiles.CreateBatchFolder);
            var progress = new Progress<string>(SetStatus);
            var summary = await Task.Run(() => BatchRemoval.RunAsync(_paths, folder, _engine, _stop.Token,
                update => Dispatcher.InvokeAsync(() => ApplyBatchUpdate(update)).Task, progress));
            var stopped = _stop.IsCancellationRequested || summary.BackendStopped;
            _perSecond = summary.Success / Math.Max(summary.Elapsed.TotalSeconds, 0.001);
            ImageDetails.Text = $"已选择 {_paths.Length} 张 · 成功 {summary.Success} · 失败 {summary.Failed} · 未处理 {summary.Unprocessed}";
            SetState(summary.BackendStopped ? ViewState.Error : ViewState.Completed);
            SetStatus($"{(stopped ? "已停止" : "完成")} · 成功 {summary.Success} · 失败 {summary.Failed} · 未处理 {summary.Unprocessed}" +
                $" · 桌面/{Path.GetFileName(folder)}" + (summary.FailureLogError is null ? "" : $" · {summary.FailureLogError}"));
            UpdatePerformanceValues();
        }
        catch (Exception error) { ShowError("批处理失败", error); }
        finally
        {
            _stop.Dispose(); _stop = null;
            foreach (var item in _batchItems) if (item.Status == "处理中") item.Status = "等待";
            TrimThumbnailCache(keepVisibleOnly: true);
        }
    }

    private void ApplyBatchUpdate(BatchUpdate update)
    {
        var item = _batchItems[update.Index - 1];
        if (update.Original is not null || update.Message is not null)
        {
            _previewVersion++;
            _pendingPreview = null;
            _selectingCurrent = true;
            try { ThumbnailList.SelectedItem = item; ThumbnailList.ScrollIntoView(item); }
            finally { _selectingCurrent = false; }
            item.Status = update.Message is null ? "处理中" : "失败";
            OriginalPreview.Source = update.Original ?? item.Thumbnail;
            OriginalPreviewMessage.Text = "此图片无法处理";
            OriginalPreviewMessage.Visibility = OriginalPreview.Source is null ? Visibility.Visible : Visibility.Collapsed;
        }
        if (update.Result is not null) item.Status = "成功";
        if (update.Result is not null) ResultPreview.Source = update.Result.Image;
        _completed = update.Success + update.Failed;
        if (update.CurrentSeconds > 0) _currentSeconds = update.CurrentSeconds;
        _perSecond = update.PerSecond;
        BusyIndicator.Value = _completed;
        ImageDetails.Text = $"已选择 {update.Total} 张 · 正在处理 {update.Index} / {update.Total} · 成功 {update.Success} · 失败 {update.Failed}";
        if (_state != ViewState.Stopping) SetStatus(update.Message ?? $"正在处理 {Path.GetFileName(_paths[update.Index - 1])}");
        UpdatePerformanceValues();
    }

    // UI projection of the sequential worker's updates; no decoded result collection.
    private sealed class BatchPreviewItem(string path, int generation) : INotifyPropertyChanged
    {
        public string Path { get; } = path;
        public string Name => System.IO.Path.GetFileName(Path);
        public int Generation { get; } = generation;
        public bool ThumbnailFailed { get; set; }
        private string _status = "等待";
        public string Status { get => _status; set { _status = value; Changed(nameof(Status)); } }
        private BitmapSource? _thumbnail;
        public BitmapSource? Thumbnail { get => _thumbnail; set { _thumbnail = value; Changed(nameof(Thumbnail)); Changed(nameof(ThumbnailMessage)); } }
        public string ThumbnailMessage => Thumbnail is not null ? "" : ThumbnailFailed ? "无法预览" : "载入中…";
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    private void ResetBatchPreview(string[] paths)
    {
        _batchGeneration++;
        _previewVersion++;
        _pendingPreview = null;
        ClearThumbnailCache();
        _batchItems = paths.Length > 1 ? paths.Select(path => new BatchPreviewItem(path, _batchGeneration)).ToArray() : [];
        ThumbnailList.ItemsSource = _batchItems;
        ThumbnailList.Visibility = paths.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
        OriginalPreviewMessage.Visibility = Visibility.Collapsed;
    }

    private void OnThumbnailLoaded(object sender, RoutedEventArgs e)
    {
        _ = LoadVisibleThumbnailsAsync();
    }

    private void OnThumbnailViewportChanged(object sender, ScrollChangedEventArgs e) => _ = LoadVisibleThumbnailsAsync();

    private void OnThumbnailContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (((FrameworkElement)sender).IsLoaded) _ = LoadVisibleThumbnailsAsync();
    }

    // Recycling does not guarantee a matching Loaded/Unloaded pair per data item.
    // The generated containers and viewport are the authority for thumbnail demand.
    private BatchPreviewItem[] VisibleThumbnailItems()
    {
        var items = new List<BatchPreviewItem>();
        var pending = new Stack<DependencyObject>();
        pending.Push(ThumbnailList);
        var viewport = new Rect(0, 0, ThumbnailList.ActualWidth, ThumbnailList.ActualHeight);
        while (pending.TryPop(out var visual))
        {
            if (visual is ListBoxItem container)
            {
                if (container.IsVisible && ThumbnailList.ItemContainerGenerator.ItemFromContainer(container) is BatchPreviewItem item
                    && item.Generation == _batchGeneration
                    && container.TransformToAncestor(ThumbnailList).TransformBounds(new Rect(container.RenderSize)).IntersectsWith(viewport))
                    items.Add(item);
                continue;
            }
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++) pending.Push(VisualTreeHelper.GetChild(visual, i));
        }
        return items.ToArray();
    }

    // One thumbnail decode at a time, chosen from currently realized containers.
    // Rapid scrolling never queues a decode Task for every path.
    private async Task LoadVisibleThumbnailsAsync()
    {
        if (_thumbnailLoading) return;
        _thumbnailLoading = true;
        try
        {
            while (IsLoaded)
            {
                var item = VisibleThumbnailItems().FirstOrDefault(x => x.Thumbnail is null && !x.ThumbnailFailed);
                if (item is null) break;
                BitmapSource? thumbnail = null;
                try { thumbnail = await Task.Run(() => LoadPreview(item.Path, 160)); }
                catch (Exception error)
                {
                    Trace.TraceError("Background Remover thumbnail decode failed: {0}", error);
                    item.ThumbnailFailed = true;
                }
                if (item.Generation != _batchGeneration || !IsLoaded) continue;
                if (VisibleThumbnailItems().Contains(item))
                {
                    item.Thumbnail = thumbnail;
                    if (thumbnail is not null) { _thumbnailCache.Remove(item); _thumbnailCache.AddLast(item); }
                    TrimThumbnailCache(keepVisibleOnly: false);
                }
            }
        }
        finally { _thumbnailLoading = false; }
    }

    private void TrimThumbnailCache(bool keepVisibleOnly)
    {
        var visible = VisibleThumbnailItems();
        foreach (var item in _thumbnailCache.ToArray())
        {
            if (!keepVisibleOnly && _thumbnailCache.Count <= 32) break;
            if (visible.Contains(item)) continue;
            item.Thumbnail = null;
            _thumbnailCache.Remove(item);
        }
    }

    private void ClearThumbnailCache()
    {
        foreach (var item in _thumbnailCache) item.Thumbnail = null;
        _thumbnailCache.Clear();
    }

    private void OnThumbnailSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_selectingCurrent || ThumbnailList.SelectedItem is not BatchPreviewItem item) return;
        _previewVersion++;
        _pendingPreview = item;
        OriginalPreview.Source = item.Thumbnail;
        OriginalPreviewMessage.Text = "正在载入预览…";
        OriginalPreviewMessage.Visibility = item.Thumbnail is null ? Visibility.Visible : Visibility.Collapsed;
        if (!_previewLoading)
        {
            _previewLoading = true;
            _previewWork = LoadSelectedPreviewAsync();
        }
    }

    // Coalesce rapid selection changes into the latest request; at most one preview decode.
    private async Task LoadSelectedPreviewAsync()
    {
        try
        {
            while (_pendingPreview is { } item)
            {
                _pendingPreview = null;
                var version = _previewVersion;
                try
                {
                    var preview = await Task.Run(() => LoadPreview(item.Path, 1280));
                    if (version != _previewVersion || item.Generation != _batchGeneration) continue;
                    OriginalPreview.Source = preview;
                    OriginalPreviewMessage.Visibility = Visibility.Collapsed;
                }
                catch (Exception error)
                {
                    Trace.TraceError("Background Remover selected preview failed: {0}", error);
                    if (version != _previewVersion) continue;
                    OriginalPreviewMessage.Text = "无法预览此图片，批处理时将记录失败";
                    OriginalPreviewMessage.Visibility = Visibility.Visible;
                }
            }
        }
        finally { _previewLoading = false; }
    }

    private static BitmapSource LoadPreview(string path, int maxSide)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
        if (decoder is not PngBitmapDecoder and not JpegBitmapDecoder and not BmpBitmapDecoder)
            throw new NotSupportedException("不支持的图片预览格式。");
        var frame = decoder.Frames[0];
        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        stream.Position = 0;
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        if (width >= height) bitmap.DecodePixelWidth = Math.Min(maxSide, width);
        else bitmap.DecodePixelHeight = Math.Min(maxSide, height);
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _result is null || _paths.Length != 1) return;
        SetState(ViewState.Saving);
        try
        {
            var clock = Stopwatch.StartNew();
            var path = await Task.Run(() => ImageFiles.SaveUnique(_result, ImageFiles.Desktop(), _paths[0]));
            Trace.TraceInformation("Background Remover save time: {0:F3} s", clock.Elapsed.TotalSeconds);
            SetState(ViewState.Completed);
            SetStatus($"已保存到桌面 · {Path.GetFileName(path)}");
        }
        catch (Exception error) { ShowError("保存失败", error); }
    }

    private async void OnBackendChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || IsBusy) return;
        var mode = (BackendMode)BackendSelector.SelectedIndex;
        SetState(ViewState.ChangingBackend);
        try
        {
            await Task.Run(() => _engine.SetBackend(mode));
            SetState(_paths.Length == 0 ? ViewState.Idle : _paths.Length == 1 ? ViewState.ReadySingle : ViewState.ReadyBatch);
            SetStatus("设备已选择，下次处理时加载。");
            UpdatePerformanceValues();
        }
        catch (Exception error) { ShowError("设备切换失败", error); }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !IsBusy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!IsBusy && e.Data.GetData(DataFormats.FileDrop) is string[] files) await LoadPathsAsync(files);
    }

    private void SetState(ViewState state)
    {
        _state = state;
        ChooseButton.IsEnabled = BackendSelector.IsEnabled = !IsBusy;
        ProcessButton.IsEnabled = _state == ViewState.ProcessingBatch || (!IsBusy && _paths.Length > 0);
        ProcessButton.Content = _state is ViewState.ProcessingBatch or ViewState.Stopping ? "停止" : "去除背景";
        SaveButton.IsEnabled = !IsBusy && _paths.Length == 1 && _result is not null;
        BusyIndicator.Visibility = IsBusy ? Visibility.Visible : Visibility.Collapsed;
        BusyIndicator.IsIndeterminate = _state is not (ViewState.ProcessingBatch or ViewState.Stopping);
        BusyIndicator.Maximum = Math.Max(1, _paths.Length);
        BusyIndicator.Value = _completed;
    }

    private void SetStatus(string message) => StatusText.Text = _engine.UsedCpuFallback
        && !message.StartsWith("GPU 不可用", StringComparison.Ordinal)
        ? "GPU 不可用，已自动切换 CPU · " + message : message;

    private void ShowError(string title, Exception error)
    {
        Trace.TraceError("Background Remover {0}: {1}", title, error);
        SetState(ViewState.Error);
        SetStatus($"{title}：{error.Message}");
    }
}
