using IOC.Core.Enums;

namespace IOC.Core.Models.Base;

public class OverallHealthSummary
{
    public int TotalSubsystems { get; set; } = 6;
    public int TotalDevices { get; set; }
    public int TotalOnline { get; set; }
    public int TotalWarning { get; set; }
    public int TotalCritical { get; set; }
    public int TotalOffline { get; set; }
    public int TotalActiveIncidents { get; set; }
    public int TotalCriticalIncidents { get; set; }
    public double OverallUptimePercent
    {
        get => TotalDevices == 0 ? 100.0 : Math.Round((double)TotalOnline / TotalDevices * 100, 1);
        set { }
    }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<SubsystemHealthSummary> SubsystemSummaries { get; set; } = [];

    public DeviceStatus OverallStatus
    {
        get
        {
            if (TotalCritical > 0 || TotalCriticalIncidents > 0) return DeviceStatus.Critical;
            if (TotalWarning > 0 || TotalOffline > 0) return DeviceStatus.Warning;
            return DeviceStatus.Online;
        }
        set { }
    }
}

