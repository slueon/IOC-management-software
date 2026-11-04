using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models;

namespace IOC.Infrastructure.Services
{
    public class AlarmManagerService : IAlarmService
    {
        private readonly IDatabaseService _databaseService;
        private readonly SoundAlarmService _soundService;
        private readonly ConcurrentDictionary<string, AlarmEvent> _activeAlarms = new();

        public event EventHandler<AlarmEvent>? AlarmTriggered;
        public event EventHandler<AlarmEvent>? AlarmUpdated;

        public AlarmManagerService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            _soundService = new SoundAlarmService();
        }

        public bool IsAudioMuted
        {
            get => _soundService.IsMuted;
            set => _soundService.IsMuted = value;
        }

        public AlarmEvent? TopActiveAlarm =>
            _activeAlarms.Values
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.Timestamp)
                .FirstOrDefault();

        public IReadOnlyList<AlarmEvent> GetActiveAlarms() =>
            _activeAlarms.Values
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.Timestamp)
                .ToList();

        public async Task RaiseAlarmAsync(AlarmEvent alarm)
        {
            // Check deduplication: If there's an active alarm for the same device and title, don't spam
            var existing = _activeAlarms.Values.FirstOrDefault(a => 
                a.DeviceId == alarm.DeviceId && 
                a.Title == alarm.Title && 
                a.Status != AlarmStatus.Resolved);

            if (existing != null)
            {
                // Update timestamp
                existing.Timestamp = DateTime.Now;
                await _databaseService.UpdateAlarmAsync(existing);
                AlarmUpdated?.Invoke(this, existing);
                return;
            }

            _activeAlarms[alarm.Id] = alarm;
            await _databaseService.SaveAlarmAsync(alarm);

            // Play sound if not acknowledged yet
            _soundService.PlayAlarm(alarm.Severity);

            AlarmTriggered?.Invoke(this, alarm);
        }

        public async Task AcknowledgeAlarmAsync(string alarmId, string acknowledgedBy)
        {
            if (_activeAlarms.TryGetValue(alarmId, out var alarm))
            {
                alarm.Status = AlarmStatus.Acknowledged;
                alarm.AcknowledgedBy = acknowledgedBy;
                alarm.AcknowledgedAt = DateTime.Now;

                await _databaseService.UpdateAlarmAsync(alarm);

                // Check if any unacknowledged critical alarms remain
                var hasUnackCritical = _activeAlarms.Values.Any(a => 
                    a.Severity == AlarmSeverity.Critical && 
                    a.Status == AlarmStatus.Active);

                if (!hasUnackCritical)
                {
                    _soundService.StopContinuousAlarm();
                }

                AlarmUpdated?.Invoke(this, alarm);
            }
        }

        public async Task ResolveAlarmAsync(string alarmId)
        {
            if (_activeAlarms.TryRemove(alarmId, out var alarm))
            {
                alarm.Status = AlarmStatus.Resolved;
                alarm.ResolvedAt = DateTime.Now;

                await _databaseService.UpdateAlarmAsync(alarm);

                // If no active alarms left, stop audio
                if (!_activeAlarms.Values.Any(a => a.Status == AlarmStatus.Active))
                {
                    _soundService.StopContinuousAlarm();
                }

                AlarmUpdated?.Invoke(this, alarm);
            }
        }

        public void ToggleMute()
        {
            IsAudioMuted = !IsAudioMuted;
        }
    }
}
