using System.IO;
using IOC.Core.Contracts.Persistence;
using IOC.Infrastructure.Persistence;
using Xunit;

namespace IOC.Tests;

public class StoragePersistenceTests : IDisposable
{
    private readonly string _testDir;
    private readonly JsonIocStorageService _storage;

    public StoragePersistenceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ioc_test_storage_" + Guid.NewGuid().ToString("N"));
        _storage = new JsonIocStorageService(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // Ignore test cleanup failures
        }
    }

    [Fact]
    public async Task SaveData_And_LoadData_PersistsAndRestoresObjects()
    {
        var testPayload = new List<string> { "Device_A", "Device_B", "Device_C" };

        await _storage.SaveDataAsync("test_devices", testPayload);
        var loaded = await _storage.LoadDataAsync<List<string>>("test_devices");

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Count);
        Assert.Equal("Device_A", loaded[0]);
    }

    [Fact]
    public async Task AppendAuditLog_SavesAndRetrievesInOrder()
    {
        var entry1 = new AuditLogEntry
        {
            OperatorId = "OP-01",
            OperatorName = "User 1",
            Subsystem = "LAN",
            Action = "REBOOT",
            Details = "Rebooted core switch"
        };
        var entry2 = new AuditLogEntry
        {
            OperatorId = "OP-02",
            OperatorName = "User 2",
            Subsystem = "PCCC",
            Action = "ACK",
            Details = "Acknowledged fire alarm"
        };

        await _storage.AppendAuditLogAsync(entry1);
        await _storage.AppendAuditLogAsync(entry2);

        var logs = await _storage.GetAuditLogsAsync();

        Assert.NotEmpty(logs);
        Assert.True(logs.Count >= 2);
        // Latest log should be first
        Assert.Equal("ACK", logs[0].Action);
        Assert.Equal("REBOOT", logs[1].Action);
    }

    [Fact]
    public async Task RecordAcknowledgement_PersistsAndLoadsRecords()
    {
        var ack = new IncidentAcknowledgementRecord
        {
            IncidentId = Guid.NewGuid(),
            Subsystem = "BMS",
            DeviceName = "CHILLER-01",
            IncidentTitle = "Quá nhiệt",
            OperatorId = "OP-8821",
            OperatorName = "Nguyễn Văn An"
        };

        await _storage.RecordAcknowledgementAsync(ack);
        var acks = await _storage.GetAcknowledgementsAsync();

        Assert.Single(acks);
        Assert.Equal("CHILLER-01", acks[0].DeviceName);
        Assert.Equal("OP-8821", acks[0].OperatorId);
    }

    [Fact]
    public async Task LoadData_CorruptedJsonFile_GracefullyReturnsDefaultWithoutCrash()
    {
        var corruptFilePath = Path.Combine(_testDir, "corrupted_key.json");
        await File.WriteAllTextAsync(corruptFilePath, "{ this is not valid json content [[[[ !!!");

        var result = await _storage.LoadDataAsync<List<string>>("corrupted_key");

        // Must catch JsonException and return null/default cleanly
        Assert.Null(result);
    }
}
