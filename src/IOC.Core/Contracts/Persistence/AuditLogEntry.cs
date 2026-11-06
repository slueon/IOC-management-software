namespace IOC.Core.Contracts.Persistence;

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Subsystem { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
}

public class IncidentAcknowledgementRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public string Subsystem { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string IncidentTitle { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
