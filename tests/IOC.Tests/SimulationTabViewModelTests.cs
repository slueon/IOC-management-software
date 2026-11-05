using IOC.App.ViewModels;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class SimulationTabViewModelTests
{
    [Fact]
    public async Task SimulationTab_Initializes_WithZeroFaultsAndLoadsStatus()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        var vm = new SimulationTabViewModel(service);
        await vm.RefreshSimulationStatusAsync();

        Assert.Equal(0, vm.ActiveIncidentsCount);
        Assert.NotNull(vm.SimulationLogs);
        Assert.Equal("Hệ thống hoạt động bình thường", vm.LastTriggeredAction);
    }

    [Fact]
    public async Task SimulationTab_TriggerScenario_FireSmoke_UpdatesCountsAndAddsLog()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        var vm = new SimulationTabViewModel(service);

        await vm.TriggerScenarioAsync("FIRE_SMOKE");

        Assert.True(vm.ActiveIncidentsCount > 0);
        Assert.True(vm.CriticalCount > 0);
        Assert.NotEmpty(vm.SimulationLogs);
        Assert.Contains("FIRE_SMOKE", vm.LastTriggeredAction);
    }

    [Fact]
    public async Task SimulationTab_TriggerMultipleScenarios_And_ResetAll_ClearsAllFaults()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        var vm = new SimulationTabViewModel(service);

        // Kích hoạt nhiều kịch bản
        await vm.TriggerScenarioAsync("CHILLER_OVERHEAT");
        await vm.TriggerScenarioAsync("DOOR_FORCED");
        await vm.TriggerScenarioAsync("PORT_DOWN");

        Assert.True(vm.ActiveIncidentsCount >= 3);

        // Khôi phục toàn bộ
        await vm.ResetAllAsync();

        Assert.Equal(0, vm.ActiveIncidentsCount);
        Assert.Equal(0, vm.CriticalCount);
        Assert.Equal(0, vm.MajorCount);
        Assert.Contains("Đã khôi phục toàn bộ", vm.LastTriggeredAction);
    }

    [Fact]
    public void SimulationTab_NavigateToSubsystem_InvokesCallback()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        int targetTab = -1;

        var vm = new SimulationTabViewModel(service, idx => targetTab = idx);
        vm.NavigateToSubsystem(3);

        Assert.Equal(3, targetTab);
    }

    [Fact]
    public void SimulationTab_NavigateToSubsystemCommand_AcceptsStringParameterFromXaml()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        int targetTab = -1;

        var vm = new SimulationTabViewModel(service, idx => targetTab = idx);

        // String parameter from XAML CommandParameter="4" must be supported without ArgumentException
        Assert.True(vm.NavigateToSubsystemCommand.CanExecute("4"));
        vm.NavigateToSubsystemCommand.Execute("4");
        Assert.Equal(4, targetTab);

        // Integer parameter direct execution
        Assert.True(vm.NavigateToSubsystemCommand.CanExecute(2));
        vm.NavigateToSubsystemCommand.Execute(2);
        Assert.Equal(2, targetTab);

        // Invalid string or null must not throw ArgumentException
        Assert.True(vm.NavigateToSubsystemCommand.CanExecute(null));
        vm.NavigateToSubsystemCommand.Execute(null);
        Assert.Equal(2, targetTab);

        Assert.True(vm.NavigateToSubsystemCommand.CanExecute("invalid_number"));
        vm.NavigateToSubsystemCommand.Execute("invalid_number");
        Assert.Equal(2, targetTab);
    }
}

