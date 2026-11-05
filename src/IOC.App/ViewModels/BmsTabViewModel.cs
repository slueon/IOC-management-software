using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.Bms;

namespace IOC.App.ViewModels;

public partial class BmsTabViewModel : SubsystemTabViewModelBase<BmsDevice>
{
    public override SubsystemType Subsystem => SubsystemType.Bms;

    [ObservableProperty]
    private string _controlLogMessage = "Hệ thống BMS sẵn sàng tiếp nhận lệnh điều khiển & giám sát tự động";

    [ObservableProperty]
    private PumpMetrics? _selectedPump;

    [ObservableProperty]
    private AhuFcuUnitInfo? _selectedHvacUnit;

    public ObservableCollection<PumpMetrics> AllPumps { get; } = [];
    public ObservableCollection<AhuFcuUnitInfo> AllHvacUnits { get; } = [];
    private readonly ConfirmationRequester? _confirmationRequester;

    public BmsTabViewModel(ISubsystemHealthMonitoringService monitoringService, ConfirmationRequester? confirmationRequester = null)
        : base(monitoringService)
    {
        _confirmationRequester = confirmationRequester;
        Title = "Cơ Điện & BMS Tòa Nhà";
        Subtitle = "Giám sát Chiller/HVAC, trạm bơm nước, điện 3 pha/máy phát & cảm biến bể ngầm/khí gas";
        IconKind = "TuneVertical";
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        LoadEquipment();
        UpdateStats();
    }

    protected override void OnSelectedDeviceChangedInternal(BmsDevice? value)
    {
        base.OnSelectedDeviceChangedInternal(value);
        LoadEquipment();
        UpdateStats();
    }

    private void LoadEquipment()
    {
        AllPumps.Clear();
        foreach (var pump in Devices.SelectMany(d => d.Pumps))
        {
            AllPumps.Add(pump);
        }
        if (SelectedPump == null || !AllPumps.Contains(SelectedPump))
        {
            SelectedPump = AllPumps.FirstOrDefault();
        }

        AllHvacUnits.Clear();
        var chillerDev = ChillerDevice;
        if (chillerDev?.Chiller != null)
        {
            foreach (var unit in chillerDev.Chiller.HvacUnits)
            {
                AllHvacUnits.Add(unit);
            }
        }
        if (SelectedHvacUnit == null || !AllHvacUnits.Contains(SelectedHvacUnit))
        {
            SelectedHvacUnit = AllHvacUnits.FirstOrDefault();
        }
    }

    public void UpdateStats()
    {
        OnPropertyChanged(nameof(ChillerDevice));
        OnPropertyChanged(nameof(PumpStationDevice));
        OnPropertyChanged(nameof(ElecGridDevice));
        OnPropertyChanged(nameof(HazardDevice));

        OnPropertyChanged(nameof(ChillerTempsText));
        OnPropertyChanged(nameof(ChillerPressureText));
        OnPropertyChanged(nameof(IsChillerAlert));

        OnPropertyChanged(nameof(ActivePumpsCount));
        OnPropertyChanged(nameof(TrippedPumpsCount));
        OnPropertyChanged(nameof(IsPumpsAlert));

        OnPropertyChanged(nameof(GridVoltageText));
        OnPropertyChanged(nameof(GridPowerText));
        OnPropertyChanged(nameof(IsGridAlert));
        OnPropertyChanged(nameof(GeneratorStatusText));

        OnPropertyChanged(nameof(SumpWaterLevelText));
        OnPropertyChanged(nameof(GasCoText));
        OnPropertyChanged(nameof(IsHazardAlert));
    }

    public BmsDevice? ChillerDevice => Devices.FirstOrDefault(d => d.Category == BmsCategory.ChillerHvac && d.Chiller != null);
    public BmsDevice? PumpStationDevice => Devices.FirstOrDefault(d => d.Category == BmsCategory.PumpSystem);
    public BmsDevice? ElecGridDevice => Devices.FirstOrDefault(d => d.Category == BmsCategory.ElectricalGrid);
    public BmsDevice? HazardDevice => Devices.FirstOrDefault(d => d.Category == BmsCategory.EnvironmentalHazard);

