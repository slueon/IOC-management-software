using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Interfaces;

public interface ISubsystemHealthEvaluator
{
    List<SubsystemIncident> EvaluateDevice(SubsystemDevice device);
    SubsystemHealthSummary CalculateSummary(SubsystemType type, IEnumerable<SubsystemDevice> devices, IEnumerable<SubsystemIncident> incidents);
    OverallHealthSummary CalculateOverallSummary(IEnumerable<SubsystemHealthSummary> subsystemSummaries);
}

