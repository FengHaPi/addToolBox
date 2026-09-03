using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace AddToolBox.BackgroundRemover;

public partial class BackgroundRemoverView
{
    private DispatcherTimer? _performanceTimer;
    private Process? _sampledProcess;
    private bool _performanceEnabled;
    private long _sampleTimestamp;
    private TimeSpan _sampleCpu;

    private void OnPerformanceClick(object sender, RoutedEventArgs e)
    {
        _performanceEnabled = !_performanceEnabled;
        PerformanceCard.Visibility = _performanceEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (_performanceEnabled) StartSampling(); else StopSampling();
    }

    private void StartSampling()
    {
        if (_performanceTimer is not null) return;
        _sampledProcess = Process.GetCurrentProcess();
        _sampleCpu = _sampledProcess.TotalProcessorTime;
        _sampleTimestamp = Stopwatch.GetTimestamp();
        _performanceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromSeconds(1) };
        _performanceTimer.Tick += SamplePerformance;
        _performanceTimer.Start();
        UpdatePerformanceValues();
    }

    private void StopSampling()
    {
        if (_performanceTimer is not null)
        {
            _performanceTimer.Stop();
            _performanceTimer.Tick -= SamplePerformance;
            _performanceTimer = null;
        }
        _sampledProcess?.Dispose();
        _sampledProcess = null;
    }

    private void SamplePerformance(object? sender, EventArgs e)
    {
        try
        {
            _sampledProcess!.Refresh();
            var now = Stopwatch.GetTimestamp();
            var cpu = _sampledProcess.TotalProcessorTime;
            var seconds = Stopwatch.GetElapsedTime(_sampleTimestamp, now).TotalSeconds;
            CpuValue.Text = $"{Math.Clamp((cpu - _sampleCpu).TotalSeconds / seconds / Environment.ProcessorCount * 100, 0, 100):F1} %";
            var memory = _sampledProcess.WorkingSet64;
            MemoryValue.Text = memory >= 1_000_000_000 ? $"{memory / 1_000_000_000.0:F2} GB" : $"{memory / 1_000_000.0:F0} MB";
            _sampleTimestamp = now;
            _sampleCpu = cpu;
            UpdatePerformanceValues();
        }
        catch (Exception error)
        {
            Trace.TraceError("Background Remover performance sampling failed: {0}", error);
            StopSampling();
            CpuValue.Text = MemoryValue.Text = "不可用";
        }
    }

    private void UpdatePerformanceValues()
    {
        if (!_performanceEnabled) return;
        DeviceValue.Text = _engine.ActualDevice switch { ProcessingDevice.Gpu => "GPU", ProcessingDevice.Cpu => "CPU", _ => "—" };
        CurrentValue.Text = _currentSeconds > 0 ? $"{_currentSeconds:F2} s" : "—";
        AverageValue.Text = _perSecond > 0 ? $"{_perSecond:F2} 张/s" : "—";
        ProgressLabel.Visibility = ProgressValue.Visibility = _paths.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
        ProgressValue.Text = $"{_completed} / {_paths.Length}";
    }
}
