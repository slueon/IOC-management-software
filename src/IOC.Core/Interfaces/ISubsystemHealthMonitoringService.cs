using IOC.Core.Enums;
using IOC.Core.Models.Base;

namespace IOC.Core.Interfaces;

public interface ISubsystemHealthMonitoringService
{
    event Action<SubsystemIncident>? IncidentRaised;
    event Action<OverallHealthSummary>? HealthUpdated;

    Task InitializeAsync();
    Task RefreshAllAsync();
    Task<IReadOnlyList<SubsystemDevice>> GetDevicesBySubsystemAsync(SubsystemType subsystem);
    Task<IReadOnlyList<SubsystemIncident>> GetActiveIncidentsAsync(SubsystemType? subsystem = null);
    Task<SubsystemHealthSummary> GetSubsystemSummaryAsync(SubsystemType subsystem);
    Task<OverallHealthSummary> GetOverallSummaryAsync();
    Task AcknowledgeIncidentAsync(string incidentId, string operatorName);
    Task ResolveIncidentAsync(string incidentId);
    Task TriggerDiagnosticFaultAsync(string deviceId, string faultScenario);
}

