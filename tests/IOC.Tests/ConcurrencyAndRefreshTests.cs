using IOC.App.ViewModels;
using IOC.Core.Enums;
using IOC.Infrastructure.Services;
using IOC.Infrastructure.Session;
using Xunit;

namespace IOC.Tests;

public class ConcurrencyAndRefreshTests
{
    [Fact]
    public async Task RefreshAllAsync_ConcurrentRapidCalls_SafelyGuardedBySemaphore()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var session = new OperatorSessionService();
        var vm = new MainViewModel(service, session);
        await vm.InitializeAsync();

        // Spawn 20 parallel tasks rapidly invoking RefreshAllAsync
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () => await vm.RefreshAllAsync()))
            .ToArray();

        // Must complete without throwing any race-condition or collection modification exceptions
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(exception);
        Assert.False(vm.IsRefreshing);
        Assert.NotNull(vm.OverallSummary);
    }

    [Fact]
    public async Task MonitoringService_ConcurrentFaults_ThreadSafe()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        var scenarios = new[]
        {
            ("SW-CORE-01", "PORT_DOWN"),
            ("BMS-CHILLER-01", "CHILLER_OVERHEAT"),
            ("AC-CTRL-01", "DOOR_FORCED"),
            ("FAS-PANEL-01", "FIRE_ALARM"),
            ("NVR-64CH-01", "RAID_DEGRADED")
        };

        // 25 tasks concurrently triggering faults
        var tasks = Enumerable.Range(0, 25).Select(i =>
        {
            var (device, scenario) = scenarios[i % scenarios.Length];
            return Task.Run(async () => await service.TriggerDiagnosticFaultAsync(device, scenario));
        }).ToArray();

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(exception);

        var activeIncidents = await service.GetActiveIncidentsAsync();
        Assert.NotEmpty(activeIncidents);
    }
}
