using IOC.Core.Enums;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;
using IOC.Core.Models.PublicAddress;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class SubsystemHealthEvaluatorTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void LanTel_HighCpuAndPortDown_GeneratesExpectedIncidents()
    {
        var device = new LanTelDevice
        {
            Id = "SW-CORE-01",
            Name = "Switch Core Trung Tâm",
            DeviceType = LanDeviceType.SwitchCore,
            CpuUsagePercent = 95.0,
            BandwidthUtilizationPercent = 90.0,
            Ports = new List<SwitchPortInfo>
            {
                new() { PortNumber = 1, PortName = "GigabitEthernet 1/0/1", IsUp = false, ConnectedDevice = "Server Rack 1" }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.NotEmpty(incidents);
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("CPU"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("Link Down"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor && i.Title.Contains("nghẽn mạng"));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }

    [Fact]
    public void PublicAddress_FuseBlownAndSpeakerFault_GeneratesIncidents()
    {
        var device = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Điều Khiển Trung Tâm Âm Thanh",
            IsMainPowerOk = false,
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new() { AmplifierId = "AMP-01", AmplifierName = "Ampli Tầng 1-5", IsFuseBlown = true }
            },
            SpeakerZones = new List<SpeakerZoneInfo>
            {
                new() { ZoneId = 1, ZoneName = "Zone Hầm B1", ImpedanceOhms = 5.0, NormalImpedanceOhms = 100.0 }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("cầu chì"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("Mất nguồn"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("trở kháng"));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }

    [Fact]
    public void Cctv_VideoLossAndNvrBadSector_GeneratesIncidents()
    {
        var camera = new CctvDevice
        {
            Id = "CAM-01",
            Name = "Camera Cổng Chính",
            DeviceType = CctvDeviceType.IpCamera,
            IsVideoLoss = true
        };

        var nvr = new CctvDevice
        {
            Id = "NVR-01",
            Name = "Đầu Ghi NVR Trung Tâm",
            DeviceType = CctvDeviceType.NvrRecorder,
            NvrStorage = new NvrStorageInfo
            {
                HasBadSector = true,
                RaidStatus = RaidHealthStatus.Degraded,
                StorageTotalGb = 8000,
                StorageUsedGb = 7800
            }
        };

        var camIncidents = _evaluator.EvaluateDevice(camera);
        var nvrIncidents = _evaluator.EvaluateDevice(nvr);

        Assert.Contains(camIncidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("Video Loss"));
        Assert.Contains(nvrIncidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("Bad Sector"));
        Assert.Contains(nvrIncidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("RAID") && i.Title.Contains("Degraded"));
        Assert.Contains(nvrIncidents, i => i.Severity == AlarmSeverity.Minor && i.Title.Contains("đầy dung lượng"));
    }

    [Fact]
    public void AccessControl_DoorForcedOpen_GeneratesMajorAlarm()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            BatteryLevelPercent = 12.0,
            DoorPoints = new List<DoorPointInfo>
            {
                new() { DoorId = "DOOR-101", DoorName = "Cửa Thoát Hiểm Phía Đông", IsForcedOpen = true },
                new() { DoorId = "DOOR-102", DoorName = "Cửa Kho Kỹ Thuật", IsHeldOpen = true, HeldDurationSeconds = 180 }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("cạy phá", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor && i.Title.Contains("chèn giữ mở"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor && i.Title.Contains("ắc quy yếu"));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }

    [Fact]
    public void Bms_ChillerExceededAndGasLeak_GeneratesCriticalAlarms()
    {
        var device = new BmsDevice
        {
            Id = "BMS-MAIN",
            Name = "Hệ Thống BMS Trung Tâm",
            Category = BmsCategory.ChillerHvac,
            Chiller = new ChillerMetrics { SupplyWaterTempCelsius = 19.5, SystemPressureBar = 7.0 },
            Pumps = new List<PumpMetrics>
            {
                new() { PumpId = "PUMP-01", PumpName = "Bơm Cứu Hỏa", IsThermalOverloadTripped = true }
            },
            PowerGrid = new PowerGridMetrics { VoltagePhaseA = 10.0 }, // Phase loss
            Hazard = new HazardSensorMetrics { GasCoConcentrationPpm = 85.0, SumpPitWaterLevelPercent = 8.0 }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("Chiller"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("nhảy rơ-le"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("Mất pha"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("Rò rỉ khí"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("cạn bể"));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }

    [Fact]
    public void FireAlarm_SmokeDetectedAndLoopFault_GeneratesCriticalAndMajorAlarms()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL",
            Name = "Tủ Trung Tâm Báo Cháy PCCC",
            IsAcPowerOk = false,
            IsBatteryBackupActive = true,
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    IsLoopOpenFault = true,
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new()
                        {
                            PointAddress = "L1-D04",
                            SensorType = FireAlarmSensorType.SmokeDetector,
                            Location = "Phòng Server Tầng 3",
                            IsAlarmTriggered = true
                        }
                    }
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical && i.Title.Contains("BÁO CHÁY"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("Đứt mạch vòng"));
        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major && i.Title.Contains("nguồn pin"));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }
}
