using WindowsCleanBooster.Models;
namespace WindowsCleanBooster.Services;

public sealed class CleanEngine
{
    private readonly ICleaner _cleaner;
    private readonly ILogger _log;

    public CleanEngine(ICleaner cleaner, ILogger log)
    {
        _cleaner = cleaner;
        _log = log;
    }

    public async Task ScanAsync(IEnumerable<CleanTarget> targets, CancellationToken ct)
    {
        foreach (var t in targets)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var (bytes, files) = await _cleaner.ScanAsync(t, ct);
                t.EstimatedBytes = bytes;
                t.EstimatedFiles = files;
            }
            catch (Exception ex)
            {
                _log.Error($"Scan failed for {t.Name}", ex);
                t.EstimatedBytes = 0;
                t.EstimatedFiles = 0;
            }
        }
    }

    public async Task<CleanSummary> CleanAsync(
        IEnumerable<CleanTarget> targets,
        IProgress<(string targetId, double progress)>? progress,
        CancellationToken ct)
    {
        var summary = new CleanSummary();
        var swTotal = System.Diagnostics.Stopwatch.StartNew();

        foreach (var t in targets.Where(x => x.EnabledByDefault))
        {
            ct.ThrowIfCancellationRequested();

            var localProgress = progress is null
                ? null
                : new Progress<double>(p => progress.Report((t.Id, p)));

            var item = await _cleaner.CleanAsync(t, localProgress, ct);
            summary.Items.Add(item);

            summary.TotalFreedBytes += item.FreedBytes;
            summary.TotalDeletedFiles += item.DeletedFiles;
            summary.TotalFailedFiles += item.FailedFiles;
        }

        swTotal.Stop();
        summary.TotalDuration = swTotal.Elapsed;
        return summary;
    }
}