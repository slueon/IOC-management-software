using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task SaveDeviceAsync(Device device);
        Task UpdateDeviceStatusAsync(string deviceId, Enums.DeviceStatus status, string lastError);
    }

    public interface IDeviceMonitorService
    {
        event EventHandler<Device>? DeviceStatusChanged;
        IReadOnlyList<Device> Devices { get; }
        
        void StartMonitoring(int intervalSeconds = 10);
        void StopMonitoring();
        void RegisterDevice(Device device);
        Task PingCheckDeviceAsync(Device device);
    }
}
