using System.IO;
using System.Management;

namespace WindowsCleanBooster.Services;

public sealed record SystemInfo(string CpuName, long TotalRamMb, long FreeDiskGb, long TotalDiskGb);

public sealed class SystemInfoService
{
    public SystemInfo Get()
    {
        string cpu = GetCpuName();
        (long totalRam, long _) = GetRamMb();
        (long freeGb, long totalGb) = GetDiskGb();

        return new SystemInfo(cpu, totalRam, freeGb, totalGb);
    }

    private static string GetCpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var o in searcher.Get())
            {
                return (o["Name"]?.ToString() ?? "Unknown CPU").Trim();
            }
        }
        catch { }
        return "Unknown CPU";
    }

    private static (long totalMb, long freeMb) GetRamMb()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem");
            foreach (var o in searcher.Get())
            {
                long totalKb = Convert.ToInt64(o["TotalVisibleMemorySize"]);
                long freeKb = Convert.ToInt64(o["FreePhysicalMemory"]);
                return (totalKb / 1024, freeKb / 1024);
            }
        }
        catch { }
        return (0, 0);
    }

    private static (long freeGb, long totalGb) GetDiskGb()
    {
        try
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed && d.Name.Equals(Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase));

            if (drive is null) return (0, 0);

            long freeGb = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            long totalGb = drive.TotalSize / (1024 * 1024 * 1024);
            return (freeGb, totalGb);
        }
        catch { }
        return (0, 0);
    }
}