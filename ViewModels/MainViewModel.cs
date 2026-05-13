using Project2Apps.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using WindowsCleanBooster.Models;
using WindowsCleanBooster.Services;

namespace WindowsCleanBooster.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{

    private readonly ILogger _log;
    private readonly CleanEngine _engine;

    private CancellationTokenSource? _cts;

    public ObservableCollection<CleanTarget> Targets { get; } =
        new ObservableCollection<CleanTarget>(CleanTargetsProvider.GetDefaultTargets());

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); UpdateCommands(); }
    }

    private string _status = "Ready.";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private long _estimatedBytes;
    public long EstimatedBytes
    {
        get => _estimatedBytes;
        set
        {
            _estimatedBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedBytesText));
        }
    }

    private double _overallProgress;
    public double OverallProgress
    {
        get => _overallProgress;
        set { _overallProgress = value; OnPropertyChanged(); }
    }

    public RelayCommand ScanCommand { get; }
    public RelayCommand CleanCommand { get; }
    public RelayCommand CancelCommand { get; }

    public MainViewModel()
    {
        _log = new Logger();

        var recycle = new RecycleBinService(_log);
        var dns = new DnsService(_log);
        var cleaner = new FileSystemCleaner(_log, recycle, dns);
        _engine = new CleanEngine(cleaner, _log);

        ScanCommand = new RelayCommand(() => _ = ScanAsync(), () => !IsBusy);
        CleanCommand = new RelayCommand(() => _ = CleanAsync(), () => !IsBusy);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        foreach (var t in Targets)
        {
            t.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CleanTarget.EnabledByDefault))
                    OnPropertyChanged(nameof(ModeText));
            };
        }
    }

    private void UpdateCommands()
    {
        ScanCommand.RaiseCanExecuteChanged();
        CleanCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    private async Task ScanAsync()
    {
        Log("Scan started");
        SetStatus("Scanning... preparing...", StatusKind.Neutral);
        IsBusy = true;
        Status = "Scanning... preparing...";
        OverallProgress = 0;
        EstimatedBytes = 0;

        _cts = new CancellationTokenSource();

        try
        {
            // UX: short warmup so it doesn't feel instant
            await Task.Delay(400, _cts.Token);

            Status = "Scanning targets...";
            await _engine.ScanAsync(Targets, _cts.Token);

            // UX: give time for UI to breathe (feels more 'real')
            await Task.Delay(600, _cts.Token);

            EstimatedBytes = Targets.Where(t => t.EnabledByDefault).Sum(t => t.EstimatedBytes);

            // Show finished state briefly then reset
            OverallProgress = 1.0;
            Status = $"Scan complete. Estimated: {FormatBytes(EstimatedBytes)}";
            await Task.Delay(500, _cts.Token);
            OverallProgress = 0;
        }
        catch (OperationCanceledException)
        {
            Status = "Scan canceled.";
            OverallProgress = 0;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
        Log("Scan finished");
        SetStatus($"Scan complete. Estimated: {FormatBytes(EstimatedBytes)}", StatusKind.Success);

    }

    private async Task CleanAsync()
    {
        SetStatus("Cleaning... please wait...", StatusKind.Neutral);
        Log("Clean started");
        IsBusy = true;
        Status = "Cleaning...";
        OverallProgress = 0;

        _cts = new CancellationTokenSource();

        var active = Targets.Where(t => t.EnabledByDefault).ToList();
        var weights = active.Count == 0 ? 1 : active.Count;

        var perTargetProgress = new Dictionary<string, double>();

        var progress = new Progress<(string targetId, double progress)>(p =>
        {
            perTargetProgress[p.targetId] = p.progress;
            OverallProgress = perTargetProgress.Values.Sum() / weights;
        });

        try
        {
            await Task.Delay(500, _cts.Token);

            var summary = await _engine.CleanAsync(active, progress, _cts.Token);

            OverallProgress = 1.0;
            await Task.Delay(1100, _cts.Token); // duži "enterprise" feel

            if (summary.TotalFailedFiles == 0)
            {
                SetStatus(
                    $"Cleanup complete ✅  Freed {FormatBytes(summary.TotalFreedBytes)}. Everything selected was cleaned successfully.",
                    StatusKind.Success);
            }
            else
            {
                SetStatus(
                    $"Cleanup complete ✅  Freed {FormatBytes(summary.TotalFreedBytes)}. " +
                    $"Skipped (in use / locked): {summary.TotalFailedFiles}. " +
                    $"Tip: Close browsers/apps and run again for best results.",
                    StatusKind.Warning);
            }

            // keep final message visible a bit
            await Task.Delay(900, _cts.Token);
            OverallProgress = 0;
            Log($"Clean finished: Freed {FormatBytes(summary.TotalFreedBytes)}");

        }
        catch (OperationCanceledException)
        {
            SetStatus("Clean canceled.", StatusKind.Warning);
            OverallProgress = 0;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static string FormatBytes(long n)
    {
        if (n <= 0) return "0 B";
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = n;
        int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v:0.##} {u[i]}";
    }
    public string EstimatedBytesText => FormatBytes(EstimatedBytes);

    private bool _prefetchEnabled;
    public bool PrefetchEnabled
    {
        get => _prefetchEnabled;
        set
        {
            if (_prefetchEnabled == value) return;
            _prefetchEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModeText));
        }
    }

    public ObservableCollection<string> ActivityLog { get; } = new();
    private void Log(string message)
    {
        ActivityLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (ActivityLog.Count > 200) ActivityLog.RemoveAt(0);
    }

    public enum StatusKind
    {
        Neutral,
        Success,
        Warning,
        Error
    }

    private StatusKind _statusKind = StatusKind.Neutral;
    public StatusKind StatusKindValue
    {
        get => _statusKind;
        set { _statusKind = value; OnPropertyChanged(); }
    }

    private void SetStatus(string text, StatusKind kind = StatusKind.Neutral)
    {
        Status = text;
        StatusKindValue = kind;
    }

    public void RefreshModeText() => OnPropertyChanged(nameof(ModeText));

    public string ModeText
    {
        get
        {
            var prefetch = Targets.FirstOrDefault(t =>
                (t.Id?.Equals("prefetch", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Name?.Contains("Prefetch", StringComparison.OrdinalIgnoreCase) ?? false));

            var on = prefetch?.EnabledByDefault == true;
            return on ? "Light (Prefetch ON)" : "Light (Safe)";
        }
    }
}