using System.Net.Http;
using IOC.Core.Contracts.Adapters;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;

namespace IOC.Infrastructure.Adapters;

public class PingDeviceAdapter : BaseDeviceAdapter<LanTelDevice, PingTelemetry>, IPingAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.Ping;

    public PingDeviceAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<PingTelemetry> ExecutePollAsync(LanTelDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (device.Status == Core.Enums.DeviceStatus.Offline || device.Status == Core.Enums.DeviceStatus.Critical)
        {
            throw new HttpRequestException($"Không thể gửi gói tin ICMP tới IP {device.IpAddress}: Host Unreachable");
        }

        var telemetry = new PingTelemetry
        {
            TargetHost = device.IpAddress,
            RoundtripTimeMs = Math.Max(1, (int)device.BandwidthUtilizationPercent),
            PacketLossPercent = device.IsCongested ? 15 : 0,
            IsReachable = true,
            CheckedAt = DateTime.UtcNow
        };

        return Task.FromResult(telemetry);
    }
}

public class SnmpDeviceAdapter : BaseDeviceAdapter<LanTelDevice, SnmpTelemetry>, ISnmpAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.Snmp;

    public SnmpDeviceAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<SnmpTelemetry> ExecutePollAsync(LanTelDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (device.Status == Core.Enums.DeviceStatus.Offline)
        {
            throw new TimeoutException($"SNMP Timeout (OID 1.3.6.1.2.1.1) đối với thiết bị {device.IpAddress}");
        }

        var telemetry = new SnmpTelemetry
        {
            DeviceOid = "1.3.6.1.4.1.9.1.516",
            CpuLoadPercent = (int)device.CpuUsagePercent,
            MemoryUsedPercent = (int)device.MemoryUsagePercent,
            ActivePortsUp = device.UpPortsCount,
            TotalPorts = device.TotalPortsCount > 0 ? device.TotalPortsCount : 24,
            InOctetsTotal = 104857600L,
            OutOctetsTotal = 83886080L
        };

        return Task.FromResult(telemetry);
    }
}

public class ModbusDeviceAdapter : BaseDeviceAdapter<BmsDevice, BmsTelemetryData>, IModbusAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.Modbus;

    public ModbusDeviceAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<BmsTelemetryData> ExecutePollAsync(BmsDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (device.Status == Core.Enums.DeviceStatus.Offline)
        {
            throw new InvalidOperationException($"Lỗi kết nối Modbus TCP (Slave ID: 1, Port 502) tại {device.IpAddress}");
        }

        var telemetry = new BmsTelemetryData
        {
            DeviceId = device.Id,
            SupplyWaterTempCelsius = device.Chiller?.SupplyWaterTempCelsius ?? 7.2,
            ReturnWaterTempCelsius = device.Chiller?.ReturnWaterTempCelsius ?? 12.1,
            SystemPressureBar = device.Chiller?.SystemPressureBar ?? 3.6,
            ActivePumpsCount = device.Pumps.Count(p => p.IsRunning),
            IsGeneratorRunning = device.Generator?.IsRunning ?? false,
            HasHazardAlarm = device.Hazard?.IsGasLeakDetected ?? false
        };

        return Task.FromResult(telemetry);
    }
}

public class CameraNvrAdapter : BaseDeviceAdapter<CctvDevice, CameraStreamTelemetry>, ICameraNvrAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.CameraRtsp;

    public CameraNvrAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<CameraStreamTelemetry> ExecutePollAsync(CctvDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (device.IsVideoLoss || device.Status == Core.Enums.DeviceStatus.Offline)
        {
            throw new HttpRequestException($"RTSP Stream Error: Không nhận được luồng video từ {device.RtspStreamUrl}");
        }

        var telemetry = new CameraStreamTelemetry
        {
            CameraId = device.Id,
            RtspUri = device.RtspStreamUrl,
            CurrentFps = device.Fps,
            BitrateKbps = device.BitrateKbps,
            IsSignalPresent = !device.IsVideoLoss,
            IsStorageHealthy = device.Status != Core.Enums.DeviceStatus.Critical
        };

        return Task.FromResult(telemetry);
    }
}

public class FireAlarmPanelAdapter : BaseDeviceAdapter<FireAlarmDevice, FireAlarmPanelTelemetry>, IFireAlarmPanelAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.FireAlarmLoop;

    public FireAlarmPanelAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<FireAlarmPanelTelemetry> ExecutePollAsync(FireAlarmDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (device.Status == Core.Enums.DeviceStatus.Offline)
        {
            throw new TimeoutException($"Mất kết nối giao tiếp Serial RS485/IP với tủ trung tâm báo cháy FACP {device.IpAddress}");
        }

        var telemetry = new FireAlarmPanelTelemetry
        {
            PanelModel = device.PanelModel,
            IsAcPowerOk = device.IsAcPowerOk,
            BatteryVoltageVolt = device.BatteryVoltageVolt,
            ActiveFireAlarmsCount = device.ActiveFireCount,
            LoopFaultsCount = device.Loops.Count(l => l.IsLoopOpenFault || l.IsGroundFault),
            TotalAddressableSensors = device.TotalSensorPointsCount
        };

        return Task.FromResult(telemetry);
    }
}

public class AccessControlAdapter : BaseDeviceAdapter<AccessControlDevice, AccessControlTelemetry>, IAccessControlAdapter
{
    public override AdapterProtocol Protocol => AdapterProtocol.AccessWiegand;

    public AccessControlAdapter(AdapterRetryPolicy? policy = null) : base(policy) { }

    protected override Task<AccessControlTelemetry> ExecutePollAsync(AccessControlDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!device.IsControllerOnline || device.Status == Core.Enums.DeviceStatus.Offline)
        {
            throw new HttpRequestException($"Bộ điều khiển cửa Access Control tại {device.IpAddress} không phản hồi gói tin keepalive");
        }

        var telemetry = new AccessControlTelemetry
        {
            ControllerId = device.Id,
            ActiveReaders = device.DoorPoints.Count,
            IsMainPowerOk = device.IsMainPowerOk,
            IsBackupBatteryOk = !device.IsBackupBatteryActive,
            TamperDetected = device.DoorPoints.Any(d => d.IsForcedOpen),
            LockedDoorsCount = device.DoorPoints.Count(d => d.IsLocked)
        };

        return Task.FromResult(telemetry);
    }
}
