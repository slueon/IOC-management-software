namespace IOC.Core.Contracts.Persistence;

public interface IIocStorageService
{
    Task SaveDataAsync<T>(string key, T data, CancellationToken cancellationToken = default);
    Task<T?> LoadDataAsync<T>(string key, CancellationToken cancellationToken = default);
    Task AppendAuditLogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogEntry>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task RecordAcknowledgementAsync(IncidentAcknowledgementRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentAcknowledgementRecord>> GetAcknowledgementsAsync(CancellationToken cancellationToken = default);
}
