using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Models.Bms;

public enum BmsCategory
{
    ChillerHvac,
    PumpSystem,
    ElectricalGrid,
    EnvironmentalHazard
}

public class BmsDevice : SubsystemDevice
{
    public BmsDevice()
    {
        Subsystem = SubsystemType.Bms;
    }

    public BmsCategory Category { get; set; } = BmsCategory.ChillerHvac;
    public ChillerMetrics? Chiller { get; set; }
    public List<PumpMetrics> Pumps { get; set; } = [];
    public PowerGridMetrics? PowerGrid { get; set; }
    public GeneratorMetrics? Generator { get; set; }
    public HazardSensorMetrics? Hazard { get; set; }
}
