using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.FireAlarm;

namespace IOC.App.ViewModels;

public partial class FireAlarmTabViewModel : SubsystemTabViewModelBase<FireAlarmDevice>
{
    public override SubsystemType Subsystem => SubsystemType.FireAlarm;

    [ObservableProperty]
    private FloorPlanOption? _selectedFloorPlan;

    [ObservableProperty]
    private FireAlarmSensorPoint? _selectedSensor;

    [ObservableProperty]
    private FireLoopInfo? _selectedLoop;

    [ObservableProperty]
    private string _controlLogMessage = "Chế độ Read-Only: Tủ báo cháy Hochiki FireNET đang truyền dữ liệu thời gian thực theo chuẩn TCVN";

    public ObservableCollection<FloorPlanOption> FloorPlans { get; } =
    [
        new() { Id = "ALL", Name = "Toàn Bộ Mặt Bằng Tòa Nhà (All Loops)" },
        new() { Id = "FLOOR_1", Name = "Tầng 1 - Sảnh Chính & Khu Kỹ Thuật" },
        new() { Id = "FLOOR_B1", Name = "Tầng Hầm B1 - Trạm Biến Áp & Bãi Xe" },
        new() { Id = "FLOOR_2", Name = "Tầng 2 & 4 - Khối Văn Phòng & Căn Tin" }
    ];

    public ObservableCollection<FireAlarmSensorPoint> AllSensors { get; } = [];
    public ObservableCollection<FireAlarmSensorPoint> FilteredSensors { get; } = [];
    public ObservableCollection<FireLoopInfo> Loops { get; } = [];

    public FireAlarmTabViewModel(ISubsystemHealthMonitoringService monitoringService)
        : base(monitoringService)
    {
        Title = "Báo Cháy PCCC (Read-Only)";
        Subtitle = "Cảnh báo khói/nhiệt khẩn cấp, đứt mạch vòng loop & mất điện lưới tủ báo cháy";
        IconKind = "FireAlert";
        SelectedFloorPlan = FloorPlans[0];
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        LoadFireEquipment();
        UpdateStats();
    }

    partial void OnSelectedFloorPlanChanged(FloorPlanOption? value)
    {
        ApplyFloorFilter();
    }

    protected override void OnSelectedDeviceChangedInternal(FireAlarmDevice? value)
    {
        base.OnSelectedDeviceChangedInternal(value);
        LoadFireEquipment();
        UpdateStats();
    }

    private void LoadFireEquipment()
    {
        Loops.Clear();
        AllSensors.Clear();

        var panel = MainPanelDevice;
        if (panel != null)
        {
            foreach (var loop in panel.Loops)
            {
                Loops.Add(loop);
                foreach (var sensor in loop.Sensors)
                {
                    AllSensors.Add(sensor);
                }
            }
        }

        if (SelectedLoop == null || !Loops.Contains(SelectedLoop))
        {
            SelectedLoop = Loops.FirstOrDefault();
        }

        ApplyFloorFilter();
    }

    private void ApplyFloorFilter()
    {
        FilteredSensors.Clear();
        var sensors = AllSensors.AsEnumerable();
        if (SelectedFloorPlan != null && SelectedFloorPlan.Id != "ALL")
        {
            sensors = sensors.Where(s => s.FloorPlanId == SelectedFloorPlan.Id);
        }

        foreach (var s in sensors)
        {
            FilteredSensors.Add(s);
        }

        if (SelectedSensor == null || !FilteredSensors.Contains(SelectedSensor))
        {
            SelectedSensor = FilteredSensors.FirstOrDefault();
        }
    }

    public void UpdateStats()
    {
        OnPropertyChanged(nameof(MainPanelDevice));
        OnPropertyChanged(nameof(ActiveFiresCount));
        OnPropertyChanged(nameof(IsFireAlarmActive));
        OnPropertyChanged(nameof(FireAlarmStatusSummary));
        OnPropertyChanged(nameof(LoopFaultCount));
        OnPropertyChanged(nameof(IsLoopFaultActive));
        OnPropertyChanged(nameof(LoopStatusSummary));
        OnPropertyChanged(nameof(PowerSourceText));
        OnPropertyChanged(nameof(IsPowerAlert));
        OnPropertyChanged(nameof(BatteryLevelText));
        OnPropertyChanged(nameof(TotalSensorsCount));
        OnPropertyChanged(nameof(TotalLoopsCount));
    }

    public FireAlarmDevice? MainPanelDevice => Devices.FirstOrDefault(d => d.Loops.Any()) ?? Devices.FirstOrDefault();

    public int ActiveFiresCount => AllSensors.Count(s => s.IsAlarmTriggered);
    public bool IsFireAlarmActive => ActiveFiresCount > 0;
    public string FireAlarmStatusSummary => IsFireAlarmActive ? $"🚨 BÁO ĐỘNG ({ActiveFiresCount} ĐIỂM)" : "AN TOÀN (0 CHÁY)";

    public int LoopFaultCount => Loops.Count(l => l.IsLoopOpenFault);
    public bool IsLoopFaultActive => LoopFaultCount > 0;
    public string LoopStatusSummary => IsLoopFaultActive ? $"⚠️ ĐỨT CÁP ({LoopFaultCount} VÒNG)" : "KHÉP KÍN (CLASS A)";

    public string PowerSourceText => MainPanelDevice != null && MainPanelDevice.IsAcPowerOk && !MainPanelDevice.IsBatteryBackupActive
        ? "AC 220V (LƯỚI ĐIỆN CHÍNH)"
        : "ẮC QUY DỰ PHÒNG (DC 24V)";

    public bool IsPowerAlert => MainPanelDevice != null && (!MainPanelDevice.IsAcPowerOk || MainPanelDevice.IsBatteryBackupActive);

    public string BatteryLevelText => MainPanelDevice != null
        ? $"{MainPanelDevice.BatteryLevelPercent:F0}% ({MainPanelDevice.BatteryVoltageVolt:F1}V)"
        : "96% (26.8V)";

    public int TotalSensorsCount => AllSensors.Count;
    public int TotalLoopsCount => Loops.Count;

    [RelayCommand]
    public void SelectSensor(FireAlarmSensorPoint? sensor)
    {
        if (sensor == null) return;
        SelectedSensor = sensor;
        ControlLogMessage = $"Đang chọn: {sensor.SensorTypeDisplay} - {sensor.PointAddress} ({sensor.Location})";
    }

    [RelayCommand]
    public void SelectLoop(FireLoopInfo? loop)
    {
        if (loop == null) return;
        SelectedLoop = loop;
        ControlLogMessage = $"Đang chọn: {loop.LoopName} - {loop.Sensors.Count} cảm biến";
    }

    [RelayCommand]
    public void AcknowledgeFireAlarm(FireAlarmSensorPoint? sensor)
    {
        if (sensor == null) return;
        ControlLogMessage = $"🔔 ĐÃ GHI NHẬN TÍN HIỆU: {sensor.DeviceName} tại {sensor.Location}. " +
                            "Lưu ý: Tuân thủ quy định PCCC, thao tác Tắt Chuông / Reset bắt buộc thực hiện tại Tủ Báo Cháy Vật Lý!";
        UpdateStats();
    }
}
