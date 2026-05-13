namespace WindowsCleanBooster.Models;

public sealed class CleanSummary
{
    public long TotalFreedBytes { get; set; }
    public int TotalDeletedFiles { get; set; }
    public int TotalFailedFiles { get; set; }
    public TimeSpan TotalDuration { get; set; }

    public List<CleanItemResult> Items { get; } = new();
}