using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;

namespace IOC.App.ViewModels;

public class SimulationLogItem
{
    public string Time { get; set; } = string.Empty;
    public string SubsystemName { get; set; } = string.Empty;
    public string ScenarioCode { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SeverityColor { get; set; } = "#EF4444";
    public string Status { get; set; } = "Đang kích hoạt";
}

public partial class SimulationTabViewModel : ViewModelBase
{
    private readonly ISubsystemHealthMonitoringService _monitoringService;
    private readonly Action<int>? _navigateToTab;

    [ObservableProperty]
    private int _activeIncidentsCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _majorCount;

    [ObservableProperty]
    private int _minorCount;

    [ObservableProperty]
    private string _lastTriggeredAction = "Hệ thống hoạt động bình thường";

    [ObservableProperty]
    private string _lastTriggeredTime = string.Empty;

    public ObservableCollection<SimulationLogItem> SimulationLogs { get; } = new();

    public SimulationTabViewModel(ISubsystemHealthMonitoringService monitoringService, Action<int>? navigateToTab = null)
    {
        _monitoringService = monitoringService;
        _navigateToTab = navigateToTab;
        LastTriggeredTime = DateTime.Now.ToString("HH:mm:ss");
    }

    [RelayCommand]
    public async Task TriggerScenarioAsync(string scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario)) return;

        var (subsystem, title, sevText, sevColor) = GetScenarioMetadata(scenario);

        await _monitoringService.TriggerDiagnosticFaultAsync("SIM", scenario);
        await RefreshSimulationStatusAsync();

        LastTriggeredAction = $"[{scenario}] {title}";
        LastTriggeredTime = DateTime.Now.ToString("HH:mm:ss");

        var log = new SimulationLogItem
        {
            Time = LastTriggeredTime,
            SubsystemName = subsystem,
            ScenarioCode = scenario,
            ScenarioName = title,
            Severity = sevText,
            SeverityColor = sevColor,
            Status = "Kích hoạt mô phỏng"
        };

