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

    [Fact]
    public void MainViewModel_SidebarToggle_ExpandsAndCollapses()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);

        Assert.False(vm.IsSidebarExpanded);
        Assert.Equal(64, vm.SidebarWidth);

        vm.ToggleSidebar();
        Assert.True(vm.IsSidebarExpanded);
        Assert.Equal(210, vm.SidebarWidth);

        vm.ToggleSidebar();
        Assert.False(vm.IsSidebarExpanded);
        Assert.Equal(64, vm.SidebarWidth);
    }

    [Fact]
    public void MainViewModel_KioskToggle_TogglesState()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);

        Assert.False(vm.IsKioskMode);
        vm.ToggleKioskMode();
        Assert.True(vm.IsKioskMode);
        vm.ToggleKioskMode();
        Assert.False(vm.IsKioskMode);
    }

    [Fact]
    public async Task MainViewModel_DangerousConfirmation_ProtectsCriticalOperations()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);
        await vm.InitializeAsync();

        bool actionExecuted = false;
        vm.RequestDangerousConfirmation(
            "XẢ CỬA KHẨN CẤP",
            "Mở khóa toàn bộ cửa?",
            "CẢNH BÁO",
            "Access Control",
            () => { actionExecuted = true; return Task.CompletedTask; }
        );

        Assert.True(vm.IsConfirmModalOpen);
        Assert.Equal("XẢ CỬA KHẨN CẤP", vm.ConfirmModalTitle);
        Assert.False(actionExecuted);

        // Cancel
        vm.CancelConfirmation();
        Assert.False(vm.IsConfirmModalOpen);
        Assert.False(actionExecuted);

        // Request again: Test 2-step protection
        vm.RequestDangerousConfirmation(
            "XẢ CỬA KHẨN CẤP",
            "Mở khóa toàn bộ cửa?",
            "CẢNH BÁO",
            "Access Control",
            () => { actionExecuted = true; return Task.CompletedTask; }
        );
        Assert.False(vm.IsConfirmedByOperator);
        Assert.NotEmpty(vm.OperatorIdentity);
        Assert.NotEmpty(vm.ConfirmationTimestamp);

        // Attempt execution without operator checkbox confirmation -> MUST BE BLOCKED
        await vm.ExecuteConfirmedActionCommand.ExecuteAsync(null);
        Assert.True(vm.IsConfirmModalOpen);
        Assert.False(actionExecuted);

        // Operator explicitly checks confirmation checkbox -> ALLOW EXECUTION
        vm.IsConfirmedByOperator = true;
        await vm.ExecuteConfirmedActionCommand.ExecuteAsync(null);

        Assert.False(vm.IsConfirmModalOpen);
        Assert.False(vm.IsConfirmedByOperator);
        Assert.True(actionExecuted);
    }

    [Fact]
    public void MainViewModel_ToggleDarkMode_TogglesState()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);

        Assert.False(vm.IsDarkMode);
        vm.ToggleDarkMode();
        Assert.True(vm.IsDarkMode);
        vm.ToggleDarkMode();
        Assert.False(vm.IsDarkMode);
    }

    [Fact]
    public async Task MainViewModel_DynamicTelemetry_CalculatesLatency()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);
        await vm.InitializeAsync();

        Assert.True(vm.TelemetryLatencyMs > 0);
        Assert.Contains("Telemetry Live", vm.TelemetryHeartbeatText);
        Assert.Contains("Độ trễ:", vm.TelemetryFooterStatus);
    }

    [Fact]
    public async Task OverviewTab_TriageFilter_FiltersCorrectly()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var vm = new MainViewModel(service);
        await vm.InitializeAsync();

        // Kích hoạt sự cố
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");
        await service.TriggerDiagnosticFaultAsync("SW-CORE-01", "PORT_DOWN");
        await vm.RefreshAllAsync();

        var overview = vm.OverviewTab;
        Assert.True(overview.TopIncidents.Count >= 2);
        Assert.True(overview.UnackCount >= 2);

        // Filter: UNACK
        overview.SetTriageFilter("UNACK");
        Assert.All(overview.FilteredIncidents, inc => Assert.False(inc.IsAcknowledged));

        // ACK 1 incident
        var first = overview.FilteredIncidents.First();
        await overview.AcknowledgeIncidentCommand.ExecuteAsync(first);

        // Filter: ACK
        overview.SetTriageFilter("ACK");
        Assert.All(overview.FilteredIncidents, inc => Assert.True(inc.IsAcknowledged));
        Assert.Contains(overview.FilteredIncidents, i => i.Id == first.Id);

        // Filter: ALL
        overview.SetTriageFilter("ALL");
        Assert.Equal(overview.TopIncidents.Count, overview.FilteredIncidents.Count);
    }
}

