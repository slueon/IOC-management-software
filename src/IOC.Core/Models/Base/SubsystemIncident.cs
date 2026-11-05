using IOC.Core.Enums;

namespace IOC.Core.Models.Base;

public class SubsystemIncident
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubsystemType Subsystem { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Minor;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

