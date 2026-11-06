using System.Collections.Concurrent;
using IOC.Core.Contracts.Persistence;
using IOC.Core.Contracts.Session;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Base;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;
using IOC.Core.Models.PublicAddress;
using IOC.Infrastructure.Persistence;
using IOC.Infrastructure.Session;

namespace IOC.Infrastructure.Services;

public class SubsystemHealthMonitoringService : ISubsystemHealthMonitoringService
{
    private readonly ISubsystemHealthEvaluator _evaluator;
    private readonly IIocStorageService _storageService;
    private readonly IOperatorSessionService _sessionService;
    private readonly ConcurrentDictionary<string, SubsystemDevice> _devices = new();
    private readonly ConcurrentDictionary<string, SubsystemIncident> _incidents = new();
    private readonly object _syncLock = new();

    public event Action<SubsystemIncident>? IncidentRaised;
    public event Action<OverallHealthSummary>? HealthUpdated;

    public IIocStorageService StorageService => _storageService;
    public IOperatorSessionService SessionService => _sessionService;

    public SubsystemHealthMonitoringService() 
        : this(new SubsystemHealthEvaluator(), new JsonIocStorageService(), new OperatorSessionService())
    {
    }

    public SubsystemHealthMonitoringService(ISubsystemHealthEvaluator evaluator)
        : this(evaluator, new JsonIocStorageService(), new OperatorSessionService())
    {
    }

    public SubsystemHealthMonitoringService(
        ISubsystemHealthEvaluator evaluator,
        IIocStorageService storageService,
        IOperatorSessionService sessionService)
    {
        _evaluator = evaluator;
        _storageService = storageService;
        _sessionService = sessionService;
    }

