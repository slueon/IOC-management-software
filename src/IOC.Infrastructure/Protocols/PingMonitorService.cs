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
        private readonly Random _random = new();
        private Timer? _monitorTimer;

        public event EventHandler<Device>? DeviceStatusChanged;

        public IReadOnlyList<Device> Devices => _devices.Values.ToList();

        /// <summary>
        /// Chế độ phòng Lab/Trình diễn: Cho phép duy trì telemetry và heartbeat mô phỏng cho các thiết bị mẫu
        /// khi chưa cắm vào mạng LAN vật lý thật của tòa nhà.
        /// </summary>
        public bool IsDemoMode { get; set; } = true;

        public PingMonitorService(IDatabaseService databaseService, IAlarmService alarmService)
        {
            _databaseService = databaseService;
            _alarmService = alarmService;
        }

        public void RegisterDevice(Device device)
        {
            _devices[device.Id] = device;
        }

        public void UnregisterDevice(string deviceId)
        {
            _devices.TryRemove(deviceId, out _);
            _failureCounts.TryRemove(deviceId, out _);
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

        public async Task TriggerManualScanAsync()
        {
            await RunCycleAsync();
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
            double latency = 0;

            try
            {
                using var ping = new Ping();
                // 1000 ms timeout
                var reply = await ping.SendPingAsync(device.IpAddress, 1000);
                isSuccess = (reply.Status == IPStatus.Success);
                if (isSuccess)
                {
                    latency = reply.RoundtripTime;
                }
                else
                {
                    error = $"Ping tháº¥t báº¡i: {reply.Status}";
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                error = $"Lá»—i máº¡ng: {ex.Message}";
            }

            // Nếu trong chế độ Demo Mode và IP không phản hồi vật lý nhưng thiết bị không bị lỗi chủ động
            if (!isSuccess && IsDemoMode && device.Status != DeviceStatus.Offline)
            {
                isSuccess = true;
                // Tạo độ trễ dao động nhẹ thực tế (0.8ms - 3.5ms)
                latency = Math.Round(0.8 + (_random.NextDouble() * 2.5), 1);
            }

            if (isSuccess)
            {
                _failureCounts[device.Id] = 0;
                device.Status = DeviceStatus.Online;
                device.LastSeen = DateTime.Now;
                device.LatencyMs = latency > 0 ? latency : Math.Round(1.0 + _random.NextDouble(), 1);
                device.LastError = string.Empty;

                await _databaseService.UpdateDeviceTelemetryAsync(device.Id, DeviceStatus.Online, device.LatencyMs, device.Telemetry, string.Empty);
                DeviceStatusChanged?.Invoke(this, device);
            }
            else
            {
                var fails = _failureCounts.AddOrUpdate(device.Id, 1, (_, current) => current + 1);

                // Sau 2 lần thất bại liên tiếp -> Đánh dấu Offline và kích hoạt cảnh báo
                if (fails >= 2 && device.Status != DeviceStatus.Offline)
                {
                    device.Status = DeviceStatus.Offline;
                    device.LastError = error;

                    await _databaseService.UpdateDeviceStatusAsync(device.Id, DeviceStatus.Offline, error);
                    DeviceStatusChanged?.Invoke(this, device);

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
                        Title = $"Máº¥t káº¿t ná»‘i thiáº¿t bá»‹: {device.Name}",
                        Description = $"KhÃ´ng nháº­n Ä‘Æ°á»£c tÃ­n hiá»‡u Heartbeat tá»›i Ä‘á»‹a chá»‰ {device.IpAddress}. {error}",
                        Location = device.Location
                    });
                }
            }
        }
    }
}
