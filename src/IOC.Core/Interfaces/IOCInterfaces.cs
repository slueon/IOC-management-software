using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Models;

namespace IOC.Core.Interfaces
{
    public interface IAlarmService
    {
        event EventHandler<AlarmEvent>? AlarmTriggered;
        event EventHandler<AlarmEvent>? AlarmUpdated;

        bool IsAudioMuted { get; set; }
        AlarmEvent? TopActiveAlarm { get; }
        IReadOnlyList<AlarmEvent> GetActiveAlarms();
        
        Task RaiseAlarmAsync(AlarmEvent alarm);
        Task AcknowledgeAlarmAsync(string alarmId, string acknowledgedBy);
        Task ResolveAlarmAsync(string alarmId);
        void ToggleMute();
    }

    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task SaveAlarmAsync(AlarmEvent alarm);
        Task UpdateAlarmAsync(AlarmEvent alarm);
        Task<List<AlarmEvent>> GetRecentAlarmsAsync(int limit = 100);
        Task<List<Device>> GetAllDevicesAsync();
        Task<Device?> GetDeviceByIdAsync(string id);
        Task SaveDeviceAsync(Device device);
        Task DeleteDeviceAsync(string id);
        Task UpdateDeviceStatusAsync(string deviceId, DeviceStatus status, string lastError);
        Task UpdateDeviceTelemetryAsync(string deviceId, DeviceStatus status, double latencyMs, string telemetry, string lastError);
        Task<List<Device>> SearchDevicesAsync(string? keyword, SubSystemType? subSystem, DeviceStatus? status);
    }

    public interface IDeviceMonitorService
    {
        event EventHandler<Device>? DeviceStatusChanged;
        IReadOnlyList<Device> Devices { get; }
        
        void StartMonitoring(int intervalSeconds = 10);
        void StopMonitoring();
        void RegisterDevice(Device device);
        void UnregisterDevice(string deviceId);
        Task PingCheckDeviceAsync(Device device);
        Task TriggerManualScanAsync();
    }

    public interface IDeviceImportExportService
    {
        string ExportToCsv(IEnumerable<Device> devices);
        Task ExportToFileAsync(IEnumerable<Device> devices, string filePath);
        List<Device> ImportFromCsv(string csvContent, out List<string> errors);
        Task<(List<Device> Devices, List<string> Errors)> ImportFromFileAsync(string filePath);
    }
}
