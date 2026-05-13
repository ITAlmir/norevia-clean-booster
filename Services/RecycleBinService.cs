using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsCleanBooster.Models;
namespace WindowsCleanBooster.Services;

public sealed class RecycleBinService
{
    private readonly ILogger _log;
    public RecycleBinService(ILogger log) => _log = log;

    public Task<long> GetEstimatedAsync(CancellationToken ct)
    {
        // Light verzija: ne skeniramo detaljno (može kasnije)
        return Task.FromResult(0L);
    }

    public Task<CleanItemResult> EmptyAsync(CleanTarget target, Stopwatch sw, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // SHEmptyRecycleBin
        // flags: no confirmation + no progress UI + no sound
        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const uint SHERB_NOPROGRESSUI = 0x00000002;
        const uint SHERB_NOSOUND = 0x00000004;

        int hr = SHEmptyRecycleBin(IntPtr.Zero, null,
            SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

        bool ok = hr == 0;
        if (!ok) _log.Warn($"SHEmptyRecycleBin failed hr={hr}");

        return Task.FromResult(new CleanItemResult
        {
            TargetId = target.Id,
            TargetName = target.Name,
            Success = ok,
            Message = ok ? "Recycle Bin emptied." : $"Recycle Bin failed (code {hr}).",
            Duration = sw.Elapsed
        });
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}