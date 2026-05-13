using WindowsCleanBooster.Models;
using System.IO;

namespace Project2Apps.Models;

public static class CleanTargetsProvider
{
    public static List<CleanTarget> GetDefaultTargets()
    {
        string? userTemp = Environment.GetEnvironmentVariable("TEMP");
        string windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

        return new List<CleanTarget>
        {
            new() { Name = "User Temp", Type = CleanTargetType.Folder, Path = userTemp, EnabledByDefault = true },
            new() { Name = "Windows Temp", Type = CleanTargetType.Folder, Path = windowsTemp, EnabledByDefault = true },
            new() { Name = "Prefetch (Light)", Type = CleanTargetType.Folder, Path = prefetch, EnabledByDefault = false }, // default OFF (osjetljivije)
            new() { Name = "Recycle Bin", Type = CleanTargetType.RecycleBin, EnabledByDefault = true },
            new() { Name = "DNS Cache", Type = CleanTargetType.DnsCache, EnabledByDefault = true },
        };
    }
}