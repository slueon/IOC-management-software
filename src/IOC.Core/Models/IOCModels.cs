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
