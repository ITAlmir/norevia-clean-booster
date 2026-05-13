using System.Diagnostics;
using System.IO;
using WindowsCleanBooster.Models;
namespace WindowsCleanBooster.Services;

public sealed class FileSystemCleaner : ICleaner
{
    private readonly ILogger _log;
    private readonly RecycleBinService _recycle;
    private readonly DnsService _dns;

    public FileSystemCleaner(ILogger log, RecycleBinService recycle, DnsService dns)
    {
        _log = log;
        _recycle = recycle;
        _dns = dns;
    }

    public async Task<(long bytes, int files)> ScanAsync(CleanTarget target, CancellationToken ct)
    {
        return target.Type switch
        {
            CleanTargetType.Folder => await Task.Run(() => ScanFolder(target.Path, ct), ct),
            CleanTargetType.RecycleBin => (await _recycle.GetEstimatedAsync(ct), 0),
            CleanTargetType.DnsCache => (0, 0),
            _ => (0, 0)
        };
    }

    public async Task<CleanItemResult> CleanAsync(CleanTarget target, IProgress<double>? progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            return target.Type switch
            {
                CleanTargetType.Folder => await Task.Run(() => CleanFolder(target, progress, ct), ct),
                CleanTargetType.RecycleBin => await _recycle.EmptyAsync(target, sw, ct),
                CleanTargetType.DnsCache => await _dns.FlushAsync(target, sw, ct),
                _ => new CleanItemResult { TargetId = target.Id, TargetName = target.Name, Success = false, Message = "Unsupported target." }
            };
        }
        catch (OperationCanceledException)
        {
            return new CleanItemResult
            {
                TargetId = target.Id,
                TargetName = target.Name,
                Success = false,
                Message = "Canceled by user.",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            _log.Error($"Clean failed for {target.Name}", ex);
            return new CleanItemResult
            {
                TargetId = target.Id,
                TargetName = target.Name,
                Success = false,
                Message = ex.Message,
                Duration = sw.Elapsed
            };
        }
        finally
        {
            sw.Stop();
        }
    }

    private static (long bytes, int files) ScanFolder(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return (0, 0);

        long bytes = 0;
        int files = 0;

        foreach (var file in EnumerateFilesSafe(path))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var fi = new FileInfo(file);
                if (!fi.Exists) continue;

                bytes += fi.Length;
                files++;
            }
            catch { /* ignore */ }
        }

        return (bytes, files);
    }

    private CleanItemResult CleanFolder(CleanTarget target, IProgress<double>? progress, CancellationToken ct)
    {
        string? path = target.Path;

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return new CleanItemResult
            {
                TargetId = target.Id,
                TargetName = target.Name,
                Success = true,
                Message = "Path not found (nothing to clean).",
                FreedBytes = 0,
                DeletedFiles = 0,
                FailedFiles = 0,
                Duration = TimeSpan.Zero
            };
        }

        // pre-scan for progress denominator (lightweight)
        var allFiles = EnumerateFilesSafe(path).ToList();
        int total = allFiles.Count;
        int processed = 0;

        long freed = 0;
        int deleted = 0;
        int failed = 0;

        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var fi = new FileInfo(file);
                if (!fi.Exists) { processed++; continue; }

                long size = fi.Length;

                // pokušaj ukloniti read-only
                fi.IsReadOnly = false;

                fi.Delete();

                freed += size;
                deleted++;
            }
            catch
            {
                failed++;
            }
            finally
            {
                processed++;
                progress?.Report(total == 0 ? 1.0 : (double)processed / total);
            }
        }

        // opcionalno: obriši prazne foldere
        TryDeleteEmptyDirs(path);

        return new CleanItemResult
        {
            TargetId = target.Id,
            TargetName = target.Name,
            Success = true,
            Message = failed == 0 ? "Clean complete." : $"Clean complete with {failed} failed files.",
            FreedBytes = freed,
            DeletedFiles = deleted,
            FailedFiles = failed,
            Duration = TimeSpan.Zero // engine mjeri, ali možeš ostaviti; ili izmjeri ovdje posebno
        };
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> subDirs = Array.Empty<string>();
            IEnumerable<string> files = Array.Empty<string>();

            try { files = Directory.EnumerateFiles(dir); } catch { }
            foreach (var f in files) yield return f;

            try { subDirs = Directory.EnumerateDirectories(dir); } catch { }
            foreach (var sd in subDirs) stack.Push(sd);
        }
    }

    private static void TryDeleteEmptyDirs(string root)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.Length))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir, false);
                }
                catch { }
            }
        }
        catch { }
    }
}