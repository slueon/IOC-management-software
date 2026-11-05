namespace IOC.Core.Models.FireAlarm;

public enum FireAlarmSensorType
{
    SmokeDetector,
    HeatDetector,
    ManualCallPoint
}

public class FireAlarmSensorPoint
{
    public string PointAddress { get; set; } = string.Empty;
    public FireAlarmSensorType SensorType { get; set; } = FireAlarmSensorType.SmokeDetector;
    public string DeviceName { get; set; } = "Đầu Báo Khói Địa Chỉ";
    public string Location { get; set; } = string.Empty;
    public string FloorPlanId { get; set; } = "FLOOR_1";
    public double PositionX { get; set; } = 50.0;
    public double PositionY { get; set; } = 50.0;
    public bool IsAlarmTriggered { get; set; }
    public double AnalogValue { get; set; }
    public string AnalogUnit { get; set; } = "%/ft";
    public bool IsFault { get; set; }
    public DateTime? AlarmTimestamp { get; set; }

    public string SensorTypeDisplay => SensorType switch
    {
        FireAlarmSensorType.SmokeDetector => "ĐẦU BÁO KHÓI (SMOKE)",
        FireAlarmSensorType.HeatDetector => "ĐẦU BÁO NHIỆT (HEAT)",
        FireAlarmSensorType.ManualCallPoint => "NÚT ẤN KHẨN CẤP (MCP)",
        _ => "CẢM BIẾN PCCC"
    };

    public string StatusText => IsAlarmTriggered ? "🔥 BÁO CHÁY" : (IsFault ? "⚠️ SỰ CỐ" : "AN TOÀN");
}

public class FireLoopInfo
{
    public int LoopNumber { get; set; }
    public string LoopName { get; set; } = string.Empty;
    public bool IsLoopOpenFault { get; set; }
    public bool IsGroundFault { get; set; }
    public string FaultLocationDescription { get; set; } = "Mạch vòng khép kín Class A (Bình thường)";
    public int TotalSensors { get; set; } = 30;
    public int ActiveAlarmCount => Sensors.Count(s => s.IsAlarmTriggered);
    public List<FireAlarmSensorPoint> Sensors { get; set; } = [];
}
