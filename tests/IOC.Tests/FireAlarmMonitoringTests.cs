using IOC.Core.Enums;
using IOC.Core.Models.FireAlarm;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class FireAlarmMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void Test_SmokeDetector_Triggered_GeneratesCriticalIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    LoopName = "Loop 1 - Khối Sảnh & Phòng Server",
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new()
                        {
                            PointAddress = "L1-D04",
                            SensorType = FireAlarmSensorType.SmokeDetector,
                            DeviceName = "Đầu Báo Khói Quang Điện",
                            Location = "Phòng Máy Chủ Server Room",
                            IsAlarmTriggered = true,
                            AnalogValue = 4.8 // High smoke density
                        }
                    }
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("BÁO CHÁY", StringComparison.OrdinalIgnoreCase) &&
            i.Title.Contains("Đầu Báo Khói", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_HeatDetector_Triggered_GeneratesCriticalIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    LoopName = "Loop 1 - Khối Sảnh & Hầm B1",
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new()
                        {
                            PointAddress = "L1-H12",
                            SensorType = FireAlarmSensorType.HeatDetector,
                            DeviceName = "Đầu Báo Nhiệt Gia Tăng",
                            Location = "Trạm Biến Áp Hầm B1",
                            IsAlarmTriggered = true,
                            AnalogValue = 68.5 // 68.5°C
                        }
                    }
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("BÁO CHÁY", StringComparison.OrdinalIgnoreCase) &&
            i.Title.Contains("Đầu Báo Nhiệt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ManualCallPoint_Triggered_GeneratesCriticalIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 2,
                    LoopName = "Loop 2 - Khối Thoát Hiểm Tầng 1-5",
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new()
                        {
                            PointAddress = "L2-MCP01",
                            SensorType = FireAlarmSensorType.ManualCallPoint,
                            DeviceName = "Nút Ấn Báo Cháy Khẩn Cấp",
                            Location = "Cửa Thoát Hiểm Sảnh Chính Tầng 1",
                            IsAlarmTriggered = true
                        }
                    }
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Nút Ấn Khẩn Cấp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_LoopOpenFault_GeneratesMajorIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    LoopName = "Loop 1 - Khối Văn Phòng",
                    IsLoopOpenFault = true,
                    FaultLocationDescription = "Đứt cáp tín hiệu giữa địa chỉ L1-D14 và L1-D15"
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("Đứt mạch vòng", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_AcPowerLoss_SwitchesToBattery_GeneratesMajorIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            IsAcPowerOk = false,
            IsBatteryBackupActive = true,
            BatteryVoltageVolt = 25.8
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("chuyển sang nguồn pin phụ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_LowBatteryVoltage_GeneratesMajorIncident()
    {
        var device = new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Hochiki",
            IsAcPowerOk = false,
            IsBatteryBackupActive = true,
            BatteryVoltageVolt = 20.4,
            BatteryLevelPercent = 16.0
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            (i.Title.Contains("ắc quy dự phòng", StringComparison.OrdinalIgnoreCase) ||
             i.Title.Contains("chuyển sang nguồn pin phụ", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Test_ReadOnlyOperatingMode_IsEnforced()
    {
        var device = new FireAlarmDevice();
        Assert.True(device.IsReadOnlyOperatingMode);
        Assert.Contains("Read-Only", device.LegalComplianceNotice, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test_FireAlarm_DiagnosticFaultSimulation_And_NormalReset()
    {
        var service = new SubsystemHealthMonitoringService();
        await service.InitializeAsync();

        // 1. Simulate Fire Alarm Smoke
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");
        var fireDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.FireAlarm);
        var panel = fireDevices.First(d => d.Id == "FAS-PANEL-01") as FireAlarmDevice;
        Assert.NotNull(panel);
        Assert.True(panel.ActiveFireCount > 0);

        // 2. Simulate Loop Open Fault
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "LOOP_FAULT");
        Assert.True(panel.HasLoopFault);

        // 3. Simulate AC Power Loss
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "POWER_LOSS");
        Assert.False(panel.IsAcPowerOk);
        Assert.True(panel.IsBatteryBackupActive);

        // 4. Reset to NORMAL
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "NORMAL");
        Assert.Equal(0, panel.ActiveFireCount);
        Assert.False(panel.HasLoopFault);
        Assert.True(panel.IsAcPowerOk);
        Assert.False(panel.IsBatteryBackupActive);
    }
}

