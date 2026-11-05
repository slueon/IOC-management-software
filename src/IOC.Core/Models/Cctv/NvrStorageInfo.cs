using IOC.Core.Enums;

namespace IOC.Core.Models.Cctv;

public class NvrDiskDriveInfo
{
    public int SlotNumber { get; set; }
    public string Model { get; set; } = "WD Purple Pro 4TB";
    public string SerialNumber { get; set; } = string.Empty;
    public double CapacityGb { get; set; } = 4000.0;
    public double TemperatureCelsius { get; set; } = 38.5;
    public string SmartHealth { get; set; } = "GOOD (PASSED)";
    public bool IsFaulty { get; set; }
}

public class NvrStorageInfo
{
    public double StorageTotalGb { get; set; } = 16000.0;
    public double StorageUsedGb { get; set; } = 9800.0;
    public double DiskUsagePercent => StorageTotalGb > 0 ? Math.Round(StorageUsedGb / StorageTotalGb * 100, 1) : 0;
    public bool HasBadSector { get; set; }
    public int BadSectorCount { get; set; }
    public RaidHealthStatus RaidStatus { get; set; } = RaidHealthStatus.Healthy;
    public string RaidType { get; set; } = "RAID 5 (4x 4TB Enterprise)";
    public int TotalChannels { get; set; } = 64;
    public int RecordingChannels { get; set; } = 64;
    public List<NvrDiskDriveInfo> PhysicalDisks { get; set; } = [];
}