        SimulationLogs.Insert(0, log);
        while (SimulationLogs.Count > 50)
        {
            SimulationLogs.RemoveAt(SimulationLogs.Count - 1);
        }
    }

    [RelayCommand]
    public async Task ResetAllAsync()
    {
        await _monitoringService.TriggerDiagnosticFaultAsync("ALL", "CLEAR_ALL");
        await RefreshSimulationStatusAsync();

        LastTriggeredAction = "Đã khôi phục toàn bộ hệ thống về bình thường";
        LastTriggeredTime = DateTime.Now.ToString("HH:mm:ss");

        var log = new SimulationLogItem
        {
            Time = LastTriggeredTime,
            SubsystemName = "Toàn Tòa Nhà",
            ScenarioCode = "CLEAR_ALL",
            ScenarioName = "Khôi phục toàn bộ phân hệ về bình thường",
            Severity = "Normal",
            SeverityColor = "#10B981",
            Status = "Đã khôi phục"
        };

        SimulationLogs.Insert(0, log);
    }

    [RelayCommand]
    public void NavigateToSubsystem(object? parameter)
    {
        if (parameter is int idx)
        {
            _navigateToTab?.Invoke(idx);
        }
        else if (parameter is string str && int.TryParse(str, out var parsed))
        {
            _navigateToTab?.Invoke(parsed);
        }
    }

    public void NavigateToSubsystem(int tabIndex)
    {
        _navigateToTab?.Invoke(tabIndex);
    }

    [RelayCommand]
    public async Task RefreshSimulationStatusAsync()
    {
        var incidents = await _monitoringService.GetActiveIncidentsAsync();
        ActiveIncidentsCount = incidents.Count;
        CriticalCount = incidents.Count(i => i.Severity == AlarmSeverity.Critical);
        MajorCount = incidents.Count(i => i.Severity == AlarmSeverity.Major);
        MinorCount = incidents.Count(i => i.Severity == AlarmSeverity.Minor);
    }

    private static (string Subsystem, string Title, string Severity, string Color) GetScenarioMetadata(string scenario)
    {
        return scenario.ToUpperInvariant() switch
        {
            // Full building drills
            "FIRE_ALERT" => ("Toàn Tòa Nhà", "Diễn tập báo cháy khẩn cấp toàn tòa nhà", "Critical", "#DC2626"),
            "SWITCH_DOWN" => ("Mạng LAN", "Mất kết nối toàn bộ Switch Core", "Major", "#EA580C"),
            "CHILLER_FAULT" => ("Cơ Điện BMS", "Sự cố quá nhiệt hệ thống Chiller", "Critical", "#D97706"),
            "DOOR_FORCED" => ("Access Control", "Báo động đột nhập / cạy phá cửa an ninh", "Major", "#059669"),

            // LAN / TEL
            "CPU_OVERLOAD" => ("Mạng LAN", "Quá tải CPU Switch Core (>90%)", "Major", "#DC2626"),
            "RAM_OVERFLOW" => ("Mạng LAN", "Tràn bộ nhớ RAM Switch Core (>90%)", "Major", "#D97706"),
            "OVERHEAT" => ("Mạng LAN", "Nhiệt độ bo mạch vượt ngưỡng (78°C)", "Critical", "#DC2626"),
            "PORT_DOWN" => ("Mạng LAN", "Đứt cáp kết nối cổng Switch vật lý", "Major", "#EA580C"),
            "CONGESTION" => ("Mạng LAN", "Bơm nghẽn lưu lượng băng thông cổng mạng", "Minor", "#D97706"),
            "PBX_DOWN" => ("Tổng Đài PBX", "Mất trung kế SIP Trunk tổng đài", "Critical", "#DC2626"),
            "NORMAL_LAN" => ("Mạng LAN", "Khôi phục trạng thái mạng LAN bình thường", "Normal", "#10B981"),

            // Public Address (PA)
            "PA_AC_LOST" => ("Âm Thanh PA", "Mất nguồn điện lưới AC 220V", "Major", "#7C3AED"),
            "PA_BATTERY_LOW" => ("Âm Thanh PA", "Ắc quy dự phòng DC 24V yếu (<20%)", "Minor", "#D97706"),
            "AMPLIFIER_OVERHEAT" => ("Âm Thanh PA", "Quá nhiệt bộ tăng âm công suất (88°C)", "Critical", "#DC2626"),
            "AMPLIFIER_OVERLOAD" => ("Âm Thanh PA", "Quá tải công suất tăng âm (>95%)", "Major", "#EA580C"),
            "IMPEDANCE_OPEN" => ("Âm Thanh PA", "Đứt cáp đường dây loa hầm B1 (Open >500Ω)", "Major", "#D97706"),
            "IMPEDANCE_SHORT" => ("Âm Thanh PA", "Chập tải đường dây loa văn phòng (Short <10Ω)", "Critical", "#DC2626"),
            "NORMAL_PA" => ("Âm Thanh PA", "Khôi phục hệ thống âm thanh PA bình thường", "Normal", "#10B981"),

            // CCTV
            "VIDEO_LOSS" => ("Camera CCTV", "Mất tín hiệu hình ảnh camera (Video Loss)", "Critical", "#DC2626"),
            "HIGH_LATENCY" => ("Camera CCTV", "Ping camera trễ cao & mất gói dữ liệu", "Minor", "#D97706"),
            "NVR_STORAGE_FULL" => ("Đầu Ghi NVR", "Dung lượng đĩa cứng đầu ghi đầy (98%)", "Major", "#EA580C"),
            "RAID_DEGRADED" => ("Đầu Ghi NVR", "Mảng RAID 5 hỏng ổ đĩa cứng", "Critical", "#DC2626"),
            "NORMAL_CCTV" => ("Camera CCTV", "Khôi phục hệ thống Camera CCTV bình thường", "Normal", "#10B981"),

            // Access Control & Elevator
            "DOOR_HELD" => ("Access Control", "Chèn giữ cửa mở quá thời gian (>30s)", "Minor", "#D97706"),
            "DOOR_OFFLINE" => ("Access Control", "Mất kết nối đầu đọc thẻ cửa kiểm soát", "Minor", "#64748B"),
            "CONTROLLER_OFFLINE" => ("Access Control", "Bộ điều khiển cửa mất kết nối mạng", "Critical", "#DC2626"),
            "AC_POWER_LOST" => ("Access Control", "Bộ điều khiển mất nguồn điện lưới AC 220V", "Major", "#7C3AED"),
            "AC_BATTERY_LOW" => ("Access Control", "Pin dự phòng bộ điều khiển yếu (<20%)", "Minor", "#D97706"),
            "ELEVATOR_EMERGENCY" => ("Thang Máy", "Thang máy kích hoạt dừng khẩn cấp (E-Stop)", "Critical", "#DC2626"),
            "ELEVATOR_OVERLOAD" => ("Thang Máy", "Cabin thang máy vượt tải trọng định mức", "Minor", "#D97706"),
            "NORMAL_ACCESS" => ("Access Control", "Khôi phục hệ thống kiểm soát cửa bình thường", "Normal", "#10B981"),

            // BMS
            "CHILLER_OVERHEAT" => ("Cơ Điện BMS", "Nhiệt độ nước lạnh Chiller vượt ngưỡng (15.5°C)", "Critical", "#DC2626"),
            "PRESSURE_FAULT" => ("Cơ Điện BMS", "Áp suất máy nén Chiller cao bất thường", "Major", "#EA580C"),
            "PUMP_TRIP" => ("Cơ Điện BMS", "Máy bơm sinh hoạt/PCCC nhảy rơ-le nhiệt", "Major", "#DC2626"),
            "PHASE_LOSS" => ("Cơ Điện BMS", "Mất pha trạm biến áp điện 3 pha", "Critical", "#DC2626"),
            "CURRENT_OVERLOAD" => ("Cơ Điện BMS", "Quá tải dòng điện tủ phân phối tổng", "Major", "#EA580C"),
            "VOLTAGE_SAG" => ("Cơ Điện BMS", "Sụt áp lưới điện tòa nhà (<200V)", "Minor", "#D97706"),
            "GEN_LOW_FUEL" => ("Cơ Điện BMS", "Máy phát Cummins mức dầu Diesel thấp (<15%)", "Major", "#D97706"),
            "SUMP_EMPTY" => ("Cơ Điện BMS", "Mực nước bể ngầm hầm B2 cạn (<12%)", "Major", "#0284C7"),
            "GAS_LEAK" => ("Cơ Điện BMS", "Nồng độ khí CO/Gas hầm xe vượt ngưỡng (65 ppm)", "Critical", "#DC2626"),
            "NORMAL_BMS" => ("Cơ Điện BMS", "Khôi phục hệ thống Cơ điện BMS bình thường", "Normal", "#10B981"),

            // Fire Alarm PCCC
            "FIRE_SMOKE" => ("Báo Cháy PCCC", "Báo động cháy đầu khói phòng Server T1", "Critical", "#DC2626"),
            "FIRE_HEAT" => ("Báo Cháy PCCC", "Báo động cháy đầu nhiệt trạm biến áp B1", "Critical", "#DC2626"),
            "FIRE_MCP" => ("Báo Cháy PCCC", "Kích hoạt nút ấn khẩn cấp MCP sảnh đón", "Critical", "#DC2626"),
            "LOOP_FAULT" => ("Báo Cháy PCCC", "Đứt mạch cáp vòng Loop Class A Style 7", "Major", "#EA580C"),
            "FA_POWER_LOSS" => ("Báo Cháy PCCC", "Tủ trung tâm mất điện lưới chuyển ắc quy", "Major", "#7C3AED"),
            "FA_BATTERY_LOW" => ("Báo Cháy PCCC", "Ắc quy tủ báo cháy DC 24V yếu (<20%)", "Minor", "#D97706"),
            "NORMAL_FIRE" => ("Báo Cháy PCCC", "Khôi phục hệ thống Báo cháy PCCC bình thường", "Normal", "#10B981"),

            _ => ("Hệ Thống", $"Mô phỏng tình huống {scenario}", "Info", "#0284C7")
        };
    }
}

