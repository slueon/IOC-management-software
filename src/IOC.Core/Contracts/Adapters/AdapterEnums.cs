namespace IOC.Core.Contracts.Adapters;

public enum AdapterProtocol
{
    Ping,
    Snmp,
    Modbus,
    CameraRtsp,
    FireAlarmLoop,
    AccessWiegand
}

public enum DeviceHealthState
{
    Online,
    Degraded,
    Warning,
    Critical,
    Offline,
    Timeout
}
