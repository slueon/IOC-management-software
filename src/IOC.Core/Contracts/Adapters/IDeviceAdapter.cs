using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;

namespace IOC.Core.Contracts.Adapters;

public interface IDeviceAdapter<in TDevice, TTelemetry>
{
    AdapterProtocol Protocol { get; }
    AdapterRetryPolicy RetryPolicy { get; }
    Task<AdapterPollResult<TTelemetry>> PollAsync(TDevice device, CancellationToken cancellationToken = default);
}

public interface IPingAdapter : IDeviceAdapter<LanTelDevice, PingTelemetry>
{
}

public interface ISnmpAdapter : IDeviceAdapter<LanTelDevice, SnmpTelemetry>
{
}

public interface IModbusAdapter : IDeviceAdapter<BmsDevice, BmsTelemetryData>
{
}

public interface ICameraNvrAdapter : IDeviceAdapter<CctvDevice, CameraStreamTelemetry>
{
}

public interface IFireAlarmPanelAdapter : IDeviceAdapter<FireAlarmDevice, FireAlarmPanelTelemetry>
{
}

public interface IAccessControlAdapter : IDeviceAdapter<AccessControlDevice, AccessControlTelemetry>
{
}
