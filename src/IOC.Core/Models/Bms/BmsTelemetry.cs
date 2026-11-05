namespace IOC.Core.Models.Bms;

public class AhuFcuUnitInfo
{
    public string UnitId { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string UnitType { get; set; } = "AHU"; // "AHU", "FCU"
    public string Location { get; set; } = string.Empty;
    public double SupplyTempCelsius { get; set; } = 18.5;
    public double ReturnTempCelsius { get; set; } = 24.0;
    public double SetTempCelsius { get; set; } = 22.0;
    public double AirFlowCfm { get; set; } = 4500.0;
    public int FanSpeedPercent { get; set; } = 75;
    public bool IsFilterClogged { get; set; }
    public string Status { get; set; } = "HOẠT ĐỘNG (RUNNING)";
}

public class ChillerMetrics
{
    public double SupplyWaterTempCelsius { get; set; } = 7.2;
    public double ReturnWaterTempCelsius { get; set; } = 12.1;
    public double DeltaTempCelsius => Math.Round(Math.Abs(ReturnWaterTempCelsius - SupplyWaterTempCelsius), 1);
    public double SystemPressureBar { get; set; } = 3.6;
    public double MaxSafeTempCelsius { get; set; } = 14.0;
    public double MaxSafePressureBar { get; set; } = 5.5;
    public double CoolingCapacityRt { get; set; } = 450.0;
    public double CoolingPowerKw { get; set; } = 1580.0;
    public double WaterFlowRateM3h { get; set; } = 275.0;
    public int CompressorCount { get; set; } = 2;
    public int CompressorRunningCount { get; set; } = 2;
    public double CopEfficiency { get; set; } = 5.8;
    public bool IsTempExceeded => SupplyWaterTempCelsius > MaxSafeTempCelsius || ReturnWaterTempCelsius > 18.0;
    public bool IsPressureExceeded => SystemPressureBar > MaxSafePressureBar || SystemPressureBar < 1.2;
    public List<AhuFcuUnitInfo> HvacUnits { get; set; } = [];
}

public class PumpMetrics
{
    public string PumpId { get; set; } = string.Empty;
    public string PumpName { get; set; } = string.Empty;
    public string PumpType { get; set; } = "Bơm sinh hoạt";
    public bool IsRunning { get; set; } = true;
    public bool IsThermalOverloadTripped { get; set; }
    public double FlowRateM3h { get; set; } = 45.0;
    public double DischargePressureBar { get; set; } = 4.2;
    public double MotorCurrentAmp { get; set; } = 18.5;
    public double RatedCurrentAmp { get; set; } = 24.0;
    public double BearingTempCelsius { get; set; } = 46.5;
    public double VibrationMmS { get; set; } = 1.4;
    public string Mode { get; set; } = "AUTO";
}

public class PowerGridMetrics
{
    public string SourceType { get; set; } = "Trạm biến áp lưới điện 22kV/0.4kV";
    public double VoltagePhaseA { get; set; } = 220.5;
    public double VoltagePhaseB { get; set; } = 219.8;
    public double VoltagePhaseC { get; set; } = 221.0;
    public double CurrentPhaseA { get; set; } = 140.0;
    public double CurrentPhaseB { get; set; } = 138.0;
    public double CurrentPhaseC { get; set; } = 142.0;
    public double ActivePowerKw { get; set; } = 92.5;
    public double ReactivePowerKvar { get; set; } = 28.4;
    public double PowerFactor { get; set; } = 0.95;
    public double FrequencyHz { get; set; } = 50.0;
    public double TotalEnergyKwh { get; set; } = 158420.0;
    public bool IsPhaseLossDetected => VoltagePhaseA < 50.0 || VoltagePhaseB < 50.0 || VoltagePhaseC < 50.0;
    public bool IsCurrentOverload => CurrentPhaseA > 220.0 || CurrentPhaseB > 220.0 || CurrentPhaseC > 220.0;
    public bool IsVoltageSag => (VoltagePhaseA > 50 && VoltagePhaseA < 195.0) || (VoltagePhaseB > 50 && VoltagePhaseB < 195.0) || (VoltagePhaseC > 50 && VoltagePhaseC < 195.0);
}

public class GeneratorMetrics
{
    public string GeneratorId { get; set; } = "GEN-01";
    public string GeneratorName { get; set; } = "Máy Phát Điện Dự Phòng Cummins 1000kVA";
    public string GeneratorStatus { get; set; } = "SẴN SÀNG (STANDBY)";
    public bool IsRunning { get; set; }
    public double FuelLevelPercent { get; set; } = 85.0;
    public bool IsFuelLow => FuelLevelPercent < 25.0;
    public double BatteryVoltageVolt { get; set; } = 26.5;
    public bool IsStarterBatteryLow => BatteryVoltageVolt < 22.0;
    public double CoolantTempCelsius { get; set; } = 76.0;
    public double OutputVoltageVolt { get; set; } = 380.0;
    public double FrequencyHz { get; set; } = 50.0;
    public double TotalRunHours { get; set; } = 148.5;
    public string AtsStatus { get; set; } = "LƯỚI ĐIỆN CHÍNH (GRID ACTIVE)";
}

public class HazardSensorMetrics
{
    public string LocationName { get; set; } = "Hầm B2 - Bể Nước Ngầm & Bãi Đỗ Xe";
    public double SumpPitWaterLevelPercent { get; set; } = 78.0;
    public double WaterLevelMeters { get; set; } = 2.8;
    public double SumpPitCapacityM3 { get; set; } = 150.0;
    public bool IsSumpPitEmptyAlarm => SumpPitWaterLevelPercent < 15.0;
    public bool IsSumpPitOverflowAlarm => SumpPitWaterLevelPercent >= 95.0;
    public double GasCoConcentrationPpm { get; set; } = 12.0;
    public double GasLpgConcentrationLelPercent { get; set; } = 0.0;
    public bool IsGasLeakDetected => GasCoConcentrationPpm >= 50.0 || GasLpgConcentrationLelPercent >= 20.0;
    public bool IsExhaustFanRunning { get; set; }
}
