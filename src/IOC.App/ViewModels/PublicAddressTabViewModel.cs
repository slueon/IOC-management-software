using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.PublicAddress;

namespace IOC.App.ViewModels;

public partial class PublicAddressTabViewModel : SubsystemTabViewModelBase<PublicAddressDevice>
{
    public override SubsystemType Subsystem => SubsystemType.PublicAddress;

    [ObservableProperty]
    private AmplifierHealthInfo? _selectedAmplifier;

    [ObservableProperty]
    private SpeakerZoneInfo? _selectedZone;

    [ObservableProperty]
    private bool _isCalibrating;

    [ObservableProperty]
    private string _calibrationStatus = "Đường dây sẵn sàng";

    public PublicAddressTabViewModel(ISubsystemHealthMonitoringService monitoringService)
        : base(monitoringService)
    {
        Title = "Âm Thanh Thông Báo (PA)";
        Subtitle = "Giám sát tủ trung tâm, bảo vệ ampli, quá nhiệt và đứt dây loa theo từng Zone";
        IconKind = "Bullhorn";
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        if (SelectedDevice != null)
        {
            SelectedAmplifier = SelectedDevice.Amplifiers.FirstOrDefault();
            SelectedZone = SelectedDevice.SpeakerZones.FirstOrDefault();
        }
        UpdateStats();
    }

    protected override void OnSelectedDeviceChangedInternal(PublicAddressDevice? value)
    {
        base.OnSelectedDeviceChangedInternal(value);
        if (value != null)
        {
            SelectedAmplifier = value.Amplifiers.FirstOrDefault();
            SelectedZone = value.SpeakerZones.FirstOrDefault();
        }
        else
        {
            SelectedAmplifier = null;
            SelectedZone = null;
        }
        UpdateStats();
    }

    public void UpdateStats()
    {
        OnPropertyChanged(nameof(TotalAmplifiersCount));
        OnPropertyChanged(nameof(HealthyAmplifiersCount));
        OnPropertyChanged(nameof(TotalZonesCount));
        OnPropertyChanged(nameof(HealthyZonesCount));
        OnPropertyChanged(nameof(FaultyZonesCount));
        OnPropertyChanged(nameof(TotalOutputWatt));
        OnPropertyChanged(nameof(TotalRatedWatt));
    }

    public int TotalAmplifiersCount => SelectedDevice?.Amplifiers.Count ?? 0;
    public int HealthyAmplifiersCount => SelectedDevice?.Amplifiers.Count(a => !a.IsFuseBlown && !a.IsOverheated && !a.IsOverloaded) ?? 0;
    public int TotalZonesCount => SelectedDevice?.SpeakerZones.Count ?? 0;
    public int HealthyZonesCount => SelectedDevice?.SpeakerZones.Count(z => !z.HasImpedanceFault) ?? 0;
    public int FaultyZonesCount => SelectedDevice?.SpeakerZones.Count(z => z.HasImpedanceFault) ?? 0;
    public double TotalOutputWatt => SelectedDevice?.TotalOutputLoadWatt ?? 0.0;
    public double TotalRatedWatt => SelectedDevice?.TotalRatedPowerWatt ?? 0.0;

    [RelayCommand]
    public async Task CalibrateLineCheckAsync()
    {
        if (SelectedDevice == null) return;
        IsCalibrating = true;
        CalibrationStatus = "Đang quét tín hiệu đo trở kháng tần số 1kHz...";
        try
        {
            await Task.Delay(400); // Hiệu ứng quét thời gian thực
            await RefreshAsync();
            CalibrationStatus = "Quét trở kháng hoàn tất lúc " + DateTime.Now.ToString("HH:mm:ss");
        }
        finally
        {
            IsCalibrating = false;
        }
    }
}
