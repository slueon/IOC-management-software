using IOC.Core.Enums;
using IOC.Core.Models.AccessControl;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class AccessControlMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void Test_DoorForcedOpen_GeneratesMajorIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            DoorPoints = new List<DoorPointInfo>
            {
                new()
                {
                    DoorId = "D-LOBBY-01",
                    DoorName = "Cửa Cổng Trượt Sảnh Chính",
                    Location = "Sảnh Tầng 1",
                    IsForcedOpen = true,
                    IsOpen = true
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("CẠY PHÁ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_DoorHeldOpen_ExceedThreshold_GeneratesMinorIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            DoorPoints = new List<DoorPointInfo>
            {
                new()
                {
                    DoorId = "D-SERVER-01",
                    DoorName = "Cửa Phòng Server",
                    Location = "Tầng 3",
                    IsHeldOpen = true,
                    HeldDurationSeconds = 180,
                    HeldThresholdSeconds = 120
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor &&
            i.Title.Contains("chèn giữ mở", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_DoorDisconnected_GeneratesIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            DoorPoints = new List<DoorPointInfo>
            {
                new()
                {
                    DoorId = "D-EXIT-01",
                    DoorName = "Cửa Thoát Hiểm Hầm B1",
                    Location = "Hầm B1",
                    IsConnected = false,
                    ReaderStatus = "MẤT KẾT NỐI"
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor &&
            i.Title.Contains("Mất kết nối đầu đọc thẻ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ControllerOffline_MarksDeviceOfflineAndGeneratesCriticalIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            IsControllerOnline = false,
            Status = DeviceStatus.Offline
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Mất kết nối Bộ điều khiển cửa", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DeviceStatus.Critical, device.Status);
    }

    [Fact]
    public void Test_ControllerMainPowerLoss_GeneratesMajorIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            IsMainPowerOk = false,
            BatteryLevelPercent = 85.0
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("Mất nguồn điện bộ điều khiển", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ControllerLowBattery_GeneratesMinorIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Tầng 1",
            IsMainPowerOk = false,
            BatteryLevelPercent = 15.0
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor &&
            i.Title.Contains("ắc quy yếu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ElevatorEmergencyStop_GeneratesCriticalIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa & Thang Máy",
            Elevators = new List<ElevatorInfo>
            {
                new()
                {
                    ElevatorId = "ELEV-01",
                    ElevatorName = "Thang Khách Tháp A (Cabin 1)",
                    CurrentFloor = 3,
                    CurrentFloorDisplay = "Tầng 3",
                    IsEmergencyStop = true
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Dừng khẩn cấp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ElevatorOverloaded_GeneratesMinorIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa & Thang Máy",
            Elevators = new List<ElevatorInfo>
            {
                new()
                {
                    ElevatorId = "ELEV-02",
                    ElevatorName = "Thang Hàng (Cabin 2)",
                    RatedLoadKg = 1000.0,
                    LoadKg = 1250.0
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor &&
            i.Title.Contains("quá tải trọng", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_ElevatorDisconnected_GeneratesCriticalIncident()
    {
        var device = new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa & Thang Máy",
            Elevators = new List<ElevatorInfo>
            {
                new()
                {
                    ElevatorId = "ELEV-03",
                    ElevatorName = "Thang VIP Lãnh Đạo",
                    IsConnected = false
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Mất kết nối bộ điều khiển thang máy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Test_FaultSimulation_And_NormalReset()
    {
        var service = new SubsystemHealthMonitoringService();
        await service.InitializeAsync();

        // 1. DOOR_FORCED fault
        await service.TriggerDiagnosticFaultAsync("AC-CTRL-01", "DOOR_FORCED");
        var devices = await service.GetDevicesBySubsystemAsync(SubsystemType.AccessControl);
        var dev = devices.First(d => d.Id == "AC-CTRL-01") as AccessControlDevice;
        Assert.NotNull(dev);
        Assert.Contains(dev.DoorPoints, d => d.IsForcedOpen);

        // 2. CONTROLLER_OFFLINE fault
        await service.TriggerDiagnosticFaultAsync("AC-CTRL-01", "CONTROLLER_OFFLINE");
        Assert.False(dev.IsControllerOnline);

        // 3. ELEVATOR_EMERGENCY fault
        await service.TriggerDiagnosticFaultAsync("AC-CTRL-01", "ELEVATOR_EMERGENCY");
        Assert.Contains(dev.Elevators, e => e.IsEmergencyStop);

        // 4. Reset to NORMAL
        await service.TriggerDiagnosticFaultAsync("AC-CTRL-01", "NORMAL");
        Assert.True(dev.IsControllerOnline);
        Assert.True(dev.IsMainPowerOk);
        Assert.DoesNotContain(dev.DoorPoints, d => d.IsForcedOpen);
        Assert.DoesNotContain(dev.Elevators, e => e.IsEmergencyStop);
    }

    [Fact]
    public void Test_AccessControlDevice_PropertyBindingSafety()
    {
        var device = new AccessControlDevice
        {
            IsMainPowerOk = true,
            BatteryLevelPercent = 15.0
        };

        Assert.False(device.IsBackupBatteryActive);
        Assert.True(device.IsBatteryLow);

        // Verify property setters do not throw even if TwoWay binding engine attempts to write back
        device.IsBackupBatteryActive = true;
        device.IsBatteryLow = false;

        device.IsMainPowerOk = false;
        Assert.True(device.IsBackupBatteryActive);
    }
}
