using IOC.Core.Enums;
using IOC.Core.Models.PublicAddress;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class PublicAddressMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void Test_PublicAddress_MainPowerDown_SwitchesToBatteryAndGeneratesIncident()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm Âm Thanh Thông Báo PA",
            IsMainPowerOk = false,
            MainPowerVoltageVolt = 0.0,
            BatteryVoltageVolt = 24.2,
            IsBackupBatteryOk = true
        };

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("Mất nguồn điện chính", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DeviceStatus.Critical, pa.Status);
    }

    [Fact]
    public void Test_PublicAddress_BatteryVoltageSag_GeneratesMajorIncident()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm Âm Thanh Thông Báo PA",
            IsMainPowerOk = true,
            BatteryVoltageVolt = 19.8, // Ngưỡng nguy hiểm < 21.6V
            IsBackupBatteryOk = false
        };

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            (i.Title.Contains("ắc quy", StringComparison.OrdinalIgnoreCase) || i.Title.Contains("pin", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Test_PublicAddress_AmplifierBlownFuse_GeneratesCriticalIncident()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm PA",
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new()
                {
                    AmplifierId = "AMP-01",
                    AmplifierName = "Ampli Kênh 1-4",
                    IsFuseBlown = true
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("cầu chì", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DeviceStatus.Critical, pa.Status);
    }

    [Fact]
    public void Test_PublicAddress_AmplifierOverheat_GeneratesAlarm()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm PA",
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new()
                {
                    AmplifierId = "AMP-01",
                    AmplifierName = "Ampli Kênh 1-4",
                    TemperatureCelsius = 88.0 // Quá nhiệt khẩn cấp
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Title.Contains("quá nhiệt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_PublicAddress_AmplifierPowerOverload_GeneratesMajorIncident()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm PA",
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new()
                {
                    AmplifierId = "AMP-01",
                    AmplifierName = "Ampli Kênh 1-4",
                    OutputLoadWatt = 550.0,
                    RatedPowerWatt = 480.0
                }
            }
        };

        Assert.True(pa.Amplifiers[0].IsOverloaded);
        Assert.True(pa.Amplifiers[0].LoadPercentage > 100.0);

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("quá tải", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_PublicAddress_SpeakerZone_OpenCircuit_DetectedAsBrokenCable()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm PA",
            SpeakerZones = new List<SpeakerZoneInfo>
            {
                new()
                {
                    ZoneId = 1,
                    ZoneName = "Zone Tầng Hầm B1",
                    Location = "Tầng Hầm B1",
                    ImpedanceOhms = 450.0, // Hở mạch / đứt dây loa
                    NormalImpedanceOhms = 100.0
                }
            }
        };

        Assert.Equal(SpeakerZoneFaultType.OpenCircuit, pa.SpeakerZones[0].FaultType);
        Assert.True(pa.SpeakerZones[0].HasImpedanceFault);

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            (i.Title.Contains("đứt", StringComparison.OrdinalIgnoreCase) || i.Title.Contains("Open Circuit", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Test_PublicAddress_SpeakerZone_ShortCircuit_DetectedAsCriticalRisk()
    {
        var pa = new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm PA",
            SpeakerZones = new List<SpeakerZoneInfo>
            {
                new()
                {
                    ZoneId = 2,
                    ZoneName = "Zone Sảnh Tầng 1",
                    Location = "Tầng 1",
                    ImpedanceOhms = 4.2, // Chập tải / ngắn mạch đường dây
                    NormalImpedanceOhms = 100.0
                }
            }
        };

        Assert.Equal(SpeakerZoneFaultType.ShortCircuit, pa.SpeakerZones[0].FaultType);
        Assert.True(pa.SpeakerZones[0].HasImpedanceFault);

        var incidents = _evaluator.EvaluateDevice(pa);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            (i.Title.Contains("chập tải", StringComparison.OrdinalIgnoreCase) || i.Title.Contains("Short Circuit", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Test_PublicAddress_ServiceFaultSimulation_LineShortAndRecovery()
    {
        var service = new SubsystemHealthMonitoringService();
        await service.InitializeAsync();

        // Giả lập chập tải đường dây loa
        await service.SimulateFaultAsync("PA-CTRL-01", "LINE_SHORT");
        var dev = service.GetDeviceById("PA-CTRL-01") as PublicAddressDevice;
        Assert.NotNull(dev);
        Assert.Contains(dev.SpeakerZones, z => z.FaultType == SpeakerZoneFaultType.ShortCircuit);

        // Khôi phục bình thường
        await service.SimulateFaultAsync("PA-CTRL-01", "NORMAL");
        Assert.All(dev.SpeakerZones, z => Assert.Equal(SpeakerZoneFaultType.Normal, z.FaultType));
    }
}
