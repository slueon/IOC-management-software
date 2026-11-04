using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models;

namespace IOC.Infrastructure.Protocols
{
    public class PingMonitorService : IDeviceMonitorService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlarmService _alarmService;
        private readonly ConcurrentDictionary<string, Device> _devices = new();
        private readonly ConcurrentDictionary<string, int> _failureCounts = new();
        private Timer? _monitorTimer;

        public event EventHandler<Device>? DeviceStatusChanged;

        public IReadOnlyList<Device> Devices => _devices.Values.ToList();

        public PingMonitorService(IDatabaseService databaseService, IAlarmService alarmService)
        {
            _databaseService = databaseService;
            _alarmService = alarmService;
        }

        public void RegisterDevice(Device device)
        {
            _devices[device.Id] = device;
        }

        public void StartMonitoring(int intervalSeconds = 10)
        {
            StopMonitoring();
            _monitorTimer = new Timer(async _ => await RunCycleAsync(), null, 1000, intervalSeconds * 1000);
        }

        public void StopMonitoring()
        {
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        public async Task RunCycleAsync()
        {
            var tasks = _devices.Values.Select(PingCheckDeviceAsync);
            await Task.WhenAll(tasks);
        }

        public async Task PingCheckDeviceAsync(Device device)
        {
            bool isSuccess = false;
            string error = string.Empty;

            try
            {
                using var ping = new Ping();
                // 1500 ms timeout
                var reply = await ping.SendPingAsync(device.IpAddress, 1500);
                isSuccess = (reply.Status == IPStatus.Success);
                if (!isSuccess)
                {
                    error = $"Ping thất bại: {reply.Status}";
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                error = $"Lỗi kết nối mạng: {ex.Message}";
            }

            if (isSuccess)
            {
                _failureCounts[device.Id] = 0;
                if (device.Status != DeviceStatus.Online)
                {
                    device.Status = DeviceStatus.Online;
                    device.LastSeen = DateTime.Now;
                    device.LastError = string.Empty;

                    await _databaseService.UpdateDeviceStatusAsync(device.Id, DeviceStatus.Online, string.Empty);
                    DeviceStatusChanged?.Invoke(this, device);

                    // Auto-resolve offline alarm if any
                    // We can check and resolve
                }
            }
            else
            {
                var fails = _failureCounts.AddOrUpdate(device.Id, 1, (_, current) => current + 1);

                // After 2 consecutive failures, mark as Offline and trigger alarm
                if (fails >= 2 && device.Status != DeviceStatus.Offline)
                {
                    device.Status = DeviceStatus.Offline;
                    device.LastError = error;

                    await _databaseService.UpdateDeviceStatusAsync(device.Id, DeviceStatus.Offline, error);
                    DeviceStatusChanged?.Invoke(this, device);

                    // Trigger alarm
                    var severity = device.SubSystem switch
                    {
                        SubSystemType.FireAlarm => AlarmSeverity.Critical,
                        SubSystemType.LanTel when device.Name.Contains("Core") => AlarmSeverity.Critical,
                        SubSystemType.PublicAddress => AlarmSeverity.Major,
                        SubSystemType.BMS => AlarmSeverity.Major,
                        SubSystemType.AccessControl => AlarmSeverity.Major,
                        _ => AlarmSeverity.Minor
                    };

                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        SubSystem = device.SubSystem,
                        Severity = severity,
                        Title = $"Mất kết nối thiết bị: {device.Name}",
                        Description = $"Không thể gửi gói tin ICMP tới IP {device.IpAddress}. {error}",
                        Location = device.Location
                    });
                }
            }
        }
    }
}