    public string ChillerTempsText => ChillerDevice?.Chiller != null
        ? $"CẤP: {ChillerDevice.Chiller.SupplyWaterTempCelsius:F1}°C / HỒI: {ChillerDevice.Chiller.ReturnWaterTempCelsius:F1}°C"
        : "CẤP: 7.1°C / HỒI: 12.3°C";

    public string ChillerPressureText => ChillerDevice?.Chiller != null
        ? $"Áp suất: {ChillerDevice.Chiller.SystemPressureBar:F1} bar | ΔT: {ChillerDevice.Chiller.DeltaTempCelsius:F1}°C"
        : "Áp suất: 3.6 bar";

    public bool IsChillerAlert => ChillerDevice?.Chiller?.IsTempExceeded == true || ChillerDevice?.Chiller?.IsPressureExceeded == true;

    public int ActivePumpsCount => AllPumps.Count(p => p.IsRunning);
    public int TrippedPumpsCount => AllPumps.Count(p => p.IsThermalOverloadTripped);
    public bool IsPumpsAlert => TrippedPumpsCount > 0;

    public string GridVoltageText => ElecGridDevice?.PowerGrid != null
        ? $"{ElecGridDevice.PowerGrid.VoltagePhaseA:F0}V / {ElecGridDevice.PowerGrid.VoltagePhaseB:F0}V / {ElecGridDevice.PowerGrid.VoltagePhaseC:F0}V"
        : "220V / 220V / 220V";

    public string GridPowerText => ElecGridDevice?.PowerGrid != null
        ? $"Tải: {ElecGridDevice.PowerGrid.ActivePowerKw:F1} kW | PF: {ElecGridDevice.PowerGrid.PowerFactor:F2}"
        : "Tải: 98.4 kW";

    public string GeneratorStatusText => ElecGridDevice?.Generator != null
        ? $"Máy phát: {ElecGridDevice.Generator.GeneratorStatus} | Dầu: {ElecGridDevice.Generator.FuelLevelPercent:F0}%"
        : "Máy phát: STANDBY";

    public bool IsGridAlert => ElecGridDevice?.PowerGrid?.IsPhaseLossDetected == true ||
                               ElecGridDevice?.PowerGrid?.IsCurrentOverload == true ||
                               ElecGridDevice?.Generator?.IsFuelLow == true;

    public string SumpWaterLevelText => HazardDevice?.Hazard != null
        ? $"{HazardDevice.Hazard.SumpPitWaterLevelPercent:F0}% ({HazardDevice.Hazard.WaterLevelMeters:F2}m / {HazardDevice.Hazard.SumpPitCapacityM3:F0}m³)"
        : "82% (2.87m)";

    public string GasCoText => HazardDevice?.Hazard != null
        ? $"{HazardDevice.Hazard.GasCoConcentrationPpm:F1} PPM"
        : "11.5 PPM";

    public bool IsHazardAlert => HazardDevice?.Hazard?.IsSumpPitEmptyAlarm == true ||
                                HazardDevice?.Hazard?.IsGasLeakDetected == true;

    [RelayCommand]
    public void SelectPump(PumpMetrics? pump)
    {
        if (pump == null) return;
        SelectedPump = pump;
        ControlLogMessage = $"Đang chọn: {pump.PumpName} ({pump.PumpType})";
    }

    [RelayCommand]
    public void SelectHvacUnit(AhuFcuUnitInfo? unit)
    {
        if (unit == null) return;
        SelectedHvacUnit = unit;
        ControlLogMessage = $"Đang chọn dàn lạnh: {unit.UnitName} ({unit.Location})";
    }

