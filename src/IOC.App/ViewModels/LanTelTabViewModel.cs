using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.LanTel;

namespace IOC.App.ViewModels;

public partial class LanTelTabViewModel : SubsystemTabViewModelBase<LanTelDevice>
{
    public override SubsystemType Subsystem => SubsystemType.LanTel;

    [ObservableProperty]
    private SwitchPortInfo? _selectedPort;

    [ObservableProperty]
    private PbxExtensionInfo? _selectedExtension;

    [ObservableProperty]
    private string _portFilterMode = "ALL"; // ALL, UP, DOWN, CONGESTED

    [ObservableProperty]
    private PbxTrunkInfo? _currentPbxTrunk;

    [ObservableProperty]
    private int _totalSwitchPorts;

    [ObservableProperty]
    private int _upSwitchPorts;

    [ObservableProperty]
    private int _downSwitchPorts;

    [ObservableProperty]
    private int _congestedSwitchPorts;

    public ObservableCollection<SwitchPortInfo> FilteredPorts { get; } = [];
    public ObservableCollection<PbxExtensionInfo> PbxExtensions { get; } = [];

    public LanTelTabViewModel(ISubsystemHealthMonitoringService monitoringService)
        : base(monitoringService)
    {
        Title = "Mạng LAN & Viễn Thông";
        Subtitle = "Giám sát Switch Core/Edge, đứt cáp tức thời, quá tải CPU/RAM/Nhiệt & Tổng đài IP-PBX";
        IconKind = "ServerNetwork";
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();

        var pbxDevice = Devices.FirstOrDefault(d => d.DeviceType == LanDeviceType.IpPbx);
        CurrentPbxTrunk = pbxDevice?.PbxTrunk;

        PbxExtensions.Clear();
        if (pbxDevice != null)
        {
            foreach (var ext in pbxDevice.Extensions)
            {
                PbxExtensions.Add(ext);
            }
        }

        UpdatePortStatistics();
        ApplyPortFilter();
    }

    protected override void OnSelectedDeviceChangedInternal(LanTelDevice? value)
    {
        UpdatePortStatistics();
        ApplyPortFilter();
    }

    private void UpdatePortStatistics()
    {
        if (SelectedDevice != null)
        {
            TotalSwitchPorts = SelectedDevice.TotalPortsCount;
            UpSwitchPorts = SelectedDevice.UpPortsCount;
            DownSwitchPorts = SelectedDevice.DownPortsCount;
            CongestedSwitchPorts = SelectedDevice.CongestedPortsCount;
        }
        else
        {
            TotalSwitchPorts = 0;
            UpSwitchPorts = 0;
            DownSwitchPorts = 0;
            CongestedSwitchPorts = 0;
        }
    }

    [RelayCommand]
    public void SetPortFilter(string filter)
    {
        PortFilterMode = filter;
        ApplyPortFilter();
    }

    private void ApplyPortFilter()
    {
        FilteredPorts.Clear();
        if (SelectedDevice == null) return;

        var query = SelectedDevice.Ports.AsEnumerable();
        switch (PortFilterMode.ToUpperInvariant())
        {
            case "UP":
                query = query.Where(p => p.IsUp);
                break;
            case "DOWN":
                query = query.Where(p => !p.IsUp && p.IsAdminEnabled);
                break;
            case "CONGESTED":
                query = query.Where(p => p.IsCongested);
                break;
        }

        foreach (var port in query)
        {
            FilteredPorts.Add(port);
        }

        if (SelectedPort == null || !FilteredPorts.Contains(SelectedPort))
        {
            SelectedPort = FilteredPorts.FirstOrDefault();
        }
    }

    [RelayCommand]
    public async Task TogglePortCableAsync(SwitchPortInfo? port)
    {
        if (port == null || SelectedDevice == null) return;

        port.IsUp = !port.IsUp;
        port.CableFaultTimestamp = port.IsUp ? null : DateTime.UtcNow;

        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task SimulatePortCongestionAsync(SwitchPortInfo? port)
    {
        if (port == null || SelectedDevice == null) return;

        if (port.IsCongested)
        {
            port.RxRateMbps = 25.0;
            port.TxRateMbps = 15.0;
        }
        else
        {
            port.RxRateMbps = 580.0;
            port.TxRateMbps = 360.0; // 940 Mbps = 94% on Gigabit
        }

        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task ToggleSipTrunkAsync()
    {
        var pbxDevice = Devices.FirstOrDefault(d => d.DeviceType == LanDeviceType.IpPbx);
        if (pbxDevice?.PbxTrunk == null) return;

        pbxDevice.PbxTrunk.IsSipTrunkUp = !pbxDevice.PbxTrunk.IsSipTrunkUp;
        pbxDevice.PbxTrunk.DownTimestamp = pbxDevice.PbxTrunk.IsSipTrunkUp ? null : DateTime.UtcNow;

        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task ToggleExtensionStatusAsync(PbxExtensionInfo? ext)
    {
        if (ext == null) return;

        ext.Status = ext.Status == PbxExtensionStatus.Disconnected
            ? PbxExtensionStatus.Registered
            : PbxExtensionStatus.Disconnected;

        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task SimulateHardwareFaultAsync(string faultType)
    {
        if (SelectedDevice == null) return;

        switch (faultType.ToUpperInvariant())
        {
            case "CPU_OVERLOAD":
                SelectedDevice.CpuUsagePercent = SelectedDevice.CpuUsagePercent > 90 ? 32.0 : 95.8;
                break;
            case "RAM_OVERFLOW":
                SelectedDevice.MemoryUsedMb = SelectedDevice.MemoryUsagePercent > 90
                    ? SelectedDevice.MemoryTotalMb * 0.35
                    : SelectedDevice.MemoryTotalMb * 0.94;
                break;
            case "OVERHEAT":
                SelectedDevice.TemperatureCelsius = SelectedDevice.TemperatureCelsius > 70 ? 43.0 : 81.5;
                break;
            case "RESTORE_ALL":
                SelectedDevice.CpuUsagePercent = 32.0;
                SelectedDevice.MemoryUsedMb = SelectedDevice.MemoryTotalMb * 0.35;
                SelectedDevice.TemperatureCelsius = 43.0;
                SelectedDevice.BandwidthUtilizationPercent = 42.0;
                foreach (var p in SelectedDevice.Ports)
                {
                    p.IsUp = p.IsAdminEnabled;
                    p.RxRateMbps = 15.0;
                    p.TxRateMbps = 10.0;
                    p.CableFaultTimestamp = null;
                }
                break;
        }

        await MonitoringService.RefreshAllAsync();
        await LoadDataAsync();
    }
}
