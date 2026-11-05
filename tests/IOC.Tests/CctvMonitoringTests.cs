using IOC.Core.Enums;
using IOC.Core.Models.Cctv;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class CctvMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void Test_Cctv_HeartbeatTimeout_MarksCameraOfflineAndCriticalAlarm()
    {
        var cam = new CctvDevice
        {
            Id = "CAM-S101",
            Name = "Camera Sảnh Chính Tầng 1",
            DeviceType = CctvDeviceType.IpCamera,
            Status = DeviceStatus.Offline,
            HeartbeatCycleSeconds = 5,
            MissedHeartbeats = 3
        };

        var incidents = _evaluator.EvaluateDevice(cam);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Heartbeat", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DeviceStatus.Critical, cam.Status);
    }

    [Fact]
    public void Test_Cctv_VideoLoss_Or_RtspStreamInactive_GeneratesMajorAlarm()
    {
        var cam = new CctvDevice
        {
            Id = "CAM-B101",
            Name = "Camera Hầm Xe B1",
            DeviceType = CctvDeviceType.IpCamera,
            Status = DeviceStatus.Online,
            IsVideoLoss = true,
            IsRtspStreamActive = false,
            BitrateKbps = 0
        };

        var incidents = _evaluator.EvaluateDevice(cam);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            (i.Title.Contains("Video Loss", StringComparison.OrdinalIgnoreCase) || i.Title.Contains("mất tín hiệu hình ảnh", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Test_Cctv_NvrStorage_NearlyFull_GeneratesAlarm()
    {
        var nvr = new CctvDevice
        {
            Id = "NVR-SRV-01",
            Name = "Đầu Ghi Hình NVR 64 Kênh",
            DeviceType = CctvDeviceType.NvrRecorder,
            NvrStorage = new NvrStorageInfo
            {
                StorageTotalGb = 10000.0,
                StorageUsedGb = 9600.0, // 96%
                HasBadSector = false,
                RaidStatus = RaidHealthStatus.Healthy
            }
        };

        var incidents = _evaluator.EvaluateDevice(nvr);

        Assert.Contains(incidents, i => i.Title.Contains("đầy dung lượng", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Cctv_NvrStorage_BadSector_GeneratesAlarm()
    {
        var nvr = new CctvDevice
        {
            Id = "NVR-SRV-01",
            Name = "Đầu Ghi Hình NVR 64 Kênh",
            DeviceType = CctvDeviceType.NvrRecorder,
            NvrStorage = new NvrStorageInfo
            {
                StorageTotalGb = 16000.0,
                StorageUsedGb = 5000.0,
                HasBadSector = true,
                BadSectorCount = 14,
                RaidStatus = RaidHealthStatus.Healthy
            }
        };

        var incidents = _evaluator.EvaluateDevice(nvr);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("Bad Sector", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Cctv_NvrStorage_RaidDegraded_GeneratesAlarm()
    {
        var nvr = new CctvDevice
        {
            Id = "NVR-SRV-01",
            Name = "Đầu Ghi Hình NVR 64 Kênh",
            DeviceType = CctvDeviceType.NvrRecorder,
            NvrStorage = new NvrStorageInfo
            {
                StorageTotalGb = 16000.0,
                StorageUsedGb = 5000.0,
                HasBadSector = false,
                RaidStatus = RaidHealthStatus.Degraded
            }
        };

        var incidents = _evaluator.EvaluateDevice(nvr);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("RAID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Cctv_PtzCapabilities_And_FloorPlanMapping()
    {
        var cam = new CctvDevice
        {
            Id = "CAM-PTZ-01",
            Name = "Camera PTZ Sảnh Chính",
            DeviceType = CctvDeviceType.IpCamera,
            FloorPlanId = "FLOOR_1",
            FloorPlanName = "Tầng 1 - Sảnh Chính",
            PositionX = 45.5,
            PositionY = 60.0,
            CameraFormFactor = "PTZ Speed Dome 360",
            PanAngle = 45.0,
            TiltAngle = -15.0,
            ZoomLevel = 4.0,
            IsNightVisionActive = true,
            BitrateKbps = 4096
        };

        Assert.Equal("FLOOR_1", cam.FloorPlanId);
        Assert.Equal(45.5, cam.PositionX);
        Assert.Equal(60.0, cam.PositionY);
        Assert.True(cam.IsNightVisionActive);
        Assert.Equal(4.0, cam.ZoomLevel);
    }

    [Fact]
    public async Task Test_Cctv_ServiceFaultSimulation_VideoLossAndRecovery()
    {
        var service = new SubsystemHealthMonitoringService();
        await service.InitializeAsync();

        // Giả lập Video Loss
        await service.SimulateFaultAsync("CAM-S101", "VIDEO_LOSS");
        var cam = service.GetDeviceById("CAM-S101") as CctvDevice;
        Assert.NotNull(cam);
        Assert.True(cam.IsVideoLoss);

        // Khôi phục
        await service.SimulateFaultAsync("CAM-S101", "NORMAL");
        Assert.False(cam.IsVideoLoss);
        Assert.True(cam.IsRtspStreamActive);
    }
}

