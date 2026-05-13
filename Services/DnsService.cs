using System.Diagnostics;
using WindowsCleanBooster.Models;
namespace WindowsCleanBooster.Services;

public sealed class DnsService
{
    private readonly ILogger _log;
    public DnsService(ILogger log) => _log = log;

    public async Task<CleanItemResult> FlushAsync(CleanTarget target, Stopwatch sw, CancellationToken ct)
    {
        // radi i bez admina u mnogim slučajevima, ali nekad traži admin
        var psi = new ProcessStartInfo
        {
            FileName = "ipconfig",
            Arguments = "/flushdns",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var p = Process.Start(psi)!;

        await p.WaitForExitAsync(ct);

        var output = await p.StandardOutput.ReadToEndAsync();
        var error = await p.StandardError.ReadToEndAsync();

        bool ok = p.ExitCode == 0;
        if (!ok) _log.Warn($"DNS flush exit={p.ExitCode}, err={error}");

        return new CleanItemResult
        {
            TargetId = target.Id,
            TargetName = target.Name,
            Success = ok,
            Message = ok ? "DNS cache flushed." : (string.IsNullOrWhiteSpace(error) ? output : error),
            Duration = sw.Elapsed
        };
    }
}