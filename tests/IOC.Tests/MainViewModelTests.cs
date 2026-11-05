using IOC.App.ViewModels;
using IOC.Core.Enums;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task MainViewModel_InitializesAll6TabsAndLoadsData()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);

        await vm.InitializeAsync();

        Assert.NotNull(vm.OverviewTab);
        Assert.NotNull(vm.LanTelTab);
        Assert.NotNull(vm.PublicAddressTab);
        Assert.NotNull(vm.CctvTab);
        Assert.NotNull(vm.AccessControlTab);
        Assert.NotNull(vm.BmsTab);
        Assert.NotNull(vm.FireAlarmTab);
        Assert.NotNull(vm.SimulationTab);

        Assert.NotEmpty(vm.LanTelTab.Devices);
        Assert.NotEmpty(vm.PublicAddressTab.Devices);
        Assert.NotEmpty(vm.CctvTab.Devices);
        Assert.NotEmpty(vm.AccessControlTab.Devices);
        Assert.NotEmpty(vm.BmsTab.Devices);
        Assert.NotEmpty(vm.FireAlarmTab.Devices);

        Assert.Equal(0, vm.SelectedTabIndex);
    }

    [Fact]
    public async Task MainViewModel_QuickSimulateScenario_SwitchesToAppropriateTab()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);
        await vm.InitializeAsync();

        await vm.QuickSimulateScenarioAsync("FIRE_ALERT");
        Assert.Equal(6, vm.SelectedTabIndex);
        Assert.True(vm.FireAlarmTab.HasAlarms);

        await vm.QuickSimulateScenarioAsync("SWITCH_DOWN");
        Assert.Equal(1, vm.SelectedTabIndex);
        Assert.True(vm.LanTelTab.HasAlarms);
    }
}

