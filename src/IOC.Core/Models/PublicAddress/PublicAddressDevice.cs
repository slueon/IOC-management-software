using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.PublicAddress;

public class PublicAddressDevice : SubsystemDevice
{
    public PublicAddressDevice()
    {
        Subsystem = SubsystemType.PublicAddress;
    }

    /// <summary>
    /// Trạng thái nguồn điện chính AC 220V
    /// </summary>
    public bool IsMainPowerOk { get; set; } = true;
    public double MainPowerVoltageVolt { get; set; } = 220.0;
    public double MainPowerFrequencyHz { get; set; } = 50.0;

    /// <summary>
    /// Trạng thái nguồn ắc quy dự phòng DC 24V
    /// </summary>
    public bool IsBackupBatteryOk { get; set; } = true;
    public double BatteryVoltageVolt { get; set; } = 27.2; // 27.2V Float Charge, 24.0V Nominal, <21.6V Low
    public double BatteryChargeCurrentAmp { get; set; } = 1.2; // Dương: Đang sạc, Âm: Đang xả
    public double BatteryLevelPercent { get; set; } = 98.0;
    public bool IsBatteryFault => !IsBackupBatteryOk || BatteryVoltageVolt < 21.6 || BatteryLevelPercent < 20.0;

    public List<AmplifierHealthInfo> Amplifiers { get; set; } = [];
    public List<SpeakerZoneInfo> SpeakerZones { get; set; } = [];

    public int TotalSpeakerCount => SpeakerZones.Sum(z => z.SpeakerCount);
    public double TotalOutputLoadWatt => Amplifiers.Sum(a => a.OutputLoadWatt);
    public double TotalRatedPowerWatt => Amplifiers.Sum(a => a.RatedPowerWatt);
}
