using IOC.Core.Enums;

namespace IOC.Core.Models.Base;

public abstract class SubsystemDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SubsystemType Subsystem { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; } = DeviceStatus.Online;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public double LatencyMs { get; set; }
    public string StatusSummary { get; set; } = "Hoạt động bình thường";
}

