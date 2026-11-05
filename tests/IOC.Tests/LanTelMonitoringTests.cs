using IOC.Core.Enums;
using IOC.Core.Models.LanTel;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class LanTelMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void SwitchPort_CableCut_TriggersInstantCableDownAlert()
    {
        var switchDevice = new LanTelDevice
        {
            Id = "SW-CORE-01",
            Name = "Switch Core Trung Tâm",
            DeviceType = LanDeviceType.SwitchCore,
            Ports = new List<SwitchPortInfo>
            {
                new()
                {
                    PortNumber = 1,
                    PortName = "GigabitEthernet 1/0/1",
                    IsUp = false,
                    IsAdminEnabled = true,
                    ConnectedDevice = "Camera Sảnh Chính Tầng 1",
                    DeviceCategory = "CCTV",
                    CableFaultTimestamp = DateTime.UtcNow
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(switchDevice);

        Assert.NotEmpty(incidents);
        var cableAlert = incidents.FirstOrDefault(i => i.Title.Contains("Link Down") || i.Title.Contains("đứt cáp"));
        Assert.NotNull(cableAlert);
        Assert.Equal(AlarmSeverity.Major, cableAlert.Severity);
        Assert.Contains("GigabitEthernet 1/0/1", cableAlert.Description);
        Assert.Contains("Camera Sảnh Chính Tầng 1", cableAlert.Description);
    }

    [Fact]
    public void HardwareMonitoring_CpuRamTempOverload_GeneratesAccurateAlerts()
    {
        var switchDevice = new LanTelDevice
        {
            Id = "SW-CORE-01",
            Name = "Switch Core Trung Tâm",
            CpuUsagePercent = 94.5,
            MemoryTotalMb = 4096.0,
            MemoryUsedMb = 3850.0, // 94% usage
            TemperatureCelsius = 78.5 // Overheat
        };

        var incidents = _evaluator.EvaluateDevice(switchDevice);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("CPU"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("RAM"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("Nhiệt độ bo mạch"));
    }

    [Fact]
    public void PortBandwidth_CongestionOver85Percent_GeneratesLocalCongestionAlert()
    {
        var switchDevice = new LanTelDevice
        {
            Id = "SW-EDGE-T1",
            Name = "Switch Phân Phối Tầng 1",
            Ports = new List<SwitchPortInfo>
            {
                new()
                {
                    PortNumber = 4,
                    PortName = "Gi1/0/4",
                    SpeedMbps = 1000,
                    RxRateMbps = 520.0,
                    TxRateMbps = 390.0, // 910 Mbps = 91%
                    ConnectedDevice = "NVR Storage Gateway"
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(switchDevice);

        var congestionAlert = incidents.FirstOrDefault(i => i.Title.Contains("nghẽn mạng"));
        Assert.NotNull(congestionAlert);
        Assert.Contains("Gi1/0/4", congestionAlert.Title);
        Assert.Contains("91", congestionAlert.Title);
    }

    [Fact]
    public void IpPbx_SipTrunkDown_And_ExtensionDisconnected_GeneratesAlerts()
    {
        var pbxDevice = new LanTelDevice
        {
            Id = "PBX-MAIN-01",
            Name = "Tổng Đài IP-PBX Tòa Nhà",
            DeviceType = LanDeviceType.IpPbx,
            PbxTrunk = new PbxTrunkInfo
            {
                TrunkName = "SIP-TRUNK-VNPT",
                IsSipTrunkUp = false,
                DownTimestamp = DateTime.UtcNow
            },
            Extensions = new List<PbxExtensionInfo>
            {
                new()
                {
                    ExtensionNumber = "102",
                    DepartmentName = "Phòng Trực Điều Hành",
                    Status = PbxExtensionStatus.Disconnected,
                    IsCriticalDepartment = true
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(pbxDevice);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("trung kế", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("máy nhánh", StringComparison.OrdinalIgnoreCase) && i.Description.Contains("102"));
    }
}
