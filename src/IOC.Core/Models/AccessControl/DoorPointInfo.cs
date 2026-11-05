using CommunityToolkit.Mvvm.ComponentModel;

namespace IOC.Core.Models.AccessControl;

public partial class DoorPointInfo : ObservableObject
{
    [ObservableProperty]
    private string _doorId = string.Empty;

    [ObservableProperty]
    private string _doorName = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _floorPlanId = "FLOOR_1";

    [ObservableProperty]
    private string _floorPlanName = "Tầng 1 - Sảnh Chính";

    [ObservableProperty]
    private double _positionX = 50.0;

    [ObservableProperty]
    private double _positionY = 50.0;

    [ObservableProperty]
    private bool _isLocked = true;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isForcedOpen;

    [ObservableProperty]
    private bool _isHeldOpen;

    [ObservableProperty]
    private int _heldDurationSeconds;

    [ObservableProperty]
    private int _heldThresholdSeconds = 120;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _readerStatus = "ONLINE";

    [ObservableProperty]
    private string _lastCardNumber = "CARD-88349";

    [ObservableProperty]
    private string _lastUserName = "Trần Tuấn Anh (IT Dept)";

    [ObservableProperty]
    private string _lastSwipeTime = "10:45:12";

    [ObservableProperty]
    private bool _lastAccessGranted = true;
}
