namespace WindowsCleanBooster.Models;

public sealed class CleanItemResult
{
    public string TargetId { get; init; } = "";
    public string TargetName { get; init; } = "";

    public bool Success { get; init; }
    public string Message { get; init; } = "";

    public long FreedBytes { get; init; }
    public int DeletedFiles { get; init; }
    public int FailedFiles { get; init; }

    public TimeSpan Duration { get; init; }
}