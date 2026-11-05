using IOC.Core.Enums;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class SubsystemHealthMonitoringServiceTests
{
    [Fact]
    public async Task Service_InitializesAll6SubsystemsWithDevices()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);

        await service.InitializeAsync();

        var lanDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.LanTel);
        var paDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.PublicAddress);
        var cctvDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.Cctv);
        var acDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.AccessControl);
        var bmsDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.Bms);
        var fireDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.FireAlarm);

        Assert.NotEmpty(lanDevices);
        Assert.NotEmpty(paDevices);
        Assert.NotEmpty(cctvDevices);
        Assert.NotEmpty(acDevices);
        Assert.NotEmpty(bmsDevices);
        Assert.NotEmpty(fireDevices);

        var overall = await service.GetOverallSummaryAsync();
        Assert.Equal(6, overall.TotalSubsystems);
        Assert.True(overall.TotalDevices >= 20);
    }

    [Fact]
    public async Task Service_TriggerDiagnosticFault_GeneratesIncidentAndRaisesEvent()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        bool eventRaised = false;
        service.IncidentRaised += (inc) =>
        {
            if (inc.DeviceId == "SW-CORE-01") eventRaised = true;
        };

        await service.TriggerDiagnosticFaultAsync("SW-CORE-01", "PORT_DOWN");

        var incidents = await service.GetActiveIncidentsAsync(SubsystemType.LanTel);
        Assert.NotEmpty(incidents);
        Assert.True(eventRaised);

        var inc = incidents.First();
        await service.AcknowledgeIncidentAsync(inc.Id, "Kỹ Sư Trực Ban Nguyễn Văn A");

        var updatedInc = (await service.GetActiveIncidentsAsync(SubsystemType.LanTel)).First(i => i.Id == inc.Id);
        Assert.True(updatedInc.IsAcknowledged);
        Assert.Equal("Kỹ Sư Trực Ban Nguyễn Văn A", updatedInc.AcknowledgedBy);
    }

    [Fact]
    public async Task Service_ResolveIncident_RemovesFromActiveList()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");

        var activeBefore = await service.GetActiveIncidentsAsync(SubsystemType.FireAlarm);
        Assert.NotEmpty(activeBefore);

        var fireInc = activeBefore.First();
        await service.ResolveIncidentAsync(fireInc.Id);

        var activeAfter = await service.GetActiveIncidentsAsync(SubsystemType.FireAlarm);
        Assert.DoesNotContain(activeAfter, i => i.Id == fireInc.Id);
    }
}

