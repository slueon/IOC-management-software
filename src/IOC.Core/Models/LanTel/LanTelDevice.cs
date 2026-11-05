using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.LanTel;

public enum LanDeviceType
{
    SwitchCore,
    SwitchEdge,
    Router,
    IpPbx
}

public class LanTelDevice : SubsystemDevice
{
    public LanTelDevice()
    {
        Subsystem = SubsystemType.LanTel;
    }

    public LanDeviceType DeviceType { get; set; } = LanDeviceType.SwitchEdge;

    // Giám sát hiệu năng phần cứng
    public double CpuUsagePercent { get; set; } = 32.0;
    public double MemoryTotalMb { get; set; } = 4096.0;
    public double MemoryUsedMb { get; set; } = 1520.0;
    public double MemoryUsagePercent => MemoryTotalMb > 0 ? Math.Round(MemoryUsedMb / MemoryTotalMb * 100, 1) : 0;
    public bool IsCpuOverload => CpuUsagePercent >= 90.0;
    public bool IsMemoryOverflow => MemoryUsagePercent >= 90.0;

    public double TemperatureCelsius { get; set; } = 42.0;
    public bool IsOverheated => TemperatureCelsius >= 75.0;
    public bool IsTemperatureWarning => TemperatureCelsius >= 60.0 && TemperatureCelsius < 75.0;

    // Bộ nguồn & Quạt tản nhiệt
    public string Psu1Status { get; set; } = "OK";
    public string Psu2Status { get; set; } = "OK";
    public bool HasPsuRedundancyOk => Psu1Status == "OK" && Psu2Status == "OK";
    public int Fan1Rpm { get; set; } = 4250;
    public int Fan2Rpm { get; set; } = 4180;
    public bool IsFanHealthy { get; set; } = true;

    // Băng thông cổng mạng tổng thể
    public double BandwidthUtilizationPercent { get; set; } = 45.0;
    public bool IsCongested => BandwidthUtilizationPercent >= 85.0;

    // Danh sách cổng mạng Switch
    public List<SwitchPortInfo> Ports { get; set; } = [];
    public int TotalPortsCount => Ports.Count;
    public int UpPortsCount => Ports.Count(p => p.IsUp);
    public int DownPortsCount => Ports.Count(p => !p.IsUp && p.IsAdminEnabled);
    public int CongestedPortsCount => Ports.Count(p => p.IsCongested);

    // Tổng đài IP-PBX & Máy nhánh
    public PbxTrunkInfo? PbxTrunk { get; set; }
    public List<PbxExtensionInfo> Extensions { get; set; } = [];
    public int TotalExtensionsCount => Extensions.Count;
    public int RegisteredExtensionsCount => Extensions.Count(e => e.Status != PbxExtensionStatus.Disconnected);
    public int DisconnectedExtensionsCount => Extensions.Count(e => e.Status == PbxExtensionStatus.Disconnected);
}
