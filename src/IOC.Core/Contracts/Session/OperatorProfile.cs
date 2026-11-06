namespace IOC.Core.Contracts.Session;

public class OperatorProfile
{
    public string OperatorId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public string BadgeId { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    public string DisplayTitle => $"{FullName} ({OperatorId} - {Role})";
    public string FormattedBadge => $"Trực Ban IOC: {FullName} (Mã: {OperatorId})";
}
