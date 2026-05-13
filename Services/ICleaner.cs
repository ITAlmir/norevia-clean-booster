using WindowsCleanBooster.Models;
namespace WindowsCleanBooster.Services;

public interface ICleaner
{
    Task<(long bytes, int files)> ScanAsync(CleanTarget target, CancellationToken ct);
    Task<CleanItemResult> CleanAsync(CleanTarget target, IProgress<double>? progress, CancellationToken ct);
}