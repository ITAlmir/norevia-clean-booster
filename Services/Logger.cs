using System.Diagnostics;

namespace WindowsCleanBooster.Services;

public interface ILogger
{
    void Info(string msg);
    void Warn(string msg);
    void Error(string msg, Exception? ex = null);
}

public sealed class Logger : ILogger
{
    public void Info(string msg) => Debug.WriteLine($"[INFO] {msg}");
    public void Warn(string msg) => Debug.WriteLine($"[WARN] {msg}");
    public void Error(string msg, Exception? ex = null)
        => Debug.WriteLine($"[ERROR] {msg} {(ex is null ? "" : ex)}");
}