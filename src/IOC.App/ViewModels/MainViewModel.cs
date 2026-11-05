using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.Base;

namespace IOC.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISubsystemHealthMonitoringService _monitoringService;
    private readonly DispatcherTimer? _clockTimer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTabTitle))]
    private int _selectedTabIndex;

    public string CurrentTabTitle => SelectedTabIndex switch
    {
        0 => "Tổng Quan Hệ Thống",
        1 => "Mạng LAN & Viễn Thông",
        2 => "Hệ Thống Âm Thanh PA",
        3 => "Giám Sát Camera CCTV",
        4 => "Kiểm Soát Vào Ra & Thang Máy",
        5 => "Hệ Thống Cơ Điện BMS",
        6 => "Báo Cháy & Cứu Hỏa PCCC",
        7 => "Mô Phỏng & Diễn Tập Sự Cố",
        _ => "Tổng Quan Hệ Thống"
    };

    [RelayCommand]
    public void SelectTab(object? parameter)
    {
        if (parameter is int idx)
        {
            SelectedTabIndex = idx;
        }
        else if (parameter is string str && int.TryParse(str, out var parsed))
        {
            SelectedTabIndex = parsed;
        }
    }

    [ObservableProperty]
    private bool _isSidebarExpanded;

    [ObservableProperty]
    private double _sidebarWidth = 64;

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        SidebarWidth = IsSidebarExpanded ? 210 : 64;
    }

    [ObservableProperty]
    private bool _isKioskMode;

    [RelayCommand]
    public void ToggleKioskMode()
    {
        IsKioskMode = !IsKioskMode;
    }

    [ObservableProperty]
    private int _lanAlarmCount;

    [ObservableProperty]
    private int _paAlarmCount;

    [ObservableProperty]
    private int _cctvAlarmCount;

    [ObservableProperty]
    private int _accessAlarmCount;

    [ObservableProperty]
    private int _bmsAlarmCount;

    [ObservableProperty]
    private int _fireAlarmCount;

    [ObservableProperty]
    private int _totalActiveAlarmsCount;

    [ObservableProperty]
    private bool _isTelemetryLive = true;

    [ObservableProperty]
    private string _telemetryHeartbeatText = "● Telemetry Live (8ms)";

    [ObservableProperty]
    private string _lastUpdateText = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    // Two-step Safety Confirmation Modal
    [ObservableProperty]
    private bool _isConfirmModalOpen;

    [ObservableProperty]
    private string _confirmModalTitle = string.Empty;

    [ObservableProperty]
    private string _confirmModalMessage = string.Empty;

    [ObservableProperty]
    private string _confirmModalWarning = string.Empty;

    [ObservableProperty]
    private string _confirmModalSubsystem = string.Empty;

    [ObservableProperty]
    private string _confirmModalActionText = "Xác Nhận Thực Hiện";

    private Func<Task>? _pendingDangerousAction;

    public void RequestDangerousConfirmation(string title, string message, string warning, string subsystem, Func<Task> action, string actionText = "Xác Nhận Thực Hiện")
    {
        ConfirmModalTitle = title;
        ConfirmModalMessage = message;
        ConfirmModalWarning = warning;
        ConfirmModalSubsystem = subsystem;
        ConfirmModalActionText = actionText;
        _pendingDangerousAction = action;
        IsConfirmModalOpen = true;
    }

    [RelayCommand]
    public async Task ExecuteConfirmedActionAsync()
    {
        IsConfirmModalOpen = false;
        if (_pendingDangerousAction != null)
        {
            var act = _pendingDangerousAction;
            _pendingDangerousAction = null;
            await act();
            await RefreshAllAsync();
        }
    }

    [RelayCommand]
    public void CancelConfirmation()
    {
        IsConfirmModalOpen = false;
        _pendingDangerousAction = null;
    }

    [ObservableProperty]
    private OverallHealthSummary? _overallSummary;

    [ObservableProperty]
    private SubsystemIncident? _topActiveAlert;

    [ObservableProperty]
    private bool _hasTopAlert;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string _currentTimeString = string.Empty;

    public OverviewTabViewModel OverviewTab { get; }
    public LanTelTabViewModel LanTelTab { get; }
    public PublicAddressTabViewModel PublicAddressTab { get; }
    public CctvTabViewModel CctvTab { get; }
    public AccessControlTabViewModel AccessControlTab { get; }
    public BmsTabViewModel BmsTab { get; }
    public FireAlarmTabViewModel FireAlarmTab { get; }
    public SimulationTabViewModel SimulationTab { get; }

    public MainViewModel(ISubsystemHealthMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;

        OverviewTab = new OverviewTabViewModel(_monitoringService, NavigateToTab);
        LanTelTab = new LanTelTabViewModel(_monitoringService);
        PublicAddressTab = new PublicAddressTabViewModel(_monitoringService);
        CctvTab = new CctvTabViewModel(_monitoringService);
        AccessControlTab = new AccessControlTabViewModel(_monitoringService, RequestDangerousConfirmation);
        BmsTab = new BmsTabViewModel(_monitoringService, RequestDangerousConfirmation);
        FireAlarmTab = new FireAlarmTabViewModel(_monitoringService);
        SimulationTab = new SimulationTabViewModel(_monitoringService, NavigateToTab);

        _monitoringService.HealthUpdated += OnHealthUpdated;
        _monitoringService.IncidentRaised += OnIncidentRaised;

        UpdateClock();

        if (System.Windows.Application.Current != null)
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
        }
    }

    public async Task InitializeAsync()
    {
        await _monitoringService.InitializeAsync();
        await RefreshAllAsync();
    }

    public void UpdateClock()
    {
        CurrentTimeString = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private void OnHealthUpdated(OverallHealthSummary summary)
    {
        void Update()
        {
            OverallSummary = summary;
            UpdateTopAlert();
        }

        if (System.Windows.Application.Current?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }

    private void OnIncidentRaised(SubsystemIncident incident)
    {
        void Update()
        {
            UpdateTopAlert();
        }

        if (System.Windows.Application.Current?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }

    private async void UpdateTopAlert()
    {
        var incidents = await _monitoringService.GetActiveIncidentsAsync();
        TopActiveAlert = incidents.FirstOrDefault(i => !i.IsAcknowledged);
        HasTopAlert = TopActiveAlert != null;

        TotalActiveAlarmsCount = incidents.Count;
        LanAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.LanTel);
        PaAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.PublicAddress);
        CctvAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.Cctv);
        AccessAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.AccessControl);
        BmsAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.Bms);
        FireAlarmCount = incidents.Count(i => i.Subsystem == SubsystemType.FireAlarm);

        LastUpdateText = $"Cập nhật lúc: {DateTime.Now:HH:mm:ss}";
    }

    public void NavigateToTab(int index)
    {
        SelectedTabIndex = index;
    }

    [RelayCommand]
    public async Task RefreshAllAsync()
    {
        IsRefreshing = true;
        try
        {
            await _monitoringService.RefreshAllAsync();
            OverallSummary = await _monitoringService.GetOverallSummaryAsync();

            await OverviewTab.LoadDataAsync();
            await LanTelTab.LoadDataAsync();
            await PublicAddressTab.LoadDataAsync();
            await CctvTab.LoadDataAsync();
            await AccessControlTab.LoadDataAsync();
            await BmsTab.LoadDataAsync();
            await FireAlarmTab.LoadDataAsync();
            await SimulationTab.RefreshSimulationStatusAsync();

            UpdateTopAlert();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    [RelayCommand]
    public async Task AcknowledgeTopAlertAsync()
    {
        if (TopActiveAlert != null)
        {
            await _monitoringService.AcknowledgeIncidentAsync(TopActiveAlert.Id, "Trực ban Vận hành");
            await RefreshAllAsync();
        }
    }

    [RelayCommand]
    public async Task QuickSimulateScenarioAsync(string scenario)
    {
        switch (scenario)
        {
            case "FIRE_ALERT":
                await _monitoringService.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");
                SelectedTabIndex = 6; // Chuyển sang Tab Báo cháy PCCC
                break;
            case "SWITCH_DOWN":
                await _monitoringService.TriggerDiagnosticFaultAsync("SW-CORE-01", "PORT_DOWN");
                SelectedTabIndex = 1; // Chuyển sang Tab Mạng LAN
                break;
            case "CHILLER_FAULT":
                await _monitoringService.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "CHILLER_OVERHEAT");
                SelectedTabIndex = 5; // Chuyển sang Tab BMS
                break;
            case "DOOR_FORCED":
                await _monitoringService.TriggerDiagnosticFaultAsync("AC-CTRL-01", "DOOR_FORCED");
                SelectedTabIndex = 4; // Chuyển sang Tab Access Control
                break;
            case "CLEAR_ALL":
                await _monitoringService.TriggerDiagnosticFaultAsync("ALL", "CLEAR_ALL");
                break;
        }

        await RefreshAllAsync();
    }
}

