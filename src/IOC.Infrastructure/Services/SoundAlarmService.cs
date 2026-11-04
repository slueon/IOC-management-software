using System;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using IOC.Core.Enums;

namespace IOC.Infrastructure.Services
{
    public class SoundAlarmService
    {
        private bool _isMuted;
        private CancellationTokenSource? _alarmCts;
        private readonly object _lock = new();

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (_isMuted)
                {
                    StopContinuousAlarm();
                }
            }
        }

        public void PlayAlarm(AlarmSeverity severity)
        {
            if (_isMuted) return;

            switch (severity)
            {
                case AlarmSeverity.Critical:
                    StartContinuousAlarm();
                    break;

                case AlarmSeverity.Major:
                    Task.Run(() =>
                    {
                        try
                        {
                            SystemSounds.Exclamation.Play();
                            Thread.Sleep(300);
                            SystemSounds.Exclamation.Play();
                        }
                        catch { /* Ignore */ }
                    });
                    break;

                case AlarmSeverity.Minor:
                    try
                    {
                        SystemSounds.Asterisk.Play();
                    }
                    catch { /* Ignore */ }
                    break;

                case AlarmSeverity.Info:
                default:
                    break;
            }
        }

        public void StartContinuousAlarm()
        {
            if (_isMuted) return;

            lock (_lock)
            {
                if (_alarmCts != null) return; // Already alarming
                _alarmCts = new CancellationTokenSource();
                var token = _alarmCts.Token;

                Task.Run(() =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested && !_isMuted)
                        {
                            SystemSounds.Hand.Play();
                            Thread.Sleep(900);
                        }
                    }
                    catch { /* Ignore cancellation */ }
                }, token);
            }
        }

        public void StopContinuousAlarm()
        {
            lock (_lock)
            {
                if (_alarmCts != null)
                {
                    _alarmCts.Cancel();
                    _alarmCts.Dispose();
                    _alarmCts = null;
                }
            }
        }
    }
}
