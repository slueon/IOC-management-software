using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.Base;

namespace IOC.App.ViewModels.Base;

public abstract partial class SubsystemTabViewModelBase<TDevice>(ISubsystemHealthMonitoringService monitoringService) : ViewModelBase
    where TDevice : SubsystemDevice
{
    protected readonly ISubsystemHealthMonitoringService MonitoringService = monitoringService;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private string _iconKind = "ViewDashboard";

    [ObservableProperty]
    private SubsystemHealthSummary? _summary;

    [ObservableProperty]
    private TDevice? _selectedDevice;

    partial void OnSelectedDeviceChanged(TDevice? value)
    {
        OnSelectedDeviceChangedInternal(value);
    }

    protected virtual void OnSelectedDeviceChangedInternal(TDevice? value)
    {
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _badgeText = "0";

    [ObservableProperty]
    private bool _hasAlarms;

    [ObservableProperty]
    private bool _hasCriticalAlarms;

    public ObservableCollection<TDevice> Devices { get; } = [];
    public ObservableCollection<SubsystemIncident> ActiveIncidents { get; } = [];

    public abstract SubsystemType Subsystem { get; }

    public virtual async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var rawDevices = await MonitoringService.GetDevicesBySubsystemAsync(Subsystem);
            RunOnUIThread(() =>
            {
                Devices.Clear();
                foreach (var d in rawDevices.OfType<TDevice>())
                {
                    Devices.Add(d);
                }

                if (SelectedDevice == null && Devices.Any())
                {
                    SelectedDevice = Devices.First();
                }
            });

            var rawIncidents = await MonitoringService.GetActiveIncidentsAsync(Subsystem);
            RunOnUIThread(() =>
            {
                ActiveIncidents.Clear();
                foreach (var inc in rawIncidents)
                {
                    ActiveIncidents.Add(inc);
                }
            });

            Summary = await MonitoringService.GetSubsystemSummaryAsync(Subsystem);
            UpdateBadgeStatus();
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected static void RunOnUIThread(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    protected void UpdateBadgeStatus()
    {
        if (Summary == null) return;

        HasAlarms = Summary.ActiveIncidentsCount > 0;
        HasCriticalAlarms = Summary.CriticalIncidentsCount > 0;
        BadgeText = Summary.ActiveIncidentsCount > 0
            ? $"{Summary.ActiveIncidentsCount} sự cố"
            : "Bình thường";
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task AcknowledgeIncidentAsync(SubsystemIncident? incident)
    {
        if (incident == null) return;
        await MonitoringService.AcknowledgeIncidentAsync(incident.Id, "Trực ban Vận hành");
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task ResolveIncidentAsync(SubsystemIncident? incident)
    {
        if (incident == null) return;
        await MonitoringService.ResolveIncidentAsync(incident.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task SimulateFaultAsync(string scenario)
    {
        if (SelectedDevice != null)
        {
            await MonitoringService.TriggerDiagnosticFaultAsync(SelectedDevice.Id, scenario);
            await LoadDataAsync();
        }
    }
}

