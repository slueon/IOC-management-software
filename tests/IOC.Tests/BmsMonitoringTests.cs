using IOC.Core.Enums;
using IOC.Core.Models.Bms;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests;

public class BmsMonitoringTests
{
    private readonly SubsystemHealthEvaluator _evaluator = new();

    [Fact]
    public void Test_Chiller_Overheat_GeneratesCriticalIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-CHILLER-01",
            Name = "Hệ Thống Chiller HVAC",
            Category = BmsCategory.ChillerHvac,
            Chiller = new ChillerMetrics
            {
                SupplyWaterTempCelsius = 19.5,
                ReturnWaterTempCelsius = 24.2,
                SystemPressureBar = 3.6,
                MaxSafeTempCelsius = 14.0
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Quá nhiệt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Chiller_AbnormalPressure_GeneratesMajorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-CHILLER-01",
            Name = "Hệ Thống Chiller HVAC",
            Category = BmsCategory.ChillerHvac,
            Chiller = new ChillerMetrics
            {
                SupplyWaterTempCelsius = 7.0,
                ReturnWaterTempCelsius = 12.0,
                SystemPressureBar = 0.8, // Pressure drop / leak
                MaxSafePressureBar = 5.5
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("áp suất", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Pump_ThermalOverloadTrip_GeneratesMajorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-PUMP-01",
            Name = "Cụm Trạm Bơm Nước Sinh Hoạt",
            Category = BmsCategory.PumpSystem,
            Pumps = new List<PumpMetrics>
            {
                new()
                {
                    PumpId = "PUMP-SH-01",
                    PumpName = "Máy Bơm Nước Sinh Hoạt 1",
                    PumpType = "Bơm sinh hoạt",
                    IsRunning = false,
                    IsThermalOverloadTripped = true,
                    FlowRateM3h = 0.0
                }
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("nhảy rơ-le nhiệt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_PowerGrid_PhaseLoss_GeneratesCriticalIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-ELEC-GRID",
            Name = "Trạm Biến Áp Lưới Điện",
            Category = BmsCategory.ElectricalGrid,
            PowerGrid = new PowerGridMetrics
            {
                VoltagePhaseA = 12.0, // Lost phase A
                VoltagePhaseB = 220.0,
                VoltagePhaseC = 221.0
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Mất pha", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_PowerGrid_CurrentOverload_GeneratesMajorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-ELEC-GRID",
            Name = "Trạm Biến Áp Lưới Điện",
            Category = BmsCategory.ElectricalGrid,
            PowerGrid = new PowerGridMetrics
            {
                VoltagePhaseA = 220.0,
                VoltagePhaseB = 220.0,
                VoltagePhaseC = 220.0,
                CurrentPhaseA = 245.0, // Overload > 220A
                CurrentPhaseB = 140.0,
                CurrentPhaseC = 142.0
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("Quá tải dòng điện", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_PowerGrid_VoltageSag_GeneratesMinorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-ELEC-GRID",
            Name = "Trạm Biến Áp Lưới Điện",
            Category = BmsCategory.ElectricalGrid,
            PowerGrid = new PowerGridMetrics
            {
                VoltagePhaseA = 182.0, // Voltage sag
                VoltagePhaseB = 184.0,
                VoltagePhaseC = 185.0
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Minor &&
            i.Title.Contains("Sụt áp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Hazard_SumpPitEmpty_GeneratesMajorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-HAZARD-B2",
            Name = "Cảm Biến Bể Ngầm & Hầm B2",
            Category = BmsCategory.EnvironmentalHazard,
            Hazard = new HazardSensorMetrics
            {
                SumpPitWaterLevelPercent = 10.0, // Water too low (< 15%)
                GasCoConcentrationPpm = 8.0
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("cạn bể nước ngầm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Hazard_GasLeak_GeneratesCriticalIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-HAZARD-B2",
            Name = "Cảm Biến Bể Ngầm & Hầm B2",
            Category = BmsCategory.EnvironmentalHazard,
            Hazard = new HazardSensorMetrics
            {
                SumpPitWaterLevelPercent = 80.0,
                GasCoConcentrationPpm = 75.0 // Dangerous gas level (>= 50 ppm)
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Critical &&
            i.Title.Contains("Rò rỉ khí Gas/CO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_Generator_LowFuel_GeneratesMajorIncident()
    {
        var device = new BmsDevice
        {
            Id = "BMS-GEN-01",
            Name = "Máy Phát Điện Dự Phòng 1000kVA",
            Category = BmsCategory.ElectricalGrid,
            Generator = new GeneratorMetrics
            {
                GeneratorName = "Máy Phát Dự Phòng Cummins 1000kVA",
                FuelLevelPercent = 18.0, // Low fuel (< 25%)
                BatteryVoltageVolt = 26.5
            }
        };

        var incidents = _evaluator.EvaluateDevice(device);

        Assert.Contains(incidents, i => i.Severity == AlarmSeverity.Major &&
            i.Title.Contains("nhiên liệu dầu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Test_Bms_DiagnosticFaultSimulation_And_NormalReset()
    {
        var service = new SubsystemHealthMonitoringService();
        await service.InitializeAsync();

        // 1. Simulate Chiller Overheat
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "CHILLER_OVERHEAT");
        var bmsDevices = await service.GetDevicesBySubsystemAsync(SubsystemType.Bms);
        var chillerDev = bmsDevices.First(d => d.Id == "BMS-CHILLER-01") as BmsDevice;
        Assert.NotNull(chillerDev?.Chiller);
        Assert.True(chillerDev.Chiller.SupplyWaterTempCelsius > 15.0);

        // 2. Simulate Pump Trip
        await service.TriggerDiagnosticFaultAsync("BMS-PUMP-STATION", "PUMP_TRIP");
        var pumpDev = bmsDevices.First(d => d.Id == "BMS-PUMP-STATION") as BmsDevice;
        Assert.NotNull(pumpDev);
        Assert.Contains(pumpDev.Pumps, p => p.IsThermalOverloadTripped);

        // 3. Simulate Phase Loss
        await service.TriggerDiagnosticFaultAsync("BMS-ELEC-GRID", "PHASE_LOSS");
        var gridDev = bmsDevices.First(d => d.Id == "BMS-ELEC-GRID") as BmsDevice;
        Assert.NotNull(gridDev?.PowerGrid);
        Assert.True(gridDev.PowerGrid.IsPhaseLossDetected);

        // 4. Simulate Gas Leak
        await service.TriggerDiagnosticFaultAsync("BMS-HAZARD-B2", "GAS_LEAK");
        var hazardDev = bmsDevices.First(d => d.Id == "BMS-HAZARD-B2") as BmsDevice;
        Assert.NotNull(hazardDev?.Hazard);
        Assert.True(hazardDev.Hazard.IsGasLeakDetected);

        // 5. Reset to NORMAL
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "NORMAL");
        await service.TriggerDiagnosticFaultAsync("BMS-PUMP-STATION", "NORMAL");
        await service.TriggerDiagnosticFaultAsync("BMS-ELEC-GRID", "NORMAL");
        await service.TriggerDiagnosticFaultAsync("BMS-HAZARD-B2", "NORMAL");

        Assert.False(chillerDev.Chiller.IsTempExceeded);
        Assert.DoesNotContain(pumpDev.Pumps, p => p.IsThermalOverloadTripped);
        Assert.False(gridDev.PowerGrid.IsPhaseLossDetected);
        Assert.False(hazardDev.Hazard.IsGasLeakDetected);
    }
}

