using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.AccessControl;

public class AccessControlDevice : SubsystemDevice
{
    public AccessControlDevice()
    {
        Subsystem = SubsystemType.AccessControl;
    }

    public bool IsControllerOnline { get; set; } = true;
    public bool IsMainPowerOk { get; set; } = true;
    public double ControllerVoltageVolt { get; set; } = 12.4;
    public double BatteryLevelPercent { get; set; } = 94.0;
    public bool IsBatteryLow { get => BatteryLevelPercent < 20.0; set { } }
    public bool IsBackupBatteryActive { get => !IsMainPowerOk; set { } }
    public List<DoorPointInfo> DoorPoints { get; set; } = [];
    public List<ElevatorInfo> Elevators { get; set; } = [];
}
