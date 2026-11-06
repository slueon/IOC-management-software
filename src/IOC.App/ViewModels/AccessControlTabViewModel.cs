using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.App.ViewModels.Base;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.AccessControl;

namespace IOC.App.ViewModels;

public partial class AccessControlTabViewModel : SubsystemTabViewModelBase<AccessControlDevice>
{
    public override SubsystemType Subsystem => SubsystemType.AccessControl;

    [ObservableProperty]
    private FloorPlanOption? _selectedFloorPlan;

    [ObservableProperty]
    private DoorPointInfo? _selectedDoor;

    [ObservableProperty]
    private ElevatorInfo? _selectedElevator;

    [ObservableProperty]
    private string _controlLogMessage = "Hệ thống Access Control sẵn sàng tiếp nhận lệnh điều khiển";

    public ObservableCollection<FloorPlanOption> FloorPlans { get; } =
    [
        new() { Id = "ALL", Name = "Toàn Bộ Mặt Bằng Tòa Nhà (All Floors)" },
        new() { Id = "FLOOR_1", Name = "Tầng 1 - Sảnh Chính & Cổng Ra Vào" },
        new() { Id = "FLOOR_B1", Name = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật" },
        new() { Id = "FLOOR_2", Name = "Tầng 2 - Khối Văn Phòng & Ban Điều Hành" }
    ];

    public ObservableCollection<DoorPointInfo> FilteredDoors { get; } = [];
    public ObservableCollection<ElevatorInfo> Elevators { get; } = [];
    private readonly ConfirmationRequester? _confirmationRequester;

    public AccessControlTabViewModel(ISubsystemHealthMonitoringService monitoringService, ConfirmationRequester? confirmationRequester = null)
        : base(monitoringService)
    {
        _confirmationRequester = confirmationRequester;
        Title = "Kiểm Soát Vào Ra (Access Control)";
        Subtitle = "Cảnh báo cạy cửa cưỡng bức, giám sát trạng thái cửa theo mặt bằng, điều khiển thang máy & sức khỏe bộ điều khiển";
        IconKind = "DoorOpen";
        SelectedFloorPlan = FloorPlans[0];
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        ApplyDoorFilter();
        LoadElevators();
        UpdateStats();
    }

    partial void OnSelectedFloorPlanChanged(FloorPlanOption? value)
    {
        ApplyDoorFilter();
    }

    protected override void OnSelectedDeviceChangedInternal(AccessControlDevice? value)
    {
        base.OnSelectedDeviceChangedInternal(value);
        ApplyDoorFilter();
        LoadElevators();
        UpdateStats();
    }

    private void ApplyDoorFilter()
    {
        FilteredDoors.Clear();
        var allDoors = Devices.SelectMany(d => d.DoorPoints);
        if (SelectedFloorPlan != null && SelectedFloorPlan.Id != "ALL")
        {
            allDoors = allDoors.Where(dp => dp.FloorPlanId == SelectedFloorPlan.Id);
        }

        foreach (var door in allDoors)
        {
            FilteredDoors.Add(door);
        }

        if (SelectedDoor == null || !FilteredDoors.Contains(SelectedDoor))
        {
            SelectedDoor = FilteredDoors.FirstOrDefault();
        }
    }

    private void LoadElevators()
    {
        Elevators.Clear();
        foreach (var el in Devices.SelectMany(d => d.Elevators))
        {
            Elevators.Add(el);
        }

        if (SelectedElevator == null || !Elevators.Contains(SelectedElevator))
        {
            SelectedElevator = Elevators.FirstOrDefault();
        }
    }

    public void UpdateStats()
    {
        OnPropertyChanged(nameof(TotalDoorsCount));
        OnPropertyChanged(nameof(LockedDoorsCount));
        OnPropertyChanged(nameof(OpenDoorsCount));
        OnPropertyChanged(nameof(ForcedDoorsCount));
        OnPropertyChanged(nameof(HeldDoorsCount));
        OnPropertyChanged(nameof(OfflineDoorsCount));
        OnPropertyChanged(nameof(TotalElevatorsCount));
        OnPropertyChanged(nameof(ElevatorEmergencyCount));
        OnPropertyChanged(nameof(ControllersOnlineCount));
        OnPropertyChanged(nameof(MinBatteryPercent));
        OnPropertyChanged(nameof(PrimaryController));
    }

    public AccessControlDevice? PrimaryController => Devices.FirstOrDefault();
    public int TotalDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count();
    public int LockedDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count(d => d.IsLocked && !d.IsOpen && !d.IsForcedOpen);
    public int OpenDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count(d => d.IsOpen);
    public int ForcedDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count(d => d.IsForcedOpen);
    public int HeldDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count(d => d.IsHeldOpen);
    public int OfflineDoorsCount => Devices.SelectMany(d => d.DoorPoints).Count(d => !d.IsConnected);
    public int TotalElevatorsCount => Elevators.Count;
    public int ElevatorEmergencyCount => Elevators.Count(e => e.IsEmergencyStop);
    public int ControllersOnlineCount => Devices.Count(d => d.IsControllerOnline);
    public double MinBatteryPercent => Devices.Any() ? Devices.Min(d => d.BatteryLevelPercent) : 0.0;

    [RelayCommand]
    public void SelectDoor(DoorPointInfo? door)
    {
        if (door == null) return;
        SelectedDoor = door;
        ControlLogMessage = $"Đang chọn cửa: {door.DoorName} ({door.Location})";
    }

    [RelayCommand]
    public void SelectElevator(ElevatorInfo? elevator)
    {
        if (elevator == null) return;
        SelectedElevator = elevator;
        ControlLogMessage = $"Đang chọn thang máy: {elevator.ElevatorName} ({elevator.CurrentFloorDisplay})";
    }

    [RelayCommand]
    public void LockDoor(DoorPointInfo? door)
    {
        if (door == null) return;
        door.IsLocked = true;
        door.IsOpen = false;
        door.IsForcedOpen = false;
        door.IsHeldOpen = false;
        door.HeldDurationSeconds = 0;
        ControlLogMessage = $"🔒 Đã phát lệnh KHÓA CỬA AN NINH: {door.DoorName}";
        UpdateStats();
    }

    [RelayCommand]
    public void UnlockDoor(DoorPointInfo? door)
    {
        if (door == null) return;
        door.IsLocked = false;
        door.IsOpen = true;
        door.IsForcedOpen = false;
        door.LastSwipeTime = DateTime.Now.ToString("HH:mm:ss");
        door.LastUserName = "Lệnh điều khiển từ xa (IOC Operator)";
        door.LastCardNumber = "REMOTE-CMD";
        door.LastAccessGranted = true;
        ControlLogMessage = $"🔓 Đã phát lệnh MỞ KHÓA TỪ XA: {door.DoorName}";
        UpdateStats();
    }

    [RelayCommand]
    public void EmergencyReleaseAll()
    {
        if (_confirmationRequester != null)
        {
            _confirmationRequester(
                "XẢ CỬA KHẨN CẤP TOÀN TÒA NHÀ (EMERGENCY RELEASE)",
                "Bạn có chắc chắn muốn mở khóa toàn bộ cửa kiểm soát thẻ và cửa thoát hiểm tòa nhà?",
                "CẢNH BÁO NGUY HIỂM: Thao tác này mở tự do toàn bộ cửa, gửi lệnh trực tiếp xuống các bộ điều khiển chốt điện từ.",
                "Access Control",
                () => { ExecuteEmergencyReleaseAllInternal(); return Task.CompletedTask; },
                "XÁC NHẬN MỞ KHÓA TOÀN BỘ"
            );
        }
        else
        {
            ExecuteEmergencyReleaseAllInternal();
        }
    }

    private void ExecuteEmergencyReleaseAllInternal()
    {
        foreach (var d in Devices.SelectMany(dev => dev.DoorPoints))
        {
            d.IsLocked = false;
            d.IsOpen = true;
            d.IsForcedOpen = false;
        }
        ControlLogMessage = "🚨 BÁO ĐỘNG SƠ TÁN: ĐÃ MỞ KHẨN CẤP TOÀN BỘ CỬA THOÁT HIỂM TÒA NHÀ!";
        UpdateStats();
    }

    [RelayCommand]
    public void LockdownAll()
    {
        if (_confirmationRequester != null)
        {
            _confirmationRequester(
                "KHÓA AN NINH TOÀN TÒA NHÀ (LOCKDOWN)",
                "Bạn có chắc chắn muốn khóa chốt an ninh toàn bộ các cửa tòa nhà ngay lập tức?",
                "CẢNH BÁO AN NINH: Toàn bộ quẹt thẻ sẽ bị từ chối truy cập cho đến khi chế độ Lockdown được gỡ bỏ.",
                "Access Control",
                () => { ExecuteLockdownAllInternal(); return Task.CompletedTask; },
                "XÁC NHẬN KHÓA AN NINH"
            );
        }
        else
        {
            ExecuteLockdownAllInternal();
        }
    }

    private void ExecuteLockdownAllInternal()
    {
        foreach (var d in Devices.SelectMany(dev => dev.DoorPoints))
        {
            d.IsLocked = true;
            d.IsOpen = false;
            d.IsForcedOpen = false;
        }
        ControlLogMessage = "🛡️ CHẾ ĐỘ PHÒNG THỦ: ĐÃ KHÓA AN TOÀN TOÀN BỘ CÁC CỬA TÒA NHÀ!";
        UpdateStats();
    }

    [RelayCommand]
    public void CallElevator(string targetFloorStr)
    {
        if (SelectedElevator == null) return;
        if (int.TryParse(targetFloorStr, out int targetFloor))
        {
            SelectedElevator.TargetFloor = targetFloor;
            SelectedElevator.Direction = targetFloor > SelectedElevator.CurrentFloor ? "UP" : (targetFloor < SelectedElevator.CurrentFloor ? "DOWN" : "IDLE");
            SelectedElevator.CurrentFloor = targetFloor;
            SelectedElevator.CurrentFloorDisplay = targetFloor < 0 ? $"Hầm B{-targetFloor}" : (targetFloor == 1 ? "Tầng 1 (Sảnh)" : $"Tầng {targetFloor}");
            SelectedElevator.DoorState = "OPEN";
            SelectedElevator.SpeedMps = 1.75;
            SelectedElevator.OperationalStatus = $"Đã di chuyển tới {SelectedElevator.CurrentFloorDisplay}";
            ControlLogMessage = $"🛗 Lệnh gọi thang {SelectedElevator.ElevatorName} tới {SelectedElevator.CurrentFloorDisplay} thành công";
            UpdateStats();
        }
    }

    [RelayCommand]
    public void ResetElevatorEmergency(ElevatorInfo? elevator)
    {
        if (elevator == null) return;
        elevator.IsEmergencyStop = false;
        elevator.OperationalStatus = "Hoạt động bình thường";
        elevator.Direction = "IDLE";
        elevator.DoorState = "CLOSED";
        ControlLogMessage = $"✅ Đã khôi phục an toàn & reset dừng khẩn cấp cho {elevator.ElevatorName}";
        UpdateStats();
    }
}
