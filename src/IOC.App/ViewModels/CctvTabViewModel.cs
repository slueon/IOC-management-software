using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.Cctv;

namespace IOC.App.ViewModels;

public class FloorPlanOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public partial class CctvTabViewModel : SubsystemTabViewModelBase<CctvDevice>
{
    public override SubsystemType Subsystem => SubsystemType.Cctv;

    [ObservableProperty]
    private CctvDevice? _selectedCamera;

    [ObservableProperty]
    private FloorPlanOption? _selectedFloorPlan;

    [ObservableProperty]
    private string _ptzMessage = "Hệ thống sẵn sàng điều khiển";

    [ObservableProperty]
    private string _snapshotMessage = "Chưa có ảnh chụp mới";

    [ObservableProperty]
    private string _liveTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public ObservableCollection<FloorPlanOption> FloorPlans { get; } =
    [
        new() { Id = "ALL", Name = "Toàn Bộ Mặt Bằng Tòa Nhà (All Floors)" },
        new() { Id = "FLOOR_1", Name = "Tầng 1 - Sảnh Chính & Cổng Ra Vào" },
        new() { Id = "FLOOR_B1", Name = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật" },
        new() { Id = "FLOOR_2", Name = "Tầng 2 & 5 - Khối Văn Phòng" },
        new() { Id = "FLOOR_OUTDOOR", Name = "Khuôn Viên Ngoài Trời & Cổng Bảo Vệ" }
    ];

    public ObservableCollection<CctvDevice> FilteredCameras { get; } = [];

    public CctvTabViewModel(ISubsystemHealthMonitoringService monitoringService)
        : base(monitoringService)
    {
        Title = "Camera An Ninh (CCTV)";
        Subtitle = "Giám sát sống/chết camera, Video Loss, điều khiển PTZ và xem trực tiếp theo mặt bằng";
        IconKind = "Cctv";
        SelectedFloorPlan = FloorPlans[0];
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();

        if (SelectedCamera == null)
        {
            SelectedCamera = Devices.FirstOrDefault(d => d.DeviceType == CctvDeviceType.IpCamera);
        }

        ApplyFloorPlanFilter();
        UpdateStats();
    }

    partial void OnSelectedFloorPlanChanged(FloorPlanOption? value)
    {
        ApplyFloorPlanFilter();
    }

    protected override void OnSelectedDeviceChangedInternal(CctvDevice? value)
    {
        base.OnSelectedDeviceChangedInternal(value);
        if (value != null && value.DeviceType == CctvDeviceType.IpCamera)
        {
            SelectedCamera = value;
            PtzMessage = $"Đang chọn {value.Name} ({value.IpAddress})";
        }
        UpdateStats();
    }

    private void ApplyFloorPlanFilter()
    {
        FilteredCameras.Clear();
        var cameras = Devices.Where(d => d.DeviceType == CctvDeviceType.IpCamera);
        if (SelectedFloorPlan != null && SelectedFloorPlan.Id != "ALL")
        {
            cameras = cameras.Where(c => c.FloorPlanId == SelectedFloorPlan.Id);
        }

        foreach (var cam in cameras)
        {
            FilteredCameras.Add(cam);
        }

        if (SelectedCamera != null && !FilteredCameras.Contains(SelectedCamera) && FilteredCameras.Any())
        {
            SelectedCamera = FilteredCameras.First();
        }
    }

    private void UpdateStats()
    {
        LiveTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        OnPropertyChanged(nameof(NvrDevice));
        OnPropertyChanged(nameof(TotalCamerasCount));
        OnPropertyChanged(nameof(OnlineCamerasCount));
        OnPropertyChanged(nameof(VideoLossCount));
        OnPropertyChanged(nameof(NvrStorageText));
        OnPropertyChanged(nameof(RaidStatusText));
    }

    public CctvDevice? NvrDevice => Devices.FirstOrDefault(d => d.DeviceType == CctvDeviceType.NvrRecorder);
    public int TotalCamerasCount => Devices.Count(d => d.DeviceType == CctvDeviceType.IpCamera);
    public int OnlineCamerasCount => Devices.Count(d => d.DeviceType == CctvDeviceType.IpCamera && d.Status == DeviceStatus.Online && !d.IsVideoLoss);
    public int VideoLossCount => Devices.Count(d => d.DeviceType == CctvDeviceType.IpCamera && (d.IsVideoLoss || !d.IsRtspStreamActive));
    public string NvrStorageText => NvrDevice?.NvrStorage != null
        ? $"{NvrDevice.NvrStorage.StorageUsedGb:F0} / {NvrDevice.NvrStorage.StorageTotalGb:F0} GB ({NvrDevice.NvrStorage.DiskUsagePercent:F1}%)"
        : "16,000 GB";
    public string RaidStatusText => NvrDevice?.NvrStorage != null
        ? $"{NvrDevice.NvrStorage.RaidType} - {NvrDevice.NvrStorage.RaidStatus}"
        : "RAID 5 - Healthy";

    [RelayCommand]
    public void SelectCamera(CctvDevice? camera)
    {
        if (camera == null) return;
        SelectedCamera = camera;
        SelectedDevice = camera;
        PtzMessage = $"Đã chuyển góc nhìn sang {camera.Name}";
    }

    [RelayCommand]
    public void PanTilt(string direction)
    {
        if (SelectedCamera == null) return;

        switch (direction?.ToUpperInvariant())
        {
            case "LEFT":
                SelectedCamera.PanAngle = Math.Max(-180.0, SelectedCamera.PanAngle - 10.0);
                break;
            case "RIGHT":
                SelectedCamera.PanAngle = Math.Min(180.0, SelectedCamera.PanAngle + 10.0);
                break;
            case "UP":
                SelectedCamera.TiltAngle = Math.Min(90.0, SelectedCamera.TiltAngle + 5.0);
                break;
            case "DOWN":
                SelectedCamera.TiltAngle = Math.Max(-90.0, SelectedCamera.TiltAngle - 5.0);
                break;
            case "CENTER":
                SelectedCamera.PanAngle = 0.0;
                SelectedCamera.TiltAngle = 0.0;
                break;
        }

        PtzMessage = $"PTZ {SelectedCamera.Name}: Pan {SelectedCamera.PanAngle:F0}°, Tilt {SelectedCamera.TiltAngle:F0}°";
        OnPropertyChanged(nameof(SelectedCamera));
    }

    [RelayCommand]
    public void Zoom(string direction)
    {
        if (SelectedCamera == null) return;

        if (direction?.ToUpperInvariant() == "IN")
        {
            SelectedCamera.ZoomLevel = Math.Min(32.0, Math.Round(SelectedCamera.ZoomLevel + 0.5, 1));
        }
        else
        {
            SelectedCamera.ZoomLevel = Math.Max(1.0, Math.Round(SelectedCamera.ZoomLevel - 0.5, 1));
        }

        PtzMessage = $"Zoom {SelectedCamera.Name}: {SelectedCamera.ZoomLevel:F1}x";
        OnPropertyChanged(nameof(SelectedCamera));
    }

    [RelayCommand]
    public void ToggleNightVision()
    {
        if (SelectedCamera == null) return;
        SelectedCamera.IsNightVisionActive = !SelectedCamera.IsNightVisionActive;
        PtzMessage = SelectedCamera.IsNightVisionActive
            ? $"Hồng ngoại nhìn đêm: BẬT (IR ON) - {SelectedCamera.Name}"
            : $"Hồng ngoại nhìn đêm: TẮT (Color Day Mode) - {SelectedCamera.Name}";
        OnPropertyChanged(nameof(SelectedCamera));
    }

    [RelayCommand]
    public void TakeSnapshot()
    {
        if (SelectedCamera == null) return;
        SnapshotMessage = $"Đã chụp ảnh bằng chứng: {SelectedCamera.Id} lúc {DateTime.Now:HH:mm:ss}";
        PtzMessage = $"Snapshot lưu thành công vào bộ nhớ NVR ({SelectedCamera.Resolution})";
    }

    [RelayCommand]
    public async Task RebootCameraAsync()
    {
        if (SelectedCamera == null) return;
        PtzMessage = $"Đang gửi lệnh khởi động lại camera {SelectedCamera.Name}...";
        await Task.Delay(400);
        SelectedCamera.Status = DeviceStatus.Online;
        SelectedCamera.IsVideoLoss = false;
        SelectedCamera.IsRtspStreamActive = true;
        PtzMessage = $"Camera {SelectedCamera.Name} đã khởi động lại thành công (RTSP Online)";
        OnPropertyChanged(nameof(SelectedCamera));
    }

    [RelayCommand]
    public void GotoPreset(string preset)
    {
        if (SelectedCamera == null) return;
        switch (preset)
        {
            case "P1":
                SelectedCamera.PanAngle = 0.0;
                SelectedCamera.TiltAngle = -10.0;
                SelectedCamera.ZoomLevel = 1.0;
                PtzMessage = $"Chuyển Preset 1: Cổng Chính (0°, -10°, 1.0x)";
                break;
            case "P2":
                SelectedCamera.PanAngle = 45.0;
                SelectedCamera.TiltAngle = -20.0;
                SelectedCamera.ZoomLevel = 2.0;
                PtzMessage = $"Chuyển Preset 2: Quầy Lễ Tân (45°, -20°, 2.0x)";
                break;
            case "P3":
                SelectedCamera.PanAngle = -60.0;
                SelectedCamera.TiltAngle = -15.0;
                SelectedCamera.ZoomLevel = 3.0;
                PtzMessage = $"Chuyển Preset 3: Cửa Thoát Hiểm (-60°, -15°, 3.0x)";
                break;
            case "P4":
                SelectedCamera.PanAngle = 90.0;
                SelectedCamera.TiltAngle = -25.0;
                SelectedCamera.ZoomLevel = 1.5;
                PtzMessage = $"Chuyển Preset 4: Lối Thang Máy (90°, -25°, 1.5x)";
                break;
        }
        OnPropertyChanged(nameof(SelectedCamera));
    }
}
