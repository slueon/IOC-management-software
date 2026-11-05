using CommunityToolkit.Mvvm.ComponentModel;

namespace IOC.Core.Models.AccessControl;

public partial class ElevatorInfo : ObservableObject
{
    [ObservableProperty]
    private string _elevatorId = string.Empty;

    [ObservableProperty]
    private string _elevatorName = string.Empty;

    [ObservableProperty]
    private int _currentFloor = 1;

    [ObservableProperty]
    private string _currentFloorDisplay = "Tầng 1";

    [ObservableProperty]
    private int _targetFloor = 1;

    [ObservableProperty]
    private string _direction = "IDLE"; // "UP", "DOWN", "IDLE"

    [ObservableProperty]
    private string _doorState = "CLOSED"; // "OPEN", "CLOSED", "OPENING", "CLOSING"

    [ObservableProperty]
    private double _speedMps = 0.0;

    [ObservableProperty]
    private double _loadKg = 240.0;

    [ObservableProperty]
    private double _ratedLoadKg = 1000.0;

    [ObservableProperty]
    private bool _isEmergencyStop;

    [ObservableProperty]
    private bool _isFireServiceMode;

    [ObservableProperty]
    private bool _isAccessCardRequired = true;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _operationalStatus = "Hoạt động bình thường";

    public double LoadPercent => RatedLoadKg > 0 ? (LoadKg / RatedLoadKg) * 100.0 : 0.0;
    public bool IsOverloaded => LoadKg > RatedLoadKg;
}