    [RelayCommand]
    public void ResetThermalOverload(PumpMetrics? pump)
    {
        if (pump == null) return;
        pump.IsThermalOverloadTripped = false;
        pump.IsRunning = true;
        pump.FlowRateM3h = pump.PumpType.Contains("PCCC", StringComparison.OrdinalIgnoreCase) ? 0.0 : 45.0;
        ControlLogMessage = $"✅ Đã reset rơ-le nhiệt thành công cho {pump.PumpName}. Máy bơm đã sẵn sàng vận hành!";
        UpdateStats();
    }

    [RelayCommand]
    public void StartGeneratorTest()
    {
        if (ElecGridDevice?.Generator == null) return;
        if (_confirmationRequester != null)
        {
            _confirmationRequester(
                "ĐỀ NỔ MÁY PHÁT ĐIỆN DỰ PHÒNG CUMMINS 1000kVA",
                "Khởi động chu trình đề nổ chạy thử không tải máy phát điện dự phòng tại phòng máy hầm B2?",
                "CẢNH BÁO VẬN HÀNH: Động cơ Diesel công suất lớn sẽ nổ máy thực tế, phát sinh tiếng ồn, khí thải và tiêu hao dầu nhiên liệu.",
                "Cơ Điện BMS",
                () => { ExecuteStartGeneratorTestInternal(); return Task.CompletedTask; },
                "XÁC NHẬN ĐỀ NỔ MÁY PHÁT"
            );
        }
        else
        {
            ExecuteStartGeneratorTestInternal();
        }
    }

    private void ExecuteStartGeneratorTestInternal()
    {
        if (ElecGridDevice?.Generator == null) return;
        ElecGridDevice.Generator.IsRunning = true;
        ElecGridDevice.Generator.GeneratorStatus = "ĐANG CHẠY THỬ (RUNNING)";
        ControlLogMessage = "⚡ Đã kích hoạt lệnh đề nổ chạy thử máy phát điện Cummins 1000kVA";
        UpdateStats();
    }

    [RelayCommand]
    public void StopGenerator()
    {
        if (ElecGridDevice?.Generator == null) return;
        ElecGridDevice.Generator.IsRunning = false;
        ElecGridDevice.Generator.GeneratorStatus = "SẴN SÀNG (STANDBY)";
        ControlLogMessage = "⏹️ Đã dừng máy phát điện dự phòng, trở về chế độ STANDBY";
        UpdateStats();
    }

    [RelayCommand]
    public void ToggleExhaustFan()
    {
        if (HazardDevice?.Hazard == null) return;
        if (_confirmationRequester != null)
        {
            string nextAction = HazardDevice.Hazard.IsExhaustFanRunning ? "TẮT" : "BẬT CƯỠNG BỨC";
            _confirmationRequester(
                "ĐIỀU KHIỂN HỆ THỐNG QUẠT THÔNG GIÓ HÚT KHÓI TẦNG HẦM",
                $"Bạn có chắc chắn muốn {nextAction} quạt hút khói công nghiệp hầm xe B2?",
                "LƯU Ý: Quạt hút khói phục vụ giải tỏa khí CO và thông gió khẩn cấp trong hầm ngầm.",
                "Cơ Điện BMS",
                () => { ExecuteToggleExhaustFanInternal(); return Task.CompletedTask; },
                $"XÁC NHẬN {nextAction}"
            );
        }
        else
        {
            ExecuteToggleExhaustFanInternal();
        }
    }

    private void ExecuteToggleExhaustFanInternal()
    {
        if (HazardDevice?.Hazard == null) return;
        HazardDevice.Hazard.IsExhaustFanRunning = !HazardDevice.Hazard.IsExhaustFanRunning;
        string status = HazardDevice.Hazard.IsExhaustFanRunning ? "BẬT CƯỠNG BỨC" : "TẮT";
        ControlLogMessage = $"🌀 Quạt thông gió hút khói hầm xe B2: {status}";
        UpdateStats();
    }
}
