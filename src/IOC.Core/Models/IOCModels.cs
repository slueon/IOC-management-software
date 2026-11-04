using System;
using IOC.Core.Enums;

namespace IOC.Core.Models
{
    public class Device
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public SubSystemType SubSystem { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Location { get; set; } = string.Empty;
        public DeviceStatus Status { get; set; } = DeviceStatus.Online;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public string LastError { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime InstallDate { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;

        // Telemetry & Real-time Monitoring properties
        public string Protocol { get; set; } = "ICMP/Ping";
        public double LatencyMs { get; set; } = 1.2;
        public string Telemetry { get; set; } = "Hoạt động bình thường";
        public double UptimePercent { get; set; } = 99.9;
        public string Floor { get; set; } = "Tầng 1";
        public double CanvasX { get; set; } = 100;
        public double CanvasY { get; set; } = 100;

        public string StatusColor => Status switch
        {
            DeviceStatus.Online => "#48BB78",
            DeviceStatus.Warning => "#ED8936",
            _ => "#F56565"
        };

        public string StatusBadgeText => Status switch
        {
            DeviceStatus.Online => "ONLINE",
            DeviceStatus.Warning => "CẢNH BÁO",
            _ => "MẤT KẾT NỐI"
        };
    }

    public class SubSystemOverview
    {
        public SubSystemType SubSystem { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StatusText { get; set; } = "BÌNH THƯỜNG";
        public string StatusColor { get; set; } = "#48BB78"; // Green
        public string ProtocolInfo { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public string PrimaryMetric { get; set; } = string.Empty;
        public string SecondaryMetric { get; set; } = string.Empty;
        public double Uptime { get; set; } = 100.0;
        public bool HasAlert { get; set; } = false;
    }

    public class AlarmEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public SubSystemType SubSystem { get; set; }
        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Major;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public AlarmStatus Status { get; set; } = AlarmStatus.Active;
        public string AcknowledgedBy { get; set; } = string.Empty;
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