    public SubsystemDevice? GetDeviceById(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var dev) ? dev : null;
    }

    public Task SimulateFaultAsync(string deviceId, string faultScenario)
    {
        return TriggerDiagnosticFaultAsync(deviceId, faultScenario);
    }

    public Task InitializeAsync()
    {
        lock (_syncLock)
        {
            if (_devices.IsEmpty)
            {
                SeedDefaultSubsystemDevices();
                RunFullEvaluation();
            }
        }
        return Task.CompletedTask;
    }

    public Task RefreshAllAsync()
    {
        lock (_syncLock)
        {
            RunFullEvaluation();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubsystemDevice>> GetDevicesBySubsystemAsync(SubsystemType subsystem)
    {
        var result = _devices.Values.Where(d => d.Subsystem == subsystem).ToList();
        return Task.FromResult<IReadOnlyList<SubsystemDevice>>(result);
    }

    public Task<IReadOnlyList<SubsystemIncident>> GetActiveIncidentsAsync(SubsystemType? subsystem = null)
    {
        var query = _incidents.Values.Where(i => !i.IsResolved);
        if (subsystem.HasValue)
        {
            query = query.Where(i => i.Subsystem == subsystem.Value);
        }
        var result = query.OrderByDescending(i => i.Severity).ThenByDescending(i => i.Timestamp).ToList();
        return Task.FromResult<IReadOnlyList<SubsystemIncident>>(result);
    }

    public Task<SubsystemHealthSummary> GetSubsystemSummaryAsync(SubsystemType subsystem)
    {
        var summary = _evaluator.CalculateSummary(subsystem, _devices.Values, _incidents.Values);
        return Task.FromResult(summary);
    }

    public Task<OverallHealthSummary> GetOverallSummaryAsync()
    {
        var summaries = Enum.GetValues<SubsystemType>()
            .Select(st => _evaluator.CalculateSummary(st, _devices.Values, _incidents.Values))
            .ToList();

        var overall = _evaluator.CalculateOverallSummary(summaries);
        return Task.FromResult(overall);
    }

    public async Task AcknowledgeIncidentAsync(string incidentId, string operatorName)
    {
        var resolvedOperator = string.IsNullOrWhiteSpace(operatorName) 
            ? _sessionService.CurrentOperator.FullName 
            : operatorName;

        if (_incidents.TryGetValue(incidentId, out var inc))
        {
            inc.IsAcknowledged = true;
            inc.AcknowledgedBy = resolvedOperator;
            inc.AcknowledgedAt = DateTime.UtcNow;

            _ = _storageService.RecordAcknowledgementAsync(new IncidentAcknowledgementRecord
            {
                IncidentId = Guid.TryParse(inc.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
                Subsystem = inc.Subsystem.ToString(),
                DeviceName = inc.DeviceName,
                IncidentTitle = inc.Title,
                OperatorId = _sessionService.CurrentOperator.OperatorId,
                OperatorName = resolvedOperator,
                AcknowledgedAt = DateTime.UtcNow
            });

            _ = _storageService.AppendAuditLogAsync(new AuditLogEntry
            {
                OperatorId = _sessionService.CurrentOperator.OperatorId,
                OperatorName = resolvedOperator,
                Subsystem = inc.Subsystem.ToString(),
                Action = "TIẾP NHẬN SỰ CỐ (ACK)",
                Details = $"Tiếp nhận sự cố: {inc.Title} trên thiết bị {inc.DeviceName}",
                IsSuccess = true
            });

            NotifyHealthUpdated();
        }
        await Task.CompletedTask;
    }

    public async Task ResolveIncidentAsync(string incidentId)
    {
        if (_incidents.TryGetValue(incidentId, out var inc))
        {
            inc.IsResolved = true;
            inc.ResolvedAt = DateTime.UtcNow;

            _ = _storageService.AppendAuditLogAsync(new AuditLogEntry
            {
                OperatorId = _sessionService.CurrentOperator.OperatorId,
                OperatorName = _sessionService.CurrentOperator.FullName,
                Subsystem = inc.Subsystem.ToString(),
                Action = "GIẢI QUYẾT SỰ CỐ (RESOLVE)",
                Details = $"Đã khắc phục sự cố: {inc.Title} trên thiết bị {inc.DeviceName}",
                IsSuccess = true
            });

            if (_devices.TryGetValue(inc.DeviceId, out var dev))
            {
                ApplyFaultToDevice(dev, "NORMAL");
            }

            RunFullEvaluation();
        }
        await Task.CompletedTask;
    }

    public Task TriggerDiagnosticFaultAsync(string deviceId, string faultScenario)
    {
        var scenario = faultScenario?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(scenario)) return Task.CompletedTask;

        _ = _storageService.AppendAuditLogAsync(new AuditLogEntry
        {
            OperatorId = _sessionService.CurrentOperator.OperatorId,
            OperatorName = _sessionService.CurrentOperator.FullName,
            Subsystem = "DIỄN TẬP / MÔ PHỎNG",
            Action = "KÍCH HOẠT KỊCH BẢN",
            Details = $"Thiết bị: {deviceId} | Kịch bản: {scenario}",
            IsSuccess = true
        });

        if (scenario.Equals("CLEAR_ALL", StringComparison.OrdinalIgnoreCase))
        {
            ClearAllFaultsInternal();
            RunFullEvaluation();
            return Task.CompletedTask;
        }

        _devices.TryGetValue(deviceId, out var device);
        device = ResolveTargetDeviceForScenario(device, scenario);

        if (device != null)
        {
            ApplyFaultToDevice(device, scenario);
        }

        RunFullEvaluation();
        return Task.CompletedTask;
    }

    private SubsystemDevice? ResolveTargetDeviceForScenario(SubsystemDevice? currentDevice, string scenario)
    {
        var sc = scenario.ToUpperInvariant();

        if (currentDevice != null && sc.Equals("NORMAL"))
        {
            return currentDevice;
        }

        if (sc.StartsWith("NVR_") || sc.Contains("RAID"))
        {
            if (currentDevice is CctvDevice nvr && nvr.DeviceType == CctvDeviceType.NvrRecorder) return currentDevice;
            return _devices.Values.OfType<CctvDevice>().FirstOrDefault(c => c.DeviceType == CctvDeviceType.NvrRecorder) ?? currentDevice;
        }
        if (sc.Contains("VIDEO") || sc.Contains("LATENCY") || sc.Contains("PING_DROP"))
        {
            if (currentDevice is CctvDevice cam && cam.DeviceType == CctvDeviceType.IpCamera) return currentDevice;
            return _devices.Values.OfType<CctvDevice>().FirstOrDefault(c => c.DeviceType == CctvDeviceType.IpCamera) ?? currentDevice;
        }

        if (sc.Contains("CHILLER") || sc.Contains("PRESSURE"))
        {
            if (currentDevice is BmsDevice bms && bms.Chiller != null) return currentDevice;
            return _devices.Values.OfType<BmsDevice>().FirstOrDefault(b => b.Chiller != null) ?? currentDevice;
        }
        if (sc.Contains("PUMP"))
        {
            if (currentDevice is BmsDevice bms && bms.Pumps.Any()) return currentDevice;
            return _devices.Values.OfType<BmsDevice>().FirstOrDefault(b => b.Pumps.Any()) ?? currentDevice;
        }
        if (sc.Contains("PHASE") || sc.Contains("CURRENT") || sc.Contains("VOLTAGE") || sc.Contains("GEN_"))
        {
            if (currentDevice is BmsDevice bms && (bms.PowerGrid != null || bms.Generator != null)) return currentDevice;
            return _devices.Values.OfType<BmsDevice>().FirstOrDefault(b => b.PowerGrid != null || b.Generator != null) ?? currentDevice;
        }
        if (sc.Contains("SUMP") || sc.Contains("GAS"))
        {
            if (currentDevice is BmsDevice bms && bms.Hazard != null) return currentDevice;
            return _devices.Values.OfType<BmsDevice>().FirstOrDefault(b => b.Hazard != null) ?? currentDevice;
        }

        if (sc.Contains("AMPLIFIER") || sc.Contains("IMPEDANCE") || sc.Contains("SPEAKER") || sc.Contains("PA_") || sc.Contains("FUSE"))
        {
            if (currentDevice is PublicAddressDevice) return currentDevice;
            return _devices.Values.OfType<PublicAddressDevice>().FirstOrDefault() ?? currentDevice;
        }

        if (sc.Contains("PBX") || sc.StartsWith("EXT_"))
        {
            if (currentDevice is LanTelDevice lanPbx && (lanPbx.PbxTrunk != null || lanPbx.Extensions.Any())) return currentDevice;
            return _devices.Values.OfType<LanTelDevice>().FirstOrDefault(l => l.PbxTrunk != null || l.Extensions.Any()) ?? currentDevice;
        }
        if (sc.Contains("PORT") || sc.Contains("CABLE") || sc.Contains("CPU") || sc.Contains("RAM") || sc.Equals("OVERHEAT") || sc.Contains("CONGESTION") || sc.Contains("LAN") || sc.Equals("SWITCH_DOWN"))
        {
            if (currentDevice is LanTelDevice lanSwitch && lanSwitch.Ports.Any()) return currentDevice;
            return _devices.Values.OfType<LanTelDevice>().FirstOrDefault(l => l.Ports.Any()) ?? currentDevice;
        }

        if (sc.Contains("DOOR"))
        {
            if (currentDevice is AccessControlDevice acDoor && acDoor.DoorPoints.Any()) return currentDevice;
            return _devices.Values.OfType<AccessControlDevice>().FirstOrDefault(a => a.DoorPoints.Any()) ?? currentDevice;
        }
        if (sc.Contains("ELEVATOR"))
        {
            if (currentDevice is AccessControlDevice acElev && acElev.Elevators.Any()) return currentDevice;
            return _devices.Values.OfType<AccessControlDevice>().FirstOrDefault(a => a.Elevators.Any()) ?? currentDevice;
        }

        if (sc.StartsWith("FIRE_") || sc.Contains("LOOP_") || sc.Equals("FIRE_ALERT"))
        {
            if (currentDevice is FireAlarmDevice fa && fa.Loops.Any()) return currentDevice;
            return _devices.Values.OfType<FireAlarmDevice>().FirstOrDefault(f => f.Loops.Any()) ?? currentDevice;
        }

        return currentDevice ?? _devices.Values.FirstOrDefault();
    }

    private void ApplyFaultToDevice(SubsystemDevice device, string scenario)
    {
        var sc = scenario.ToUpperInvariant();

        if (sc == "NORMAL")
        {
            foreach (var d in _devices.Values.Where(d => d.Subsystem == device.Subsystem))
            {
                ApplyFaultCore(d, "NORMAL");
            }
        }
        else
        {
            ApplyFaultCore(device, scenario);
        }
    }

    private static void ApplyFaultCore(SubsystemDevice device, string scenario)
    {
        switch (device)
        {
            case LanTelDevice lan:
                ApplyLanFault(lan, scenario);
                break;
            case PublicAddressDevice pa:
                ApplyPaFault(pa, scenario);
                break;
            case CctvDevice cctv:
                ApplyCctvFault(cctv, scenario);
                break;
            case AccessControlDevice ac:
                ApplyAccessFault(ac, scenario);
                break;
            case BmsDevice bms:
                ApplyBmsFault(bms, scenario);
                break;
            case FireAlarmDevice fa:
                ApplyFireAlarmFault(fa, scenario);
                break;
        }
    }

    private void ClearAllFaultsInternal()
    {
        foreach (var device in _devices.Values)
        {
            ApplyFaultCore(device, "NORMAL");
        }
    }

    private static void ApplyLanFault(LanTelDevice lan, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "PORT_DOWN":
            case "CABLE_CUT":
                if (lan.Ports.Any())
                {
                    lan.Ports[0].IsUp = false;
                    lan.Ports[0].CableFaultTimestamp = DateTime.UtcNow;
                }
                break;
            case "PORT_UP":
            case "RESTORE_CABLE":
                if (lan.Ports.Any())
                {
                    lan.Ports[0].IsUp = true;
                    lan.Ports[0].CableFaultTimestamp = null;
                }
                break;
            case "CPU_OVERLOAD":
                lan.CpuUsagePercent = 96.5;
                break;
            case "RAM_OVERFLOW":
                lan.MemoryUsedMb = lan.MemoryTotalMb * 0.94;
                break;
            case "OVERHEAT":
                lan.TemperatureCelsius = 82.5;
                break;
            case "CONGESTION":
                if (lan.Ports.Any())
                {
                    lan.Ports[0].RxRateMbps = 550.0;
                    lan.Ports[0].TxRateMbps = 380.0; // 930 Mbps on Gigabit port (93%)
                }
                lan.BandwidthUtilizationPercent = 92.0;
                break;
            case "PBX_DOWN":
                if (lan.PbxTrunk != null)
                {
                    lan.PbxTrunk.IsSipTrunkUp = false;
                    lan.PbxTrunk.DownTimestamp = DateTime.UtcNow;
                }
                break;
            case "EXT_DISCONNECT":
                var targetExt = lan.Extensions.FirstOrDefault(e => e.ExtensionNumber == "102") ?? lan.Extensions.FirstOrDefault();
                if (targetExt != null) targetExt.Status = PbxExtensionStatus.Disconnected;
                break;
            case "EXT_RESTORE":
                foreach (var e in lan.Extensions) e.Status = PbxExtensionStatus.Registered;
                break;
            case "NORMAL":
                lan.CpuUsagePercent = 28.0;
                lan.MemoryUsedMb = lan.MemoryTotalMb * 0.38;
                lan.TemperatureCelsius = 42.0;
                lan.BandwidthUtilizationPercent = 42.0;
                foreach (var p in lan.Ports)
                {
                    p.IsUp = true;
                    p.RxRateMbps = 15.0;
                    p.TxRateMbps = 12.0;
                    p.CableFaultTimestamp = null;
                }
                if (lan.PbxTrunk != null)
                {
                    lan.PbxTrunk.IsSipTrunkUp = true;
                    lan.PbxTrunk.DownTimestamp = null;
                }
                foreach (var e in lan.Extensions)
                {
                    e.Status = PbxExtensionStatus.Registered;
                }
                break;
        }
    }

    private static void ApplyPaFault(PublicAddressDevice pa, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "MAIN_POWER_DOWN":
                pa.IsMainPowerOk = false;
                pa.MainPowerVoltageVolt = 0.0;
                pa.BatteryVoltageVolt = 24.2;
                pa.BatteryChargeCurrentAmp = -4.5;
                break;
            case "BATTERY_LOW":
                pa.IsBackupBatteryOk = false;
                pa.BatteryVoltageVolt = 19.8;
                pa.BatteryLevelPercent = 14.0;
                break;
            case "FUSE_BLOWN":
                if (pa.Amplifiers.Any()) pa.Amplifiers[0].IsFuseBlown = true;
                break;
            case "AMP_OVERHEAT":
                if (pa.Amplifiers.Any())
                {
                    pa.Amplifiers[0].TemperatureCelsius = 87.5;
                    pa.Amplifiers[0].FanStatus = "Kẹt quạt tản nhiệt / Quá nhiệt";
                }
                break;
            case "AMP_OVERLOAD":
                if (pa.Amplifiers.Any())
                {
                    pa.Amplifiers[0].OutputLoadWatt = 540.0;
                    pa.Amplifiers[0].RatedPowerWatt = 480.0;
                }
                break;
            case "LINE_SHORT":
            case "SPEAKER_FAULT":
                if (pa.SpeakerZones.Any()) pa.SpeakerZones[0].ImpedanceOhms = 4.2;
                break;
            case "LINE_OPEN":
                if (pa.SpeakerZones.Count > 1)
                {
                    pa.SpeakerZones[1].ImpedanceOhms = 999.0;
                }
                else if (pa.SpeakerZones.Any())
                {
                    pa.SpeakerZones[0].ImpedanceOhms = 999.0;
                }
                break;
            case "LINE_DEGRADED":
                if (pa.SpeakerZones.Count > 2)
                {
                    pa.SpeakerZones[2].ImpedanceOhms = 58.0;
                }
                break;
            case "NORMAL":
                pa.IsMainPowerOk = true;
                pa.MainPowerVoltageVolt = 220.0;
                pa.IsBackupBatteryOk = true;
                pa.BatteryVoltageVolt = 27.2;
                pa.BatteryLevelPercent = 98.0;
                pa.BatteryChargeCurrentAmp = 1.2;
                foreach (var amp in pa.Amplifiers)
                {
                    amp.IsFuseBlown = false;
                    amp.TemperatureCelsius = 46.0;
                    amp.OutputLoadWatt = 240.0;
                    amp.RatedPowerWatt = 480.0;
                    amp.FanStatus = "Quạt chạy bình thường";
                    amp.IsProtectionActive = false;
                }
                foreach (var z in pa.SpeakerZones) z.ImpedanceOhms = z.NormalImpedanceOhms;
                break;
        }
    }

    private static void ApplyCctvFault(CctvDevice cctv, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "VIDEO_LOSS":
                cctv.IsVideoLoss = true;
                cctv.IsRtspStreamActive = false;
                cctv.BitrateKbps = 0;
                break;
            case "OFFLINE":
                cctv.Status = DeviceStatus.Offline;
                cctv.MissedHeartbeats = 5;
                cctv.IsRtspStreamActive = false;
                break;
            case "NVR_BAD_SECTOR":
                if (cctv.NvrStorage != null)
                {
                    cctv.NvrStorage.HasBadSector = true;
                    cctv.NvrStorage.BadSectorCount = 18;
                }
                break;
            case "NVR_RAID_DEGRADED":
                if (cctv.NvrStorage != null)
                {
                    cctv.NvrStorage.RaidStatus = RaidHealthStatus.Degraded;
                }
                break;
            case "NVR_FULL":
                if (cctv.NvrStorage != null)
                {
                    cctv.NvrStorage.StorageUsedGb = cctv.NvrStorage.StorageTotalGb * 0.965; // 96.5%
                }
                break;
            case "NORMAL":
                cctv.Status = DeviceStatus.Online;
                cctv.IsVideoLoss = false;
                cctv.IsRtspStreamActive = true;
                cctv.MissedHeartbeats = 0;
                cctv.BitrateKbps = 4096;
                if (cctv.NvrStorage != null)
                {
                    cctv.NvrStorage.HasBadSector = false;
                    cctv.NvrStorage.BadSectorCount = 0;
                    cctv.NvrStorage.RaidStatus = RaidHealthStatus.Healthy;
                    cctv.NvrStorage.StorageUsedGb = 9800.0;
                }
                break;
        }
    }

    private static void ApplyAccessFault(AccessControlDevice ac, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "DOOR_FORCED":
                if (ac.DoorPoints.Any())
                {
                    ac.DoorPoints[0].IsForcedOpen = true;
                    ac.DoorPoints[0].IsOpen = true;
                }
                break;
            case "DOOR_HELD":
                if (ac.DoorPoints.Count != 0)
                {
                    ac.DoorPoints[0].IsHeldOpen = true;
                    ac.DoorPoints[0].IsOpen = true;
                    ac.DoorPoints[0].HeldDurationSeconds = 180;
                }
                break;
            case "DOOR_OFFLINE":
                if (ac.DoorPoints.Any())
                {
                    ac.DoorPoints[0].IsConnected = false;
                    ac.DoorPoints[0].ReaderStatus = "MẤT KẾT NỐI";
                }
                break;
            case "CONTROLLER_OFFLINE":
                ac.IsControllerOnline = false;
                ac.Status = DeviceStatus.Offline;
                break;
            case "POWER_LOSS":
                ac.IsMainPowerOk = false;
                ac.BatteryLevelPercent = 75.0;
                break;
            case "BATTERY_LOW":
                ac.IsMainPowerOk = false;
                ac.BatteryLevelPercent = 14.0;
                break;
            case "ELEVATOR_EMERGENCY":
                if (ac.Elevators.Any())
                {
                    ac.Elevators[0].IsEmergencyStop = true;
                    ac.Elevators[0].OperationalStatus = "DỪNG KHẨN CẤP";
                }
                break;
            case "NORMAL":
                ac.IsControllerOnline = true;
                ac.IsMainPowerOk = true;
                ac.BatteryLevelPercent = 95.0;
                ac.Status = DeviceStatus.Online;
                foreach (var d in ac.DoorPoints)
                {
                    d.IsForcedOpen = false;
                    d.IsHeldOpen = false;
                    d.HeldDurationSeconds = 0;
                    d.IsConnected = true;
                    d.ReaderStatus = "ONLINE";
                    d.IsOpen = false;
                    d.IsLocked = true;
                }
                foreach (var e in ac.Elevators)
                {
                    e.IsEmergencyStop = false;
                    e.IsConnected = true;
                    e.LoadKg = 240.0;
                    e.OperationalStatus = "Hoạt động bình thường";
                }
                break;
        }
    }

    private static void ApplyBmsFault(BmsDevice bms, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "CHILLER_OVERHEAT":
                if (bms.Chiller != null)
                {
                    bms.Chiller.SupplyWaterTempCelsius = 19.5;
                    bms.Chiller.ReturnWaterTempCelsius = 24.2;
                }
                break;
            case "PRESSURE_FAULT":
                if (bms.Chiller != null)
                {
                    bms.Chiller.SystemPressureBar = 0.8;
                }
                break;
            case "PUMP_TRIP":
                if (bms.Pumps.Any())
                {
                    bms.Pumps[0].IsThermalOverloadTripped = true;
                    bms.Pumps[0].IsRunning = false;
                    bms.Pumps[0].FlowRateM3h = 0.0;
                }
                break;
            case "PHASE_LOSS":
                if (bms.PowerGrid != null)
                {
                    bms.PowerGrid.VoltagePhaseA = 12.0;
                }
                break;
            case "CURRENT_OVERLOAD":
                if (bms.PowerGrid != null)
                {
                    bms.PowerGrid.CurrentPhaseA = 245.0;
                }
                break;
            case "VOLTAGE_SAG":
                if (bms.PowerGrid != null)
                {
                    bms.PowerGrid.VoltagePhaseA = 182.0;
                    bms.PowerGrid.VoltagePhaseB = 184.0;
                    bms.PowerGrid.VoltagePhaseC = 185.0;
                }
                break;
            case "GEN_LOW_FUEL":
                if (bms.Generator != null)
                {
                    bms.Generator.FuelLevelPercent = 18.0;
                }
                break;
            case "SUMP_EMPTY":
                if (bms.Hazard != null)
                {
                    bms.Hazard.SumpPitWaterLevelPercent = 10.0;
                }
                break;
            case "GAS_LEAK":
                if (bms.Hazard != null)
                {
                    bms.Hazard.GasCoConcentrationPpm = 95.0;
                    bms.Hazard.IsExhaustFanRunning = true;
                }
                break;
            case "NORMAL":
                if (bms.Chiller != null)
                {
                    bms.Chiller.SupplyWaterTempCelsius = 7.2;
                    bms.Chiller.ReturnWaterTempCelsius = 12.1;
                    bms.Chiller.SystemPressureBar = 3.6;
                    foreach (var u in bms.Chiller.HvacUnits)
                    {
                        u.IsFilterClogged = false;
                    }
                }
                foreach (var p in bms.Pumps)
                {
                    p.IsThermalOverloadTripped = false;
                    p.IsRunning = true;
                    p.FlowRateM3h = p.PumpType.Contains("PCCC", StringComparison.OrdinalIgnoreCase) ? 0.0 : 45.0;
                }
                if (bms.PowerGrid != null)
                {
                    bms.PowerGrid.VoltagePhaseA = 220.5;
                    bms.PowerGrid.VoltagePhaseB = 219.8;
                    bms.PowerGrid.VoltagePhaseC = 221.0;
                    bms.PowerGrid.CurrentPhaseA = 140.0;
                    bms.PowerGrid.CurrentPhaseB = 138.0;
                    bms.PowerGrid.CurrentPhaseC = 142.0;
                }
                if (bms.Generator != null)
                {
                    bms.Generator.FuelLevelPercent = 85.0;
                    bms.Generator.BatteryVoltageVolt = 26.5;
                    bms.Generator.IsRunning = false;
                    bms.Generator.GeneratorStatus = "SẴN SÀNG (STANDBY)";
                }
                if (bms.Hazard != null)
                {
                    bms.Hazard.GasCoConcentrationPpm = 8.0;
                    bms.Hazard.GasLpgConcentrationLelPercent = 0.0;
                    bms.Hazard.SumpPitWaterLevelPercent = 80.0;
                    bms.Hazard.IsExhaustFanRunning = false;
                }
                break;
        }
    }

    private static void ApplyFireAlarmFault(FireAlarmDevice fa, string scenario)
    {
        switch (scenario.ToUpperInvariant())
        {
            case "FIRE_ALARM":
            case "FIRE_SMOKE":
                if (fa.Loops.Any() && fa.Loops[0].Sensors.Any())
                {
                    fa.Loops[0].Sensors[0].IsAlarmTriggered = true;
                    fa.Loops[0].Sensors[0].AnalogValue = 4.8;
                    fa.Loops[0].Sensors[0].AlarmTimestamp = DateTime.Now;
                }
                break;
            case "FIRE_HEAT":
                if (fa.Loops.Any() && fa.Loops[0].Sensors.Count > 2)
                {
                    fa.Loops[0].Sensors[2].IsAlarmTriggered = true;
                    fa.Loops[0].Sensors[2].AnalogValue = 68.5;
                    fa.Loops[0].Sensors[2].AlarmTimestamp = DateTime.Now;
                }
                break;
            case "FIRE_MCP":
            case "FIRE_CALL_POINT":
                if (fa.Loops.Any() && fa.Loops[0].Sensors.Count > 4)
                {
                    fa.Loops[0].Sensors[4].IsAlarmTriggered = true;
                    fa.Loops[0].Sensors[4].AlarmTimestamp = DateTime.Now;
                }
                break;
            case "LOOP_FAULT":
                if (fa.Loops.Any())
                {
                    fa.Loops[0].IsLoopOpenFault = true;
                    fa.Loops[0].FaultLocationDescription = "Đứt cáp tín hiệu giữa địa chỉ L1-S02 và L1-H01 (Khu vực Hành lang Tầng 2)";
                }
                break;
            case "POWER_LOSS":
                fa.IsAcPowerOk = false;
                fa.IsBatteryBackupActive = true;
                fa.BatteryVoltageVolt = 25.4;
                fa.BatteryLevelPercent = 78.0;
                break;
            case "BATTERY_LOW":
                fa.IsAcPowerOk = false;
                fa.IsBatteryBackupActive = true;
                fa.BatteryVoltageVolt = 20.2;
                fa.BatteryLevelPercent = 15.0;
                break;
            case "NORMAL":
                foreach (var loop in fa.Loops)
                {
                    loop.IsLoopOpenFault = false;
                    loop.IsGroundFault = false;
                    loop.FaultLocationDescription = "Mạch vòng khép kín Class A (Bình thường)";
                    foreach (var s in loop.Sensors)
                    {
                        s.IsAlarmTriggered = false;
                        s.IsFault = false;
                        s.AlarmTimestamp = null;
                        if (s.SensorType == FireAlarmSensorType.SmokeDetector) s.AnalogValue = 1.1;
                        if (s.SensorType == FireAlarmSensorType.HeatDetector) s.AnalogValue = 28.0;
                    }
                }
                fa.IsAcPowerOk = true;
                fa.IsBatteryBackupActive = false;
                fa.BatteryVoltageVolt = 26.8;
                fa.BatteryLevelPercent = 96.0;
                break;
        }
    }

    private void RunFullEvaluation()
    {
        foreach (var device in _devices.Values)
        {
            var detected = _evaluator.EvaluateDevice(device);

            // Tự động khôi phục / đóng sự cố khi tình trạng phần cứng đã trở lại bình thường
            var activeForDevice = _incidents.Values
                .Where(i => i.DeviceId == device.Id && !i.IsResolved)
                .ToList();

            foreach (var active in activeForDevice)
            {
                if (!detected.Any(d => d.Title == active.Title))
                {
                    active.IsResolved = true;
                    active.ResolvedAt = DateTime.UtcNow;
                }
            }

            foreach (var inc in detected)
            {
                var existing = _incidents.Values.FirstOrDefault(i =>
                    i.DeviceId == inc.DeviceId &&
                    i.Title == inc.Title &&
                    !i.IsResolved);

                if (existing == null)
                {
                    _incidents[inc.Id] = inc;
                    IncidentRaised?.Invoke(inc);
                }
                else
                {
                    // Duplicate incident suppression: update timestamp to latest occurrence instead of duplicating
                    existing.Timestamp = inc.Timestamp;
                }
            }
        }

        _ = _storageService.SaveDataAsync("active_incidents", _incidents.Values.Where(i => !i.IsResolved).ToList());
        NotifyHealthUpdated();
    }

    private void NotifyHealthUpdated()
    {
        var summaries = Enum.GetValues<SubsystemType>()
            .Select(st => _evaluator.CalculateSummary(st, _devices.Values, _incidents.Values))
            .ToList();

        var overall = _evaluator.CalculateOverallSummary(summaries);
        HealthUpdated?.Invoke(overall);
    }

    private void SeedDefaultSubsystemDevices()
    {
        // 1. LAN/Tel
        var coreSwitch = new LanTelDevice
        {
            Id = "SW-CORE-01",
            Name = "Switch Core Trung Tâm (Cisco Catalyst 9300-24T)",
            DeviceType = LanDeviceType.SwitchCore,
            IpAddress = "192.168.10.1",
            Port = 161,
            Location = "Phòng Máy Chủ Server Room (Tầng 3)",
            Manufacturer = "Cisco",
            Model = "Catalyst 9300-24T",
            CpuUsagePercent = 32.5,
            MemoryTotalMb = 4096.0,
            MemoryUsedMb = 1540.0,
            TemperatureCelsius = 43.5,
            BandwidthUtilizationPercent = 48.0,
            Psu1Status = "OK",
            Psu2Status = "OK",
            Fan1Rpm = 4250,
            Fan2Rpm = 4180
        };

        var coreConnectedDevices = new[]
        {
            ("Uplink Switch Edge Tầng 1", "Hạ tầng", 10, 380.0, 210.0),
            ("Uplink Switch Edge Tầng 2", "Hạ tầng", 10, 290.0, 140.0),
            ("Máy Chủ NVR Storage 64 Ch", "CCTV", 30, 620.0, 110.0),
            ("Gateway BMS Master", "BMS", 40, 45.0, 30.0),
            ("Bộ Điều Khiển Cửa Access", "Access", 50, 12.0, 8.0),
            ("Gateway Báo Cháy PCCC", "PCCC", 60, 5.0, 2.0),
            ("Tủ Trung Tâm Âm Thanh PA", "PA", 70, 20.0, 15.0),
            ("Tổng Đài IP-PBX UCM6304", "Voice", 20, 40.0, 35.0),
            ("Server Giám Sát IOC Master", "Máy chủ", 1, 150.0, 95.0),
            ("Firewall Biên FortiGate 100F", "Bảo mật", 99, 210.0, 180.0),
            ("Máy Trạm Kỹ Sư Trực Ban 01", "Trực ban", 1, 25.0, 15.0),
            ("Máy Trạm Kỹ Sư Trực Ban 02", "Trực ban", 1, 18.0, 10.0)
        };

        for (int i = 1; i <= 24; i++)
        {
            if (i <= coreConnectedDevices.Length)
            {
                var (devName, cat, vlan, rx, tx) = coreConnectedDevices[i - 1];
                coreSwitch.Ports.Add(new SwitchPortInfo
                {
                    PortNumber = i,
                    PortName = $"Gi1/0/{i}",
                    IsUp = true,
                    IsAdminEnabled = true,
                    SpeedMbps = 1000,
                    VlanId = vlan,
                    ConnectedDevice = devName,
                    DeviceCategory = cat,
                    RxRateMbps = rx,
                    TxRateMbps = tx
                });
            }
            else
            {
                coreSwitch.Ports.Add(new SwitchPortInfo
                {
                    PortNumber = i,
                    PortName = $"Gi1/0/{i}",
                    IsUp = false,
                    IsAdminEnabled = false,
                    SpeedMbps = 1000,
                    VlanId = 1,
                    ConnectedDevice = "Cổng Dự Phòng (Unused)",
                    DeviceCategory = "Dự phòng"
                });
            }
        }
        AddDevice(coreSwitch);

        AddDevice(new LanTelDevice
        {
            Id = "SW-EDGE-T1",
            Name = "Switch Phân Phối Tầng 1",
            DeviceType = LanDeviceType.SwitchEdge,
            IpAddress = "192.168.10.11",
            Port = 161,
            Location = "Tủ Rack Tầng 1",
            Manufacturer = "Cisco",
            Model = "Catalyst 2960X",
            CpuUsagePercent = 26.0,
            MemoryTotalMb = 1024.0,
            MemoryUsedMb = 380.0,
            TemperatureCelsius = 39.0,
            BandwidthUtilizationPercent = 35.0,
            Ports = new List<SwitchPortInfo>
            {
                new() { PortNumber = 1, PortName = "Gi1/0/1", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "Cửa Kiểm Soát Vào Ra Sảnh", DeviceCategory = "Access", RxRateMbps = 12.0, TxRateMbps = 8.0 },
                new() { PortNumber = 2, PortName = "Gi1/0/2", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "Camera Sảnh Chính Tầng 1", DeviceCategory = "CCTV", RxRateMbps = 35.0, TxRateMbps = 5.0 },
                new() { PortNumber = 3, PortName = "Gi1/0/3", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "Camera Hầm B1 Làn Vào", DeviceCategory = "CCTV", RxRateMbps = 28.0, TxRateMbps = 4.0 },
                new() { PortNumber = 4, PortName = "Gi1/0/4", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "IP Phone Lễ Tân 103", DeviceCategory = "Voice", RxRateMbps = 2.0, TxRateMbps = 2.0 }
            }
        });

        AddDevice(new LanTelDevice
        {
            Id = "SW-EDGE-T2",
            Name = "Switch Phân Phối Tầng 2",
            DeviceType = LanDeviceType.SwitchEdge,
            IpAddress = "192.168.10.12",
            Port = 161,
            Location = "Tủ Rack Tầng 2",
            Manufacturer = "Cisco",
            Model = "Catalyst 2960X",
            CpuUsagePercent = 22.0,
            MemoryTotalMb = 1024.0,
            MemoryUsedMb = 350.0,
            TemperatureCelsius = 38.5,
            BandwidthUtilizationPercent = 28.0,
            Ports = new List<SwitchPortInfo>
            {
                new() { PortNumber = 1, PortName = "Gi1/0/1", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "Camera Sảnh Thang Máy Tầng 2", DeviceCategory = "CCTV", RxRateMbps = 24.0, TxRateMbps = 3.0 },
                new() { PortNumber = 2, PortName = "Gi1/0/2", IsUp = true, SpeedMbps = 1000, ConnectedDevice = "AP Wifi Hành Lang Tầng 2", DeviceCategory = "Wifi", RxRateMbps = 95.0, TxRateMbps = 40.0 }
            }
        });

        AddDevice(new LanTelDevice
        {
            Id = "ROUTER-GW-01",
            Name = "Router Biên Gateway Mạng Tòa Nhà",
            DeviceType = LanDeviceType.Router,
            IpAddress = "192.168.10.254",
            Port = 161,
            Location = "Phòng Máy Chủ Server Room",
            Manufacturer = "Cisco",
            Model = "ISR 4331",
            CpuUsagePercent = 30.0,
            MemoryTotalMb = 4096.0,
            MemoryUsedMb = 1640.0,
            TemperatureCelsius = 42.0,
            BandwidthUtilizationPercent = 50.0
        });

        var pbx = new LanTelDevice
        {
            Id = "PBX-MAIN-01",
            Name = "Tổng Đài IP-PBX Tòa Nhà",
            DeviceType = LanDeviceType.IpPbx,
            IpAddress = "192.168.10.50",
            Port = 5060,
            Location = "Phòng Tổng Đài & IT",
            Manufacturer = "Grandstream",
            Model = "UCM6304",
            CpuUsagePercent = 18.0,
            MemoryTotalMb = 2048.0,
            MemoryUsedMb = 635.0,
            TemperatureCelsius = 40.0,
            PbxTrunk = new PbxTrunkInfo
            {
                TrunkName = "SIP-TRUNK-VNPT-MAIN",
                Provider = "VNPT Telecom",
                ServerIp = "10.200.1.10",
                IsSipTrunkUp = true,
                ActiveChannels = 6,
                MaxChannels = 30,
                LatencyMs = 4.2,
                CallCompletionRatePercent = 99.4
            },
            Extensions = new List<PbxExtensionInfo>
            {
                new() { ExtensionNumber = "101", DepartmentName = "Phòng Giám Đốc", AssignedPerson = "Trần Đình Khang", IpAddress = "192.168.20.101", PhoneModel = "Grandstream GRP2602", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = false },
                new() { ExtensionNumber = "102", DepartmentName = "Phòng Trực Điều Hành IOC", AssignedPerson = "Trực Ban Vận Hành", IpAddress = "192.168.20.102", PhoneModel = "Yealink T31P", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = true },
                new() { ExtensionNumber = "103", DepartmentName = "Lễ Tân Tòa Nhà Sảnh Chính", AssignedPerson = "Nguyễn Thu Hà", IpAddress = "192.168.20.103", PhoneModel = "Cisco 7821", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = false },
                new() { ExtensionNumber = "104", DepartmentName = "Trực Ban An Ninh & Bảo Vệ", AssignedPerson = "Đội Trưởng An Ninh", IpAddress = "192.168.20.104", PhoneModel = "Grandstream GRP2602", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = true },
                new() { ExtensionNumber = "105", DepartmentName = "Phòng Kỹ Thuật M&E", AssignedPerson = "Kỹ Sư Trực M&E", IpAddress = "192.168.20.105", PhoneModel = "Yealink T31P", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = true },
                new() { ExtensionNumber = "106", DepartmentName = "Chốt Kiểm Soát Xe Hầm B1", AssignedPerson = "Bảo Vệ Hầm B1", IpAddress = "192.168.20.106", PhoneModel = "Grandstream GRP2602", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = false },
                new() { ExtensionNumber = "107", DepartmentName = "Tổng Đài CSKH & Tiếp Dân", AssignedPerson = "Chăm Sóc Cư Dân", IpAddress = "192.168.20.107", PhoneModel = "Cisco 7821", Status = PbxExtensionStatus.InCall, IsCriticalDepartment = false },
                new() { ExtensionNumber = "108", DepartmentName = "Phòng Y Tế & Sơ Cứu", AssignedPerson = "Bác Sĩ Trực Ca", IpAddress = "192.168.20.108", PhoneModel = "Yealink T31P", Status = PbxExtensionStatus.Registered, IsCriticalDepartment = true }
            }
        };
        AddDevice(pbx);

        // 2. PA (Public Address)
        AddDevice(new PublicAddressDevice
        {
            Id = "PA-ZONE-EXP-01",
            Name = "Bộ Mở Rộng Vùng Âm Thanh Kỹ Thuật Số",
            IpAddress = "192.168.20.6",
            Port = 161,
            Location = "Tủ Kỹ Thuật Tầng 5",
            Manufacturer = "Bosch",
            Model = "PRS-16MCL",
            IsMainPowerOk = true,
            MainPowerVoltageVolt = 220.0,
            IsBackupBatteryOk = true,
            BatteryVoltageVolt = 26.8,
            BatteryLevelPercent = 96.0,
            BatteryChargeCurrentAmp = 0.8,
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new() { AmplifierId = "AMP-EXP-01", AmplifierName = "Ampli Mở Rộng Vùng 9-10", TemperatureCelsius = 44.0, OutputLoadWatt = 210.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 1 (100V Line)" },
                new() { AmplifierId = "AMP-EXP-02", AmplifierName = "Ampli Mở Rộng Vùng 11-12", TemperatureCelsius = 47.0, OutputLoadWatt = 195.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 2 (100V Line)" }
            },
            SpeakerZones = new List<SpeakerZoneInfo>
            {
                new() { ZoneId = 9, ZoneName = "Zone 9: Khu Kỹ Thuật M&E Tầng Mái", Location = "Tầng Mái", ImpedanceOhms = 110.0, NormalImpedanceOhms = 110.0, SpeakerCount = 10, ZonePowerWatt = 60.0, IsActive = true },
                new() { ZoneId = 10, ZoneName = "Zone 10: Khu Vực Nhà Ăn Cán Bộ", Location = "Tầng 4", ImpedanceOhms = 95.0, NormalImpedanceOhms = 95.0, SpeakerCount = 14, ZonePowerWatt = 80.0, IsActive = true },
                new() { ZoneId = 11, ZoneName = "Zone 11: Phòng Thể Thao & Đa Năng", Location = "Tầng 5", ImpedanceOhms = 100.0, NormalImpedanceOhms = 100.0, SpeakerCount = 12, ZonePowerWatt = 75.0, IsActive = true },
                new() { ZoneId = 12, ZoneName = "Zone 12: Bãi Đỗ Xe Nâng Hạ Tầng B3", Location = "Tầng Hầm B3", ImpedanceOhms = 105.0, NormalImpedanceOhms = 105.0, SpeakerCount = 16, ZonePowerWatt = 100.0, IsActive = true }
            }
        });

        AddDevice(new PublicAddressDevice
        {
            Id = "PA-CTRL-01",
            Name = "Tủ Trung Tâm Âm Thanh Thông Báo PA",
            IpAddress = "192.168.20.5",
            Port = 161,
            Location = "Phòng Điều Hành Trung Tâm (Tầng 1)",
            Manufacturer = "Bosch",
            Model = "Praesideo PRS-NCO3",
            IsMainPowerOk = true,
            MainPowerVoltageVolt = 220.4,
            IsBackupBatteryOk = true,
            BatteryVoltageVolt = 27.2,
            BatteryLevelPercent = 98.0,
            BatteryChargeCurrentAmp = 1.2,
            Amplifiers = new List<AmplifierHealthInfo>
            {
                new() { AmplifierId = "AMP-01", AmplifierName = "Ampli Công Suất Vùng 1-2 (Hầm & Sảnh)", TemperatureCelsius = 46.0, OutputLoadWatt = 280.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 1 (100V Line)" },
                new() { AmplifierId = "AMP-02", AmplifierName = "Ampli Công Suất Vùng 3-4 (Văn Phòng & Hội Trường)", TemperatureCelsius = 49.0, OutputLoadWatt = 310.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 2 (100V Line)" },
                new() { AmplifierId = "AMP-03", AmplifierName = "Ampli Công Suất Vùng 5-6 (Hành Lang & Khối Dịch Vụ)", TemperatureCelsius = 45.0, OutputLoadWatt = 250.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 3 (100V Line)" },
                new() { AmplifierId = "AMP-04", AmplifierName = "Ampli Dự Phòng & Báo Cháy Khẩn Cấp", TemperatureCelsius = 42.0, OutputLoadWatt = 190.0, RatedPowerWatt = 480.0, IsFuseBlown = false, ChannelInfo = "Kênh 4 (Standby/EVAC)" }
            },
            SpeakerZones = new List<SpeakerZoneInfo>
            {
                new() { ZoneId = 1, ZoneName = "Zone 1: Tầng Hầm B1-B2", Location = "Tầng Hầm", ImpedanceOhms = 100.0, NormalImpedanceOhms = 100.0, SpeakerCount = 24, ZonePowerWatt = 120.0, IsActive = true },
                new() { ZoneId = 2, ZoneName = "Zone 2: Sảnh Đón Khách & Lễ Tân", Location = "Tầng 1", ImpedanceOhms = 120.0, NormalImpedanceOhms = 120.0, SpeakerCount = 16, ZonePowerWatt = 90.0, IsActive = true },
                new() { ZoneId = 3, ZoneName = "Zone 3: Khối Văn Phòng Tầng 2-5", Location = "Tầng 2-5", ImpedanceOhms = 100.0, NormalImpedanceOhms = 100.0, SpeakerCount = 32, ZonePowerWatt = 180.0, IsActive = true },
                new() { ZoneId = 4, ZoneName = "Zone 4: Hội Trường & Phòng Hội Nghị", Location = "Tầng 6", ImpedanceOhms = 80.0, NormalImpedanceOhms = 80.0, SpeakerCount = 12, ZonePowerWatt = 120.0, IsActive = true },
                new() { ZoneId = 5, ZoneName = "Zone 5: Khối Văn Phòng Tầng 7-10", Location = "Tầng 7-10", ImpedanceOhms = 100.0, NormalImpedanceOhms = 100.0, SpeakerCount = 32, ZonePowerWatt = 180.0, IsActive = true },
                new() { ZoneId = 6, ZoneName = "Zone 6: Khu Thương Mại & Dịch Vụ", Location = "Tầng 1-2", ImpedanceOhms = 90.0, NormalImpedanceOhms = 90.0, SpeakerCount = 20, ZonePowerWatt = 120.0, IsActive = true },
                new() { ZoneId = 7, ZoneName = "Zone 7: Thang Thoát Hiểm Khẩn Cấp", Location = "Tháp A & B", ImpedanceOhms = 110.0, NormalImpedanceOhms = 110.0, SpeakerCount = 18, ZonePowerWatt = 90.0, IsActive = true },
                new() { ZoneId = 8, ZoneName = "Zone 8: Sân Vườn & Cảnh Quan Ngoài Trời", Location = "Khuôn Viên Ngoài Trời", ImpedanceOhms = 75.0, NormalImpedanceOhms = 75.0, SpeakerCount = 14, ZonePowerWatt = 140.0, IsActive = true }
            }
        });

        // 3. CCTV
        AddDevice(new CctvDevice
        {
            Id = "CAM-S101",
            Name = "Camera Sảnh Chính Tầng 1",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.21",
            Port = 80,
            Location = "Sảnh Đón Tiếp Tầng 1",
            Manufacturer = "Hikvision",
            Model = "DS-2CD2143G2-I",
            RtspStreamUrl = "rtsp://192.168.30.21:554/live/ch0",
            Resolution = "2560x1440 (2K)",
            Fps = 30,
            BitrateKbps = 4096,
            IsVideoLoss = false,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 3.2,
            FloorPlanId = "FLOOR_1",
            FloorPlanName = "Tầng 1 - Sảnh Chính & Cổng Ra Vào",
            PositionX = 30.0,
            PositionY = 45.0,
            CameraFormFactor = "Dome Bán Cầu",
            PanAngle = 10.0,
            TiltAngle = -15.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-PTZ-102",
            Name = "Camera PTZ Quay Quét Sảnh Lễ Tân",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.26",
            Port = 80,
            Location = "Quầy Lễ Tân & Cổng Kiểm Soát",
            Manufacturer = "Hikvision",
            Model = "DS-2DE4425IW-DE (PTZ)",
            RtspStreamUrl = "rtsp://192.168.30.26:554/live/ch0",
            Resolution = "3840x2160 (4K UHD)",
            Fps = 30,
            BitrateKbps = 6144,
            IsVideoLoss = false,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 2.8,
            FloorPlanId = "FLOOR_1",
            FloorPlanName = "Tầng 1 - Sảnh Chính & Cổng Ra Vào",
            PositionX = 70.0,
            PositionY = 50.0,
            CameraFormFactor = "PTZ Speed Dome 360",
            PanAngle = 35.0,
            TiltAngle = -20.0,
            ZoomLevel = 2.5,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-B101",
            Name = "Camera Hầm Xe B1 (Làn Vào)",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.22",
            Port = 80,
            Location = "Cổng Hầm B1 Phía Đông",
            Manufacturer = "Dahua",
            Model = "DH-IPC-HFW2439S-SA",
            RtspStreamUrl = "rtsp://192.168.30.22:554/live/ch0",
            Resolution = "1920x1080 (FHD)",
            Fps = 25,
            BitrateKbps = 3072,
            IsVideoLoss = false,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 4.1,
            FloorPlanId = "FLOOR_B1",
            FloorPlanName = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật",
            PositionX = 25.0,
            PositionY = 65.0,
            CameraFormFactor = "Bullet Thân Trụ",
            PanAngle = -30.0,
            TiltAngle = -10.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-B102",
            Name = "Camera Hầm Xe B1 (Làn Ra & Chốt Trực)",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.27",
            Port = 80,
            Location = "Chốt Kiểm Soát Vé Hầm B1",
            Manufacturer = "Dahua",
            Model = "DH-IPC-HFW2439S-SA",
            RtspStreamUrl = "rtsp://192.168.30.27:554/live/ch0",
            Resolution = "1920x1080 (FHD)",
            Fps = 25,
            BitrateKbps = 3072,
            IsVideoLoss = false,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 3.9,
            FloorPlanId = "FLOOR_B1",
            FloorPlanName = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật",
            PositionX = 75.0,
            PositionY = 30.0,
            CameraFormFactor = "Bullet Thân Trụ",
            PanAngle = 15.0,
            TiltAngle = -12.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-P301",
            Name = "Camera Bãi Đỗ Xe Ngoài Trời",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.23",
            Port = 80,
            Location = "Bãi Đỗ Xe Khu Tây",
            Manufacturer = "Hikvision",
            Model = "DS-2CD2T87G2P-LSU",
            RtspStreamUrl = "rtsp://192.168.30.23:554/live/ch0",
            Resolution = "3840x2160 (4K)",
            Fps = 25,
            BitrateKbps = 8192,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 5.2,
            FloorPlanId = "FLOOR_OUTDOOR",
            FloorPlanName = "Khuôn Viên Ngoài Trời & Cổng Bảo Vệ",
            PositionX = 50.0,
            PositionY = 40.0,
            CameraFormFactor = "Bullet 4K Thân Trụ",
            PanAngle = 0.0,
            TiltAngle = -15.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-T501",
            Name = "Camera Hành Lang Tầng 5",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.24",
            Port = 80,
            Location = "Hành Lang Văn Phòng Tầng 5",
            Manufacturer = "Dahua",
            Model = "IPC-HDW2431T-AS",
            RtspStreamUrl = "rtsp://192.168.30.24:554/live/ch0",
            Resolution = "2688x1520",
            Fps = 30,
            BitrateKbps = 4096,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 3.6,
            FloorPlanId = "FLOOR_2",
            FloorPlanName = "Tầng 2 & 5 - Khối Văn Phòng",
            PositionX = 25.0,
            PositionY = 45.0,
            CameraFormFactor = "Dome Bán Cầu",
            PanAngle = -20.0,
            TiltAngle = -10.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "CAM-S202",
            Name = "Camera Sảnh Thang Máy Tầng 2",
            DeviceType = CctvDeviceType.IpCamera,
            IpAddress = "192.168.30.25",
            Port = 80,
            Location = "Sảnh Thang Máy Tầng 2",
            Manufacturer = "Axis",
            Model = "M3065-V",
            RtspStreamUrl = "rtsp://192.168.30.25:554/live/ch0",
            Resolution = "1920x1080",
            Fps = 30,
            BitrateKbps = 2048,
            HeartbeatCycleSeconds = 5,
            HeartbeatLatencyMs = 3.1,
            FloorPlanId = "FLOOR_2",
            FloorPlanName = "Tầng 2 & 5 - Khối Văn Phòng",
            PositionX = 75.0,
            PositionY = 55.0,
            CameraFormFactor = "Dome Bán Cầu",
            PanAngle = 10.0,
            TiltAngle = -15.0,
            ZoomLevel = 1.0,
            HasPtzCapability = true
        });

        AddDevice(new CctvDevice
        {
            Id = "NVR-SRV-01",
            Name = "Đầu Ghi Hình NVR 64 Kênh",
            DeviceType = CctvDeviceType.NvrRecorder,
            IpAddress = "192.168.30.10",
            Port = 8000,
            Location = "Phòng Máy Chủ Server Room",
            Manufacturer = "Hikvision",
            Model = "DS-9664NI-I8",
            NvrStorage = new NvrStorageInfo
            {
                StorageTotalGb = 16000.0,
                StorageUsedGb = 9800.0,
                HasBadSector = false,
                BadSectorCount = 0,
                RaidStatus = RaidHealthStatus.Healthy,
                RaidType = "RAID 5 (4x 4TB Enterprise)",
                TotalChannels = 64,
                RecordingChannels = 48,
                PhysicalDisks = new List<NvrDiskDriveInfo>
                {
                    new() { SlotNumber = 1, Model = "WD Purple Pro 4TB", SerialNumber = "WD-WCC7K10238", CapacityGb = 4000.0, TemperatureCelsius = 37.5, SmartHealth = "GOOD (PASSED)", IsFaulty = false },
                    new() { SlotNumber = 2, Model = "WD Purple Pro 4TB", SerialNumber = "WD-WCC7K10239", CapacityGb = 4000.0, TemperatureCelsius = 38.0, SmartHealth = "GOOD (PASSED)", IsFaulty = false },
                    new() { SlotNumber = 3, Model = "WD Purple Pro 4TB", SerialNumber = "WD-WCC7K10240", CapacityGb = 4000.0, TemperatureCelsius = 39.2, SmartHealth = "GOOD (PASSED)", IsFaulty = false },
                    new() { SlotNumber = 4, Model = "WD Purple Pro 4TB", SerialNumber = "WD-WCC7K10241", CapacityGb = 4000.0, TemperatureCelsius = 37.8, SmartHealth = "GOOD (PASSED)", IsFaulty = false }
                }
            }
        });

        // 4. Access Control
        AddDevice(new AccessControlDevice
        {
            Id = "AC-CTRL-01",
            Name = "Bộ Điều Khiển Cửa Trung Tâm Tầng 1 & Thang Máy",
            IpAddress = "192.168.40.10",
            Port = 4370,
            Location = "Tủ Kỹ Thuật Tầng 1",
            Manufacturer = "ZKTeco",
            Model = "inBio-460 Pro",
            IsControllerOnline = true,
            IsMainPowerOk = true,
            ControllerVoltageVolt = 12.4,
            BatteryLevelPercent = 94.0,
            DoorPoints = new List<DoorPointInfo>
            {
                new()
                {
                    DoorId = "D-LOBBY-01",
                    DoorName = "Cổng Cửa Trượt Tự Động Sảnh Chính",
                    Location = "Sảnh Đón Tiếp Tầng 1",
                    FloorPlanId = "FLOOR_1",
                    FloorPlanName = "Tầng 1 - Sảnh Chính & Cổng Ra Vào",
                    PositionX = 110.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-88341",
                    LastUserName = "Nguyễn Văn Tuấn (Khách VIP)",
                    LastSwipeTime = "10:32:15",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-SERVER-01",
                    DoorName = "Cửa Thẻ Từ Phòng Server Room",
                    Location = "Phòng Máy Chủ Server Room",
                    FloorPlanId = "FLOOR_1",
                    FloorPlanName = "Tầng 1 - Sảnh Chính & Cổng Ra Vào",
                    PositionX = 310.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-00109",
                    LastUserName = "Lê Hoàng Yến (Admin Hạ Tầng)",
                    LastSwipeTime = "09:50:40",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-EXIT-01",
                    DoorName = "Cửa Thoát Hiểm Khẩn Cấp Tầng 1",
                    Location = "Hành Lang Cánh Đông",
                    FloorPlanId = "FLOOR_1",
                    FloorPlanName = "Tầng 1 - Sảnh Chính & Cổng Ra Vào",
                    PositionX = 210.0,
                    PositionY = 150.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-77312",
                    LastUserName = "Trần Đình Phúc (Bảo Vệ)",
                    LastSwipeTime = "08:15:22",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-B1-PARK",
                    DoorName = "Cổng Kiểm Soát Vào Hầm B1",
                    Location = "Ram Dốc Hầm B1",
                    FloorPlanId = "FLOOR_B1",
                    FloorPlanName = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật",
                    PositionX = 110.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-55210",
                    LastUserName = "Phạm Quang Minh (Ban QL)",
                    LastSwipeTime = "10:14:05",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-B1-ELEC",
                    DoorName = "Cửa Phòng Trạm Điện Hầm B1",
                    Location = "Phòng Trạm Biến Áp Hầm B1",
                    FloorPlanId = "FLOOR_B1",
                    FloorPlanName = "Tầng Hầm B1 - Bãi Xe & Kỹ Thuật",
                    PositionX = 310.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-33419",
                    LastUserName = "Đỗ Duy Mạnh (Kỹ Sư Điện)",
                    LastSwipeTime = "07:45:10",
                    LastAccessGranted = true
                }
            },
            Elevators = new List<ElevatorInfo>
            {
                new()
                {
                    ElevatorId = "ELEV-01",
                    ElevatorName = "Thang Khách Tháp A (Cabin 1)",
                    CurrentFloor = 1,
                    CurrentFloorDisplay = "Tầng 1 (Sảnh)",
                    TargetFloor = 5,
                    Direction = "UP",
                    DoorState = "CLOSED",
                    SpeedMps = 1.75,
                    LoadKg = 320.0,
                    RatedLoadKg = 1000.0,
                    IsEmergencyStop = false,
                    IsFireServiceMode = false,
                    IsAccessCardRequired = true,
                    IsConnected = true,
                    OperationalStatus = "Đang vận hành bình thường"
                },
                new()
                {
                    ElevatorId = "ELEV-02",
                    ElevatorName = "Thang Khách Tháp B (Cabin 2)",
                    CurrentFloor = 4,
                    CurrentFloorDisplay = "Tầng 4",
                    TargetFloor = 4,
                    Direction = "IDLE",
                    DoorState = "OPEN",
                    SpeedMps = 0.0,
                    LoadKg = 160.0,
                    RatedLoadKg = 1000.0,
                    IsEmergencyStop = false,
                    IsFireServiceMode = false,
                    IsAccessCardRequired = true,
                    IsConnected = true,
                    OperationalStatus = "Đang đón khách"
                },
                new()
                {
                    ElevatorId = "ELEV-03",
                    ElevatorName = "Thang Hàng & PCCC Chữa Cháy (Cabin 3)",
                    CurrentFloor = -1,
                    CurrentFloorDisplay = "Tầng Hầm B1",
                    TargetFloor = -1,
                    Direction = "IDLE",
                    DoorState = "CLOSED",
                    SpeedMps = 0.0,
                    LoadKg = 450.0,
                    RatedLoadKg = 1600.0,
                    IsEmergencyStop = false,
                    IsFireServiceMode = false,
                    IsAccessCardRequired = false,
                    IsConnected = true,
                    OperationalStatus = "Sẵn sàng hoạt động"
                },
                new()
                {
                    ElevatorId = "ELEV-04",
                    ElevatorName = "Thang VIP Chuyên Dụng (Cabin 4)",
                    CurrentFloor = 8,
                    CurrentFloorDisplay = "Tầng 8 (VIP)",
                    TargetFloor = 1,
                    Direction = "DOWN",
                    DoorState = "CLOSED",
                    SpeedMps = 2.0,
                    LoadKg = 180.0,
                    RatedLoadKg = 800.0,
                    IsEmergencyStop = false,
                    IsFireServiceMode = false,
                    IsAccessCardRequired = true,
                    IsConnected = true,
                    OperationalStatus = "Kiểm soát thẻ phân tầng"
                }
            }
        });

        AddDevice(new AccessControlDevice
        {
            Id = "AC-CTRL-02",
            Name = "Bộ Điều Khiển Cửa Tầng 2 & Khối Kỹ Thuật",
            IpAddress = "192.168.40.11",
            Port = 4370,
            Location = "Tủ Kỹ Thuật Tầng 2",
            Manufacturer = "ZKTeco",
            Model = "inBio-260 Pro",
            IsControllerOnline = true,
            IsMainPowerOk = true,
            ControllerVoltageVolt = 12.2,
            BatteryLevelPercent = 91.0,
            DoorPoints = new List<DoorPointInfo>
            {
                new()
                {
                    DoorId = "D-T2-01",
                    DoorName = "Cửa Văn Phòng Điều Hành Tầng 2",
                    Location = "Tầng 2 - Khối Điều Hành",
                    FloorPlanId = "FLOOR_2",
                    FloorPlanName = "Tầng 2 - Khối Văn Phòng",
                    PositionX = 110.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-66230",
                    LastUserName = "Ngô Trọng Hiếu (Giám Đốc)",
                    LastSwipeTime = "10:05:44",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-T2-CONF",
                    DoorName = "Cửa Phòng Họp Hội Đồng Quản Trị",
                    Location = "Tầng 2 - Phòng Họp Lớn",
                    FloorPlanId = "FLOOR_2",
                    FloorPlanName = "Tầng 2 - Khối Văn Phòng",
                    PositionX = 310.0,
                    PositionY = 65.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-11984",
                    LastUserName = "Vũ Bích Ngọc (Thư Ký)",
                    LastSwipeTime = "09:30:18",
                    LastAccessGranted = true
                },
                new()
                {
                    DoorId = "D-T2-EXIT",
                    DoorName = "Cửa Thoát Hiểm Khẩn Cấp Tầng 2",
                    Location = "Thang Bộ Cánh Tây",
                    FloorPlanId = "FLOOR_2",
                    FloorPlanName = "Tầng 2 - Khối Văn Phòng",
                    PositionX = 210.0,
                    PositionY = 150.0,
                    IsLocked = true,
                    IsOpen = false,
                    IsConnected = true,
                    ReaderStatus = "ONLINE",
                    LastCardNumber = "CARD-77312",
                    LastUserName = "Trần Đình Phúc (Bảo Vệ)",
                    LastSwipeTime = "08:16:00",
                    LastAccessGranted = true
                }
            }
        });

        // 5. BMS
        AddDevice(new BmsDevice
        {
            Id = "BMS-CHILLER-01",
            Name = "Hệ Thống Điều Hòa Trung Tâm Chiller 01 & AHU/FCU",
            Category = BmsCategory.ChillerHvac,
            IpAddress = "192.168.50.20",
            Port = 502,
            Location = "Tầng Kỹ Thuật M&E (Mái)",
            Manufacturer = "Daikin",
            Model = "Water-Cooled Centrifugal Chiller (450 RT)",
            Chiller = new ChillerMetrics
            {
                SupplyWaterTempCelsius = 7.1,
                ReturnWaterTempCelsius = 12.3,
                SystemPressureBar = 3.6,
                MaxSafeTempCelsius = 14.0,
                MaxSafePressureBar = 5.5,
                CoolingCapacityRt = 450.0,
                CoolingPowerKw = 1580.0,
                WaterFlowRateM3h = 275.0,
                CompressorCount = 2,
                CompressorRunningCount = 2,
                CopEfficiency = 5.8,
                HvacUnits = new List<AhuFcuUnitInfo>
                {
                    new() { UnitId = "AHU-S101", UnitName = "AHU-01: Sảnh Đón Tiếp & Lễ Tân", UnitType = "AHU", Location = "Tầng 1", SupplyTempCelsius = 17.5, ReturnTempCelsius = 23.5, SetTempCelsius = 22.0, AirFlowCfm = 8500.0, FanSpeedPercent = 80, IsFilterClogged = false, Status = "HOẠT ĐỘNG" },
                    new() { UnitId = "AHU-HT201", UnitName = "AHU-02: Hội Trường & Phòng Hội Thảo", UnitType = "AHU", Location = "Tầng 6", SupplyTempCelsius = 18.0, ReturnTempCelsius = 24.0, SetTempCelsius = 22.0, AirFlowCfm = 12000.0, FanSpeedPercent = 85, IsFilterClogged = false, Status = "HOẠT ĐỘNG" },
                    new() { UnitId = "FCU-T301", UnitName = "FCU-01: Phòng Họp Ban Giám Đốc", UnitType = "FCU", Location = "Tầng 3", SupplyTempCelsius = 19.2, ReturnTempCelsius = 24.5, SetTempCelsius = 23.0, AirFlowCfm = 1200.0, FanSpeedPercent = 65, IsFilterClogged = false, Status = "HOẠT ĐỘNG" },
                    new() { UnitId = "FCU-T302", UnitName = "FCU-02: Khối Văn Phòng Làm Việc Mở", UnitType = "FCU", Location = "Tầng 3", SupplyTempCelsius = 18.8, ReturnTempCelsius = 24.2, SetTempCelsius = 22.5, AirFlowCfm = 2400.0, FanSpeedPercent = 70, IsFilterClogged = false, Status = "HOẠT ĐỘNG" }
                }
            }
        });

        AddDevice(new BmsDevice
        {
            Id = "BMS-PUMP-STATION",
            Name = "Cụm Trạm Bơm Cấp Nước Sinh Hoạt & PCCC",
            Category = BmsCategory.PumpSystem,
            IpAddress = "192.168.50.30",
            Port = 502,
            Location = "Phòng Bơm Hầm B2",
            Manufacturer = "Grundfos & Ebara",
            Model = "Hydro MPC Booster & Fire Pump System",
            Pumps = new List<PumpMetrics>
            {
                new() { PumpId = "PUMP-SH-01", PumpName = "Máy Bơm Nước Sinh Hoạt 1 (Chính)", PumpType = "Bơm Sinh Hoạt", IsRunning = true, IsThermalOverloadTripped = false, FlowRateM3h = 52.0, DischargePressureBar = 4.5, MotorCurrentAmp = 18.4, RatedCurrentAmp = 25.0, BearingTempCelsius = 46.5, VibrationMmS = 1.2, Mode = "AUTO" },
                new() { PumpId = "PUMP-SH-02", PumpName = "Máy Bơm Nước Sinh Hoạt 2 (Dự Phòng)", PumpType = "Bơm Sinh Hoạt", IsRunning = false, IsThermalOverloadTripped = false, FlowRateM3h = 0.0, DischargePressureBar = 4.5, MotorCurrentAmp = 0.0, RatedCurrentAmp = 25.0, BearingTempCelsius = 28.0, VibrationMmS = 0.0, Mode = "STANDBY" },
                new() { PumpId = "PUMP-BOOST-01", PumpName = "Cụm Bơm Tăng Áp Tầng Mái", PumpType = "Bơm Tăng Áp", IsRunning = true, IsThermalOverloadTripped = false, FlowRateM3h = 24.0, DischargePressureBar = 3.8, MotorCurrentAmp = 8.6, RatedCurrentAmp = 12.0, BearingTempCelsius = 42.0, VibrationMmS = 0.9, Mode = "AUTO" },
                new() { PumpId = "PUMP-CH-01", PumpName = "Máy Bơm Cứu Hỏa Động Cơ Điện PCCC", PumpType = "Bơm PCCC", IsRunning = false, IsThermalOverloadTripped = false, FlowRateM3h = 0.0, DischargePressureBar = 8.5, MotorCurrentAmp = 0.0, RatedCurrentAmp = 110.0, BearingTempCelsius = 30.0, VibrationMmS = 0.0, Mode = "STANDBY" },
                new() { PumpId = "PUMP-CH-02", PumpName = "Máy Bơm Cứu Hỏa Diesel PCCC", PumpType = "Bơm PCCC Diesel", IsRunning = false, IsThermalOverloadTripped = false, FlowRateM3h = 0.0, DischargePressureBar = 8.5, MotorCurrentAmp = 0.0, RatedCurrentAmp = 125.0, BearingTempCelsius = 31.0, VibrationMmS = 0.0, Mode = "STANDBY" }
            }
        });

        AddDevice(new BmsDevice
        {
            Id = "BMS-ELEC-GRID",
            Name = "Trạm Biến Áp Lưới Điện & Máy Phát Dự Phòng",
            Category = BmsCategory.ElectricalGrid,
            IpAddress = "192.168.50.40",
            Port = 502,
            Location = "Trạm Điện Tổng Tòa Nhà & Phòng Máy Phát Hầm B1",
            Manufacturer = "Schneider Electric & Cummins",
            Model = "PowerLogic PM8000 & Cummins QST30-G4",
            PowerGrid = new PowerGridMetrics
            {
                SourceType = "Trạm biến áp lưới điện 22kV/0.4kV",
                VoltagePhaseA = 221.2,
                VoltagePhaseB = 220.8,
                VoltagePhaseC = 221.5,
                CurrentPhaseA = 148.0,
                CurrentPhaseB = 145.2,
                CurrentPhaseC = 149.0,
                ActivePowerKw = 98.4,
                ReactivePowerKvar = 28.5,
                PowerFactor = 0.95,
                FrequencyHz = 50.0,
                TotalEnergyKwh = 158420.0
            },
            Generator = new GeneratorMetrics
            {
                GeneratorId = "GEN-01",
                GeneratorName = "Máy Phát Điện Dự Phòng Cummins 1000kVA",
                GeneratorStatus = "SẴN SÀNG (STANDBY)",
                IsRunning = false,
                FuelLevelPercent = 85.0,
                BatteryVoltageVolt = 26.5,
                CoolantTempCelsius = 76.0,
                OutputVoltageVolt = 380.0,
                FrequencyHz = 50.0,
                TotalRunHours = 148.5,
                AtsStatus = "LƯỚI ĐIỆN CHÍNH (GRID ACTIVE)"
            }
        });

        AddDevice(new BmsDevice
        {
            Id = "BMS-HAZARD-B2",
            Name = "Cảm Biến Bể Ngầm & Khí Gas/CO Hầm Xe",
            Category = BmsCategory.EnvironmentalHazard,
            IpAddress = "192.168.50.50",
            Port = 502,
            Location = "Khu Vực Bể Ngầm & Bãi Xe Hầm B1-B3",
            Manufacturer = "Honeywell",
            Model = "Analytics Sensepoint XCD",
            Hazard = new HazardSensorMetrics
            {
                LocationName = "Hầm B2 - Bể Nước Ngầm & Bãi Đỗ Xe",
                SumpPitWaterLevelPercent = 82.0,
                WaterLevelMeters = 2.87,
                SumpPitCapacityM3 = 150.0,
                GasCoConcentrationPpm = 11.5,
                GasLpgConcentrationLelPercent = 0.0,
                IsExhaustFanRunning = false
            }
        });

        AddDevice(new BmsDevice
        {
            Id = "BMS-AHU-01",
            Name = "Hệ Thống Xử Lý Không Khí AHU Sảnh Tầng 1",
            Category = BmsCategory.ChillerHvac,
            IpAddress = "192.168.50.25",
            Port = 502,
            Location = "Phòng Kỹ Thuật AHU Tầng 1",
            Manufacturer = "Trane",
            Model = "Climate Changer",
            Chiller = new ChillerMetrics
            {
                SupplyWaterTempCelsius = 8.0,
                ReturnWaterTempCelsius = 13.0,
                SystemPressureBar = 3.2
            }
        });

        // 6. Fire Alarm (PCCC - Read-Only)
        AddDevice(new FireAlarmDevice
        {
            Id = "FAS-REPEATER-01",
            Name = "Tủ Hiển Thị Phụ Báo Cháy (Sảnh Bảo Vệ)",
            IpAddress = "192.168.60.11",
            Port = 502,
            Location = "Chốt An Ninh Sảnh Chính Tầng 1",
            Manufacturer = "Hochiki",
            Model = "FN-LCD-S Network Annunciator",
            IsAcPowerOk = true,
            IsBatteryBackupActive = false,
            BatteryVoltageVolt = 27.0,
            IsReadOnlyOperatingMode = true
        });

        AddDevice(new FireAlarmDevice
        {
            Id = "FAS-PANEL-01",
            Name = "Tủ Trung Tâm Báo Cháy Tự Động PCCC",
            IpAddress = "192.168.60.10",
            Port = 502,
            Location = "Phòng Trực Điều Hành PCCC (Tầng 1)",
            Manufacturer = "Hochiki",
            Model = "FireNET Plus Analog Addressable",
            IsAcPowerOk = true,
            IsBatteryBackupActive = false,
            BatteryVoltageVolt = 26.8,
            IsReadOnlyOperatingMode = true,
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    LoopName = "Vòng Loop 1: Tầng Hầm & Tầng 1-3",
                    IsLoopOpenFault = false,
                    TotalSensors = 32,
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new() { PointAddress = "L1-S01", SensorType = FireAlarmSensorType.SmokeDetector, DeviceName = "Đầu Báo Khói Quang Điện Sảnh", Location = "Sảnh Đón Tiếp Tầng 1", FloorPlanId = "FLOOR_1", PositionX = 110.0, PositionY = 65.0, IsAlarmTriggered = false, AnalogValue = 1.1, AnalogUnit = "%/ft" },
                        new() { PointAddress = "L1-S02", SensorType = FireAlarmSensorType.SmokeDetector, DeviceName = "Đầu Báo Khói Phòng Server", Location = "Phòng Máy Chủ Server Room", FloorPlanId = "FLOOR_1", PositionX = 310.0, PositionY = 65.0, IsAlarmTriggered = false, AnalogValue = 1.2, AnalogUnit = "%/ft" },
                        new() { PointAddress = "L1-H01", SensorType = FireAlarmSensorType.HeatDetector, DeviceName = "Đầu Báo Nhiệt Trạm Biến Áp", Location = "Trạm Biến Áp Hầm B1", FloorPlanId = "FLOOR_B1", PositionX = 110.0, PositionY = 65.0, IsAlarmTriggered = false, AnalogValue = 28.5, AnalogUnit = "°C" },
                        new() { PointAddress = "L1-H02", SensorType = FireAlarmSensorType.HeatDetector, DeviceName = "Đầu Báo Nhiệt Bếp Căn Tin", Location = "Phòng Bếp Nhà Ăn Tầng 4", FloorPlanId = "FLOOR_2", PositionX = 110.0, PositionY = 65.0, IsAlarmTriggered = false, AnalogValue = 32.0, AnalogUnit = "°C" },
                        new() { PointAddress = "L1-M01", SensorType = FireAlarmSensorType.ManualCallPoint, DeviceName = "Nút Ấn Báo Cháy Khẩn Cấp Sảnh", Location = "Cửa Ra Vào Sảnh Tầng 1", FloorPlanId = "FLOOR_1", PositionX = 210.0, PositionY = 150.0, IsAlarmTriggered = false },
                        new() { PointAddress = "L1-M02", SensorType = FireAlarmSensorType.ManualCallPoint, DeviceName = "Nút Ấn Báo Cháy Khẩn Cấp Hầm", Location = "Cửa Thoát Hiểm Hầm B1", FloorPlanId = "FLOOR_B1", PositionX = 210.0, PositionY = 150.0, IsAlarmTriggered = false }
                    }
                },
                new()
                {
                    LoopNumber = 2,
                    LoopName = "Vòng Loop 2: Khối Văn Phòng Tầng 4-15",
                    IsLoopOpenFault = false,
                    TotalSensors = 45,
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new() { PointAddress = "L2-S01", SensorType = FireAlarmSensorType.SmokeDetector, Location = "Hành Lang Tầng 5", IsAlarmTriggered = false, AnalogValue = 1.0 },
                        new() { PointAddress = "L2-M02", SensorType = FireAlarmSensorType.ManualCallPoint, Location = "Cầu Thang Tầng 8", IsAlarmTriggered = false }
                    }
                }
            }
        });
    }

    private void AddDevice(SubsystemDevice device)
    {
        _devices[device.Id] = device;
    }
}
