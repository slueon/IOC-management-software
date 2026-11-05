using IOC.Core.Enums;

namespace IOC.Core.Models.Base;

public class SubsystemHealthSummary
{
    public SubsystemType Subsystem { get; set; }
    public string SubsystemName { get; set; } = string.Empty;
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int WarningDevices { get; set; }
    public int CriticalDevices { get; set; }
    public int OfflineDevices { get; set; }
    public int ActiveIncidentsCount { get; set; }
    public int CriticalIncidentsCount { get; set; }
    public double HealthPercentage
    {
        get => TotalDevices == 0 ? 100.0 : Math.Round((double)OnlineDevices / TotalDevices * 100, 1);
        set { }
    }
    public DeviceStatus OverallStatus
    {
        get
        {
            if (CriticalDevices > 0 || CriticalIncidentsCount > 0) return DeviceStatus.Critical;
            if (WarningDevices > 0 || OfflineDevices > 0) return DeviceStatus.Warning;
            return DeviceStatus.Online;
        }
        set { }
    }
}

