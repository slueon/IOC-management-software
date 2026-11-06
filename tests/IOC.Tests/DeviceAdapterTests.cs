using IOC.Core.Contracts.Adapters;
using IOC.Core.Enums;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;
using IOC.Infrastructure.Adapters;
using Xunit;

namespace IOC.Tests;

public class DeviceAdapterTests
{
    [Fact]
    public async Task PingAdapter_OnlineDevice_ReturnsSuccessWithTelemetry()
    {
        var adapter = new PingDeviceAdapter();
        var device = new LanTelDevice
        {
            Id = "SW-TEST",
            IpAddress = "192.168.1.1",
            Status = DeviceStatus.Online,
            BandwidthUtilizationPercent = 25.0
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeviceHealthState.Online, result.HealthState);
        Assert.NotNull(result.Data);
        Assert.Equal("192.168.1.1", result.Data.TargetHost);
        Assert.True(result.Data.IsReachable);
    }

    [Fact]
    public async Task PingAdapter_OfflineDevice_RetriesAndReturnsFailure()
    {
        var policy = new AdapterRetryPolicy { MaxRetries = 2, BackoffInterval = TimeSpan.FromMilliseconds(10) };
        var adapter = new PingDeviceAdapter(policy);
        var device = new LanTelDevice
        {
            Id = "SW-OFFLINE",
            IpAddress = "10.0.0.99",
            Status = DeviceStatus.Offline
        };

        var result = await adapter.PollAsync(device);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeviceHealthState.Offline, result.HealthState);
        Assert.Equal(2, result.AttemptsTaken);
        Assert.Contains("Host Unreachable", result.ErrorMessage);
    }

    [Fact]
    public async Task Adapter_CancellationToken_AbortsImmediately()
    {
        var policy = new AdapterRetryPolicy { MaxRetries = 5, BackoffInterval = TimeSpan.FromMilliseconds(500) };
        var adapter = new PingDeviceAdapter(policy);
        var device = new LanTelDevice
        {
            Id = "SW-CANCEL",
            IpAddress = "10.0.0.1",
            Status = DeviceStatus.Offline
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel right before polling

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adapter.PollAsync(device, cts.Token));
    }

    [Fact]
    public async Task SnmpAdapter_ReachableSwitch_ExtractsCpuAndPorts()
    {
        var adapter = new SnmpDeviceAdapter();
        var device = new LanTelDevice
        {
            Id = "SW-01",
            IpAddress = "192.168.1.1",
            Status = DeviceStatus.Online,
            CpuUsagePercent = 42.0,
            MemoryTotalMb = 1000,
            MemoryUsedMb = 500,
            Ports = new List<SwitchPortInfo>
            {
                new() { PortNumber = 1, IsUp = true },
                new() { PortNumber = 2, IsUp = false }
            }
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(42, result.Data.CpuLoadPercent);
        Assert.Equal(1, result.Data.ActivePortsUp);
        Assert.Equal(2, result.Data.TotalPorts);
    }

    [Fact]
    public async Task ModbusAdapter_ChillerDevice_ExtractsBmsTelemetry()
    {
        var adapter = new ModbusDeviceAdapter();
        var device = new BmsDevice
        {
            Id = "BMS-CHILLER",
            IpAddress = "192.168.1.200",
            Status = DeviceStatus.Online,
            Chiller = new ChillerMetrics
            {
                SupplyWaterTempCelsius = 7.5,
                ReturnWaterTempCelsius = 12.5,
                SystemPressureBar = 4.2
            }
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(7.5, result.Data.SupplyWaterTempCelsius);
        Assert.Equal(12.5, result.Data.ReturnWaterTempCelsius);
        Assert.Equal(4.2, result.Data.SystemPressureBar);
    }

    [Fact]
    public async Task CameraNvrAdapter_ActiveStream_ReturnsHealthyTelemetry()
    {
        var adapter = new CameraNvrAdapter();
        var device = new CctvDevice
        {
            Id = "CAM-01",
            IpAddress = "192.168.1.50",
            RtspStreamUrl = "rtsp://192.168.1.50/live/ch0",
            Fps = 30,
            BitrateKbps = 4096,
            IsVideoLoss = false,
            Status = DeviceStatus.Online
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(30, result.Data.CurrentFps);
        Assert.True(result.Data.IsSignalPresent);
    }

    [Fact]
    public async Task FireAlarmAdapter_ActivePanel_ReturnsLoopMetrics()
    {
        var adapter = new FireAlarmPanelAdapter();
        var device = new FireAlarmDevice
        {
            Id = "FACP-01",
            IpAddress = "192.168.1.250",
            Status = DeviceStatus.Online,
            PanelModel = "Hochiki FireNET Plus",
            IsAcPowerOk = true,
            BatteryVoltageVolt = 26.5,
            Loops = new List<FireLoopInfo>
            {
                new()
                {
                    LoopNumber = 1,
                    Sensors = new List<FireAlarmSensorPoint>
                    {
                        new() { PointAddress = "L1-01", IsAlarmTriggered = true },
                        new() { PointAddress = "L1-02", IsAlarmTriggered = false }
                    }
                }
            }
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.ActiveFireAlarmsCount);
        Assert.Equal(2, result.Data.TotalAddressableSensors);
    }

    [Fact]
    public async Task AccessControlAdapter_Controller_ReturnsDoorLockCount()
    {
        var adapter = new AccessControlAdapter();
        var device = new AccessControlDevice
        {
            Id = "AC-01",
            IpAddress = "192.168.1.100",
            Status = DeviceStatus.Online,
            IsControllerOnline = true,
            IsMainPowerOk = true,
            DoorPoints = new List<DoorPointInfo>
            {
                new() { DoorId = "D1", IsLocked = true, IsForcedOpen = false },
                new() { DoorId = "D2", IsLocked = false, IsForcedOpen = true }
            }
        };

        var result = await adapter.PollAsync(device);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.LockedDoorsCount);
        Assert.True(result.Data.TamperDetected);
    }
}
