using System.IO;
using IOC.Core.Enums;
using IOC.Infrastructure.Persistence;
using IOC.Infrastructure.Services;
using IOC.Infrastructure.Session;
using Xunit;

namespace IOC.Tests;

public class IncidentLifecycleAndSuppressionTests : IDisposable
{
    private readonly string _testDir;
    private readonly JsonIocStorageService _storage;
    private readonly OperatorSessionService _session;
    private readonly SubsystemHealthMonitoringService _service;

    public IncidentLifecycleAndSuppressionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ioc_test_lifecycle_" + Guid.NewGuid().ToString("N"));
        _storage = new JsonIocStorageService(_testDir);
        _session = new OperatorSessionService();
        var evaluator = new SubsystemHealthEvaluator();
        _service = new SubsystemHealthMonitoringService(evaluator, _storage, _session);
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
        catch { }
    }

    [Fact]
    public async Task DuplicateIncident_TriggeredMultipleTimes_SuppressesDuplicates()
    {
        await _service.InitializeAsync();

        // Trigger the exact same fault 5 times
        for (int i = 0; i < 5; i++)
        {
            await _service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");
        }

        var activeIncidents = await _service.GetActiveIncidentsAsync(SubsystemType.FireAlarm);

        // All 5 triggers should be deduplicated into a single active alarm
        Assert.Single(activeIncidents);
        Assert.Equal("FAS-PANEL-01", activeIncidents[0].DeviceId);
    }

    [Fact]
    public async Task IncidentLifecycle_NewToAcknowledgedToResolved()
    {
        await _service.InitializeAsync();

        // 1. Trigger incident
        await _service.TriggerDiagnosticFaultAsync("SW-CORE-01", "PORT_DOWN");
        var activeIncidents = await _service.GetActiveIncidentsAsync(SubsystemType.LanTel);
        Assert.Single(activeIncidents);

        var incident = activeIncidents[0];
        Assert.False(incident.IsAcknowledged);
        Assert.False(incident.IsResolved);

        // 2. Acknowledge incident
        await _service.AcknowledgeIncidentAsync(incident.Id, "Nguyễn Văn An");
        var ackedIncidents = await _service.GetActiveIncidentsAsync(SubsystemType.LanTel);
        Assert.Single(ackedIncidents);
        Assert.True(ackedIncidents[0].IsAcknowledged);
        Assert.Equal("Nguyễn Văn An", ackedIncidents[0].AcknowledgedBy);

        // 3. Resolve incident
        await _service.ResolveIncidentAsync(incident.Id);
        var resolvedActiveIncidents = await _service.GetActiveIncidentsAsync(SubsystemType.LanTel);
        Assert.Empty(resolvedActiveIncidents);

        // 4. Check audit logs were created
        var auditLogs = await _storage.GetAuditLogsAsync();
        Assert.Contains(auditLogs, a => a.Action.Contains("ACK"));
        Assert.Contains(auditLogs, a => a.Action.Contains("RESOLVE"));
    }
}
