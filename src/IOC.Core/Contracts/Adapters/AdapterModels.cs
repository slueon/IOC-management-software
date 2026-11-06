namespace IOC.Core.Contracts.Adapters;

public class AdapterRetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(2000);
    public TimeSpan BackoffInterval { get; set; } = TimeSpan.FromMilliseconds(300);

    public static AdapterRetryPolicy Default => new();
    public static AdapterRetryPolicy FastFail => new() { MaxRetries = 1, Timeout = TimeSpan.FromMilliseconds(1000) };
    public static AdapterRetryPolicy Resilient => new() { MaxRetries = 5, Timeout = TimeSpan.FromMilliseconds(3000), BackoffInterval = TimeSpan.FromMilliseconds(500) };
}

public class AdapterPollResult<TTelemetry>
{
    public bool IsSuccess { get; set; }
    public int LatencyMs { get; set; }
    public DeviceHealthState HealthState { get; set; }
    public TTelemetry? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptsTaken { get; set; } = 1;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AdapterPollResult<TTelemetry> Success(TTelemetry data, int latencyMs, DeviceHealthState health = DeviceHealthState.Online, int attempts = 1)
    {
        return new AdapterPollResult<TTelemetry>
        {
            IsSuccess = true,
            LatencyMs = latencyMs,
            HealthState = health,
            Data = data,
            AttemptsTaken = attempts,
            Timestamp = DateTime.UtcNow
        };
    }

    public static AdapterPollResult<TTelemetry> Failure(string errorMessage, DeviceHealthState health = DeviceHealthState.Offline, int attempts = 1, int latencyMs = 0)
    {
        return new AdapterPollResult<TTelemetry>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HealthState = health,
            AttemptsTaken = attempts,
            LatencyMs = latencyMs,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class PingTelemetry
{
    public string TargetHost { get; set; } = string.Empty;
    public int RoundtripTimeMs { get; set; }
    public int PacketLossPercent { get; set; }
    public bool IsReachable { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class SnmpTelemetry
{
    public string DeviceOid { get; set; } = string.Empty;
    public int CpuLoadPercent { get; set; }
    public int MemoryUsedPercent { get; set; }
    public int ActivePortsUp { get; set; }
    public int TotalPorts { get; set; }
    public long InOctetsTotal { get; set; }
    public long OutOctetsTotal { get; set; }
}

public class CameraStreamTelemetry
{
    public string CameraId { get; set; } = string.Empty;
    public string RtspUri { get; set; } = string.Empty;
    public int CurrentFps { get; set; }
    public int BitrateKbps { get; set; }
    public bool IsSignalPresent { get; set; }
    public bool IsStorageHealthy { get; set; }
}

public class AccessControlTelemetry
{
    public string ControllerId { get; set; } = string.Empty;
    public int ActiveReaders { get; set; }
    public bool IsMainPowerOk { get; set; }
    public bool IsBackupBatteryOk { get; set; }
    public bool TamperDetected { get; set; }
    public int LockedDoorsCount { get; set; }
}


public class BmsTelemetryData
{
    public string DeviceId { get; set; } = string.Empty;
    public double SupplyWaterTempCelsius { get; set; }
    public double ReturnWaterTempCelsius { get; set; }
    public double SystemPressureBar { get; set; }
    public int ActivePumpsCount { get; set; }
    public bool IsGeneratorRunning { get; set; }
    public bool HasHazardAlarm { get; set; }
}

public class FireAlarmPanelTelemetry
{
    public string PanelModel { get; set; } = string.Empty;
    public bool IsAcPowerOk { get; set; }
    public double BatteryVoltageVolt { get; set; }
    public int ActiveFireAlarmsCount { get; set; }
    public int LoopFaultsCount { get; set; }
    public int TotalAddressableSensors { get; set; }
}
