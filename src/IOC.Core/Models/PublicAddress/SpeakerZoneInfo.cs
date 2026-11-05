namespace IOC.Core.Models.PublicAddress;

public enum SpeakerZoneFaultType
{
    Normal = 0,
    OpenCircuit = 1,     // Đứt đường dây loa (Trở kháng >= 250 Ohm)
    ShortCircuit = 2,    // Chập tải đường dây (Trở kháng <= 15 Ohm)
    Degraded = 3         // Lệch tải / Hỏng loa nhánh (|Z - Z_norm| > 30%)
}

public class SpeakerZoneInfo
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int SpeakerCount { get; set; } = 12;
    public double ZonePowerWatt { get; set; } = 120.0;
    public double ImpedanceOhms { get; set; } = 100.0;
    public double NormalImpedanceOhms { get; set; } = 100.0;

    public SpeakerZoneFaultType FaultType
    {
        get
        {
            if (ImpedanceOhms >= 250.0) return SpeakerZoneFaultType.OpenCircuit;
            if (ImpedanceOhms <= 15.0) return SpeakerZoneFaultType.ShortCircuit;
            if (NormalImpedanceOhms > 0 && Math.Abs(ImpedanceOhms - NormalImpedanceOhms) / NormalImpedanceOhms > 0.30)
                return SpeakerZoneFaultType.Degraded;
            return SpeakerZoneFaultType.Normal;
        }
    }

    public bool HasImpedanceFault => FaultType != SpeakerZoneFaultType.Normal;

    public string FaultDescription => FaultType switch
    {
        SpeakerZoneFaultType.OpenCircuit => "Đứt cáp loa (Open Circuit)",
        SpeakerZoneFaultType.ShortCircuit => "Chập tải loa (Short Circuit)",
        SpeakerZoneFaultType.Degraded => "Lệch tải loa (Degraded)",
        _ => "Bình thường"
    };

    public bool IsActive { get; set; } = true;
}

public class AmplifierHealthInfo
{
    public string AmplifierId { get; set; } = string.Empty;
    public string AmplifierName { get; set; } = string.Empty;
    public string ChannelInfo { get; set; } = "Kênh Đơn (100V Line)";
    public double TemperatureCelsius { get; set; } = 48.0;
    public bool IsOverheated => TemperatureCelsius >= 75.0;
    public bool IsCriticalOverheated => TemperatureCelsius >= 85.0;
    public double OutputLoadWatt { get; set; } = 240.0;
    public double RatedPowerWatt { get; set; } = 480.0;
    public double LoadPercentage => RatedPowerWatt > 0 ? (OutputLoadWatt / RatedPowerWatt) * 100.0 : 0.0;
    public bool IsOverloaded => OutputLoadWatt > RatedPowerWatt;
    public bool IsFuseBlown { get; set; }
    public bool IsProtectionActive { get; set; }
    public string FanStatus { get; set; } = "Quạt chạy bình thường";
}
