using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.Cctv;

public enum CctvDeviceType
{
    IpCamera,
    NvrRecorder
}

public class CctvDevice : SubsystemDevice
{
    public CctvDevice()
    {
        Subsystem = SubsystemType.Cctv;
    }

    public CctvDeviceType DeviceType { get; set; } = CctvDeviceType.IpCamera;
    public string RtspStreamUrl { get; set; } = string.Empty;
    public bool IsRtspStreamActive { get; set; } = true;
    public bool IsVideoLoss { get; set; }
    public int Fps { get; set; } = 30;
    public string Resolution { get; set; } = "1920x1080 (FHD)";
    public int BitrateKbps { get; set; } = 4096;
    public int HeartbeatCycleSeconds { get; set; } = 5;
    public int MissedHeartbeats { get; set; }
    public double HeartbeatLatencyMs { get; set; } = 3.5;
    public DateTime LastHeartbeatTime { get; set; } = DateTime.UtcNow;

    // Mặt bằng (Floor Plan)
    public string FloorPlanId { get; set; } = "FLOOR_1";
    public string FloorPlanName { get; set; } = "Tầng 1 - Sảnh Chính";
    public double PositionX { get; set; } = 50.0; // 0.0 - 100.0 (%)
    public double PositionY { get; set; } = 50.0; // 0.0 - 100.0 (%)
    public string CameraFormFactor { get; set; } = "Dome Bán Cầu";

    // Điều khiển PTZ (Pan / Tilt / Zoom)
    public bool HasPtzCapability { get; set; } = true;
    public double PanAngle { get; set; } = 0.0;   // -180 đến +180 độ
    public double TiltAngle { get; set; } = 0.0;  // -90 đến +90 độ
    public double ZoomLevel { get; set; } = 1.0;  // 1.0x - 32.0x
    public bool IsNightVisionActive { get; set; }
    public bool IsRecording { get; set; } = true;

    public NvrStorageInfo? NvrStorage { get; set; }
}
