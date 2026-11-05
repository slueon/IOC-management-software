using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.Base;

namespace IOC.App.ViewModels;

public class ActionLogItem
{
    public int Registry { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubsystemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = "Thành công";
}

public class CriticalSensorItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subsystem { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";
    public string Metric { get; set; } = string.Empty;
}

public partial class OverviewTabViewModel : ViewModelBase
{
    private readonly ISubsystemHealthMonitoringService _monitoringService;
    private readonly Action<int> _navigateToTabAction;

    [ObservableProperty]
    private OverallHealthSummary? _overallSummary;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SubsystemIncident? _selectedIncident;

    public ObservableCollection<SubsystemHealthSummary> SubsystemCards { get; } = [];
    public ObservableCollection<SubsystemIncident> TopIncidents { get; } = [];
    public ObservableCollection<ActionLogItem> ActionLogs { get; } = [];
    public ObservableCollection<CriticalSensorItem> CriticalSensors { get; } = [];

    public OverviewTabViewModel(ISubsystemHealthMonitoringService monitoringService, Action<int> navigateToTabAction)
    {
        _monitoringService = monitoringService;
        _navigateToTabAction = navigateToTabAction;
        InitializeMockLogsAndSensors();
    }

    private void InitializeMockLogsAndSensors()
    {
        ActionLogs.Clear();
        ActionLogs.Add(new ActionLogItem { Registry = 101, OperatorName = "Nguyễn Văn An", Role = "Trực Ban IOC", Category = "Kiểm Soát", SubsystemName = "Access Control", Description = "Xác thực quẹt thẻ sảnh chính T1", Timestamp = DateTime.Now.AddMinutes(-2).ToString("HH:mm:ss") });
        ActionLogs.Add(new ActionLogItem { Registry = 102, OperatorName = "Trần Quốc Bảo", Role = "Kỹ Sư Hạ Tầng", Category = "Đo Kiểm", SubsystemName = "Mạng LAN / Tel", Description = "Kiểm tra cổng Core Switch 24P", Timestamp = DateTime.Now.AddMinutes(-5).ToString("HH:mm:ss") });
        ActionLogs.Add(new ActionLogItem { Registry = 103, OperatorName = "Lê Hoàng Minh", Role = "Trực Ban PCCC", Category = "An Toàn", SubsystemName = "Báo Cháy PCCC", Description = "Giám sát mạch vòng Loop 1 & 2", Timestamp = DateTime.Now.AddMinutes(-12).ToString("HH:mm:ss") });
        ActionLogs.Add(new ActionLogItem { Registry = 104, OperatorName = "Phạm Đức Huy", Role = "Kỹ Sư Cơ Điện", Category = "Vận Hành", SubsystemName = "Cơ Điện BMS", Description = "Đọc Modbus nhiệt độ Chiller", Timestamp = DateTime.Now.AddMinutes(-20).ToString("HH:mm:ss") });
        ActionLogs.Add(new ActionLogItem { Registry = 105, OperatorName = "Đỗ Minh Trí", Role = "Giám Sát An Ninh", Category = "Giám Sát", SubsystemName = "Camera CCTV", Description = "Kiểm tra luồng RTSP đầu ghi NVR", Timestamp = DateTime.Now.AddMinutes(-32).ToString("HH:mm:ss") });
        ActionLogs.Add(new ActionLogItem { Registry = 106, OperatorName = "Hệ thống Tự động", Role = "Giám Sát Định Kỳ", Category = "Tự Động", SubsystemName = "Âm Thanh PA", Description = "Đo trở kháng đường dây loa các Zone", Timestamp = DateTime.Now.AddMinutes(-45).ToString("HH:mm:ss") });

        CriticalSensors.Clear();
        CriticalSensors.Add(new CriticalSensorItem { Code = "SW-CORE-01", Name = "Cisco Catalyst 24P", Subsystem = "Mạng LAN / Tel", Location = "Phòng Server T1", IpAddress = "192.168.1.1", Status = "Online", Metric = "Port: 21/24 Up" });
        CriticalSensors.Add(new CriticalSensorItem { Code = "PA-CTRL-01", Name = "Bosch Praesideo Controller", Subsystem = "Âm Thanh PA", Location = "Tủ Rack T1", IpAddress = "192.168.1.50", Status = "Online", Metric = "Pin: 27.2V | Tải: 42%" });
        CriticalSensors.Add(new CriticalSensorItem { Code = "NVR-64CH-01", Name = "Hikvision Embedded NVR", Subsystem = "Camera CCTV", Location = "Phòng An Ninh T1", IpAddress = "192.168.1.60", Status = "Online", Metric = "RAID5 Normal | 4 HDD" });
        CriticalSensors.Add(new CriticalSensorItem { Code = "AC-CTRL-01", Name = "Honeywell Door Controller", Subsystem = "Access Control", Location = "Sảnh Chính T1", IpAddress = "192.168.1.100", Status = "Online", Metric = "Ắc quy: 98% | 4 Cửa Khóa" });
        CriticalSensors.Add(new CriticalSensorItem { Code = "BMS-CHILLER-01", Name = "Trane Central Chiller", Subsystem = "Cơ Điện BMS", Location = "Tầng Hầm B1", IpAddress = "192.168.1.200", Status = "Online", Metric = "Cấp: 7.2°C | Hồi: 12.1°C" });
        CriticalSensors.Add(new CriticalSensorItem { Code = "FAS-PANEL-01", Name = "Hochiki Fire Alarm Panel", Subsystem = "Báo Cháy PCCC", Location = "Trực Ban PCCC T1", IpAddress = "192.168.1.250", Status = "Online", Metric = "Loop 1 & 2 Normal" });
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            OverallSummary = await _monitoringService.GetOverallSummaryAsync();

            SubsystemCards.Clear();
            foreach (var summary in OverallSummary.SubsystemSummaries)
            {
                SubsystemCards.Add(summary);
            }

            var incidents = await _monitoringService.GetActiveIncidentsAsync();
            TopIncidents.Clear();
            foreach (var inc in incidents)
            {
                TopIncidents.Add(inc);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NavigateToSubsystem(object? parameter)
    {
        if (parameter is SubsystemType type)
        {
            NavigateToSubsystemInternal(type);
        }
        else if (parameter is string str)
        {
            if (Enum.TryParse<SubsystemType>(str, true, out var parsedType))
            {
                NavigateToSubsystemInternal(parsedType);
            }
            else if (int.TryParse(str, out var parsedIndex))
            {
                _navigateToTabAction(parsedIndex);
            }
        }
        else if (parameter is int index)
        {
            _navigateToTabAction(index);
        }
    }

    public void NavigateToSubsystem(SubsystemType type)
    {
        NavigateToSubsystemInternal(type);
    }

    private void NavigateToSubsystemInternal(SubsystemType type)
    {
        int targetIndex = type switch
        {
            SubsystemType.LanTel => 1,
            SubsystemType.PublicAddress => 2,
            SubsystemType.Cctv => 3,
            SubsystemType.AccessControl => 4,
            SubsystemType.Bms => 5,
            SubsystemType.FireAlarm => 6,
            _ => 0
        };
        _navigateToTabAction(targetIndex);
    }

    [RelayCommand]
    public async Task AcknowledgeIncidentAsync(SubsystemIncident? incident)
    {
        if (incident == null) return;
        await _monitoringService.AcknowledgeIncidentAsync(incident.Id, "Trực ban Vận hành");

        ActionLogs.Insert(0, new ActionLogItem
        {
            Registry = ActionLogs.Count + 1,
            OperatorName = "Trực Ban IOC",
            Role = "Trực Ban Vận Hành",
            Category = "Xử Lý Sự Cố",
            SubsystemName = incident.Subsystem.ToString(),
            Description = $"Xác nhận ACK sự cố [{incident.DeviceName}]: {incident.Title}",
            Timestamp = DateTime.Now.ToString("HH:mm:ss")
        });

        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task ResolveIncidentAsync(SubsystemIncident? incident)
    {
        if (incident == null) return;
        await _monitoringService.ResolveIncidentAsync(incident.Id);

        ActionLogs.Insert(0, new ActionLogItem
        {
            Registry = ActionLogs.Count + 1,
            OperatorName = "Trực Ban IOC",
            Role = "Kỹ Sư Trực Ban",
            Category = "Đóng Sự Cố",
            SubsystemName = incident.Subsystem.ToString(),
            Description = $"Đóng và khôi phục sự cố [{incident.DeviceName}]: {incident.Title}",
            Timestamp = DateTime.Now.ToString("HH:mm:ss")
        });

        await LoadDataAsync();
    }
}
