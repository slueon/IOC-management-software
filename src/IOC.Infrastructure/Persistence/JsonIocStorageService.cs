using System.IO;
using System.Text.Json;
using IOC.Core.Contracts.Persistence;

namespace IOC.Infrastructure.Persistence;

public class JsonIocStorageService : IIocStorageService
{
    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string StorageDirectory => _storageDirectory;

    public JsonIocStorageService(string? customDirectory = null)
    {
        _storageDirectory = customDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ioc_storage");
        Directory.CreateDirectory(_storageDirectory);
    }

    private string GetFilePath(string key) => Path.Combine(_storageDirectory, $"{key}.json");

    public async Task SaveDataAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath(key);
            var tempPath = filePath + ".tmp";

            var jsonString = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(tempPath, jsonString, cancellationToken);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T?> LoadDataAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                return default;
            }

            var jsonString = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
        }
        catch (JsonException)
        {
            // Corrupt file recovery: Return default rather than crashing the application
            return default;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AppendAuditLogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            const string key = "audit_logs";
            var filePath = GetFilePath(key);
            var logs = new List<AuditLogEntry>();

            if (File.Exists(filePath))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(filePath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var existing = JsonSerializer.Deserialize<List<AuditLogEntry>>(text, JsonOptions);
                        if (existing != null) logs = existing;
                    }
                }
                catch (JsonException)
                {
                    // Ignore corrupted file and start fresh
                }
            }

            logs.Insert(0, entry);
            if (logs.Count > 1000)
            {
                logs = logs.Take(1000).ToList();
            }

            var tempPath = filePath + ".tmp";
            var updatedJson = JsonSerializer.Serialize(logs, JsonOptions);
            await File.WriteAllTextAsync(tempPath, updatedJson, cancellationToken);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath("audit_logs");
            if (!File.Exists(filePath))
            {
                return Array.Empty<AuditLogEntry>();
            }

            var text = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<AuditLogEntry>();

            var logs = JsonSerializer.Deserialize<List<AuditLogEntry>>(text, JsonOptions) ?? new List<AuditLogEntry>();
            return logs.Take(limit).ToList().AsReadOnly();
        }
        catch (JsonException)
        {
            return Array.Empty<AuditLogEntry>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RecordAcknowledgementAsync(IncidentAcknowledgementRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            const string key = "acknowledgements";
            var filePath = GetFilePath(key);
            var acks = new List<IncidentAcknowledgementRecord>();

            if (File.Exists(filePath))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(filePath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var existing = JsonSerializer.Deserialize<List<IncidentAcknowledgementRecord>>(text, JsonOptions);
                        if (existing != null) acks = existing;
                    }
                }
                catch (JsonException)
                {
                    // Ignore corrupted file
                }
            }

            acks.Insert(0, record);
            if (acks.Count > 500) acks = acks.Take(500).ToList();

            var tempPath = filePath + ".tmp";
            var updatedJson = JsonSerializer.Serialize(acks, JsonOptions);
            await File.WriteAllTextAsync(tempPath, updatedJson, cancellationToken);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<IncidentAcknowledgementRecord>> GetAcknowledgementsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath("acknowledgements");
            if (!File.Exists(filePath)) return Array.Empty<IncidentAcknowledgementRecord>();

            var text = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<IncidentAcknowledgementRecord>();

            var acks = JsonSerializer.Deserialize<List<IncidentAcknowledgementRecord>>(text, JsonOptions) ?? new List<IncidentAcknowledgementRecord>();
            return acks.AsReadOnly();
        }
        catch (JsonException)
        {
            return Array.Empty<IncidentAcknowledgementRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
