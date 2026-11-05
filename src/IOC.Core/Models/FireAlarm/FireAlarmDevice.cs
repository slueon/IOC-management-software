using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.FireAlarm;

public class FireAlarmDevice : SubsystemDevice
{
    public FireAlarmDevice()
    {
        Subsystem = SubsystemType.FireAlarm;
    }

    public bool IsAcPowerOk { get; set; } = true;
    public bool IsBatteryBackupActive { get; set; }
    public double BatteryVoltageVolt { get; set; } = 26.8;
    public double BatteryLevelPercent { get; set; } = 96.0;
    public bool IsBatteryLow => BatteryLevelPercent < 20.0 || BatteryVoltageVolt < 21.0;
    public bool IsReadOnlyOperatingMode { get; set; } = true;
    public string PanelModel { get; set; } = "Hochiki FireNET Plus 4-Loop Addressable FACP";
    public string FirmwareVersion { get; set; } = "v5.2.1-SEC (Modbus RTU/TCP)";
    public string LegalComplianceNotice { get; set; } = "Tuân thủ tiêu chuẩn TCVN 5738 & TCVN 3890 - Chế độ Read-Only giám sát an toàn";
    public List<FireLoopInfo> Loops { get; set; } = [];

    public int ActiveFireCount => Loops
        .SelectMany(l => l.Sensors)
        .Count(s => s.IsAlarmTriggered);

    public bool HasLoopFault => Loops.Any(l => l.IsLoopOpenFault || l.IsGroundFault);
    public int TotalSensorPointsCount => Loops.Sum(l => l.Sensors.Count);
}
