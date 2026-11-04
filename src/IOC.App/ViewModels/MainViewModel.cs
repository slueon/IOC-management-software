using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models;
using IOC.Infrastructure.Database;
using IOC.Infrastructure.Protocols;
using IOC.Infrastructure.Services;

namespace IOC.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlarmService _alarmService;
        private readonly IDeviceMonitorService _monitorService;
        private readonly DispatcherTimer _clockTimer;

        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        [ObservableProperty]
        private bool _isAudioMuted;

        [ObservableProperty]
        private AlarmEvent? _topActiveAlarm;

        [ObservableProperty]
        private bool _hasActiveAlarm;

        [ObservableProperty]
        private int _totalDevices;

        [ObservableProperty]
        private int _onlineDevices;

        [ObservableProperty]
        private int _offlineDevices;

        [ObservableProperty]
        private int _activeAlarmCount;

        public ObservableCollection<AlarmEvent> ActiveAlarms { get; } = new();
        public ObservableCollection<AlarmEvent> RecentAlarms { get; } = new();
        public ObservableCollection<Device> Devices { get; } = new();

        public MainViewModel()
        {
            _databaseService = new SqliteDatabaseService();
            _alarmService = new AlarmManagerService(_databaseService);
            _monitorService = new PingMonitorService(_databaseService, _alarmService);

            _alarmService.AlarmTriggered += OnAlarmTriggered;
            _alarmService.AlarmUpdated += OnAlarmUpdated;
            _monitorService.DeviceStatusChanged += OnDeviceStatusChanged;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _clockTimer.Start();

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await _databaseService.InitializeAsync();
            var devices = await _databaseService.GetAllDevicesAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Devices.Clear();
                foreach (var d in devices)
                {
                    Devices.Add(d);
                    _monitorService.RegisterDevice(d);
                }
                UpdateCounters();
            });

            // Load recent alarms
            var recents = await _databaseService.GetRecentAlarmsAsync(50);
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentAlarms.Clear();
                foreach (var a in recents)
                {
                    RecentAlarms.Add(a);
                    if (a.Status != AlarmStatus.Resolved)
                    {
                        ActiveAlarms.Add(a);
                    }
                }
                UpdateTopAlarm();
            });

            // Start monitoring background
            _monitorService.StartMonitoring(15);
        }

        private void OnAlarmTriggered(object? sender, AlarmEvent alarm)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = ActiveAlarms.FirstOrDefault(a => a.Id == alarm.Id);
                if (existing == null)
                {
                    ActiveAlarms.Insert(0, alarm);
                    RecentAlarms.Insert(0, alarm);
                }
                UpdateTopAlarm();
            });
        }

        private void OnAlarmUpdated(object? sender, AlarmEvent alarm)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var active = ActiveAlarms.FirstOrDefault(a => a.Id == alarm.Id);
                if (alarm.Status == AlarmStatus.Resolved)
                {
                    if (active != null) ActiveAlarms.Remove(active);
                }
                else if (active != null)
                {
                    var index = ActiveAlarms.IndexOf(active);
                    ActiveAlarms[index] = alarm;
                }

                var recent = RecentAlarms.FirstOrDefault(a => a.Id == alarm.Id);
                if (recent != null)
                {
                    var rIndex = RecentAlarms.IndexOf(recent);
                    RecentAlarms[rIndex] = alarm;
                }

                UpdateTopAlarm();
            });
        }

        private void OnDeviceStatusChanged(object? sender, Device device)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var d = Devices.FirstOrDefault(x => x.Id == device.Id);
                if (d != null)
                {
                    var index = Devices.IndexOf(d);
                    Devices[index] = device;
                }
                UpdateCounters();
            });
        }

        private void UpdateTopAlarm()
        {
            TopActiveAlarm = _alarmService.TopActiveAlarm;
            HasActiveAlarm = (TopActiveAlarm != null);
            ActiveAlarmCount = ActiveAlarms.Count;
        }

        private void UpdateCounters()
        {
            TotalDevices = Devices.Count;
            OnlineDevices = Devices.Count(d => d.Status == DeviceStatus.Online);
            OfflineDevices = Devices.Count(d => d.Status == DeviceStatus.Offline);
        }

        [RelayCommand]
        public async Task AcknowledgeAlarmAsync(AlarmEvent? alarm)
        {
            if (alarm == null) return;
            await _alarmService.AcknowledgeAlarmAsync(alarm.Id, "Kỹ sư trực ban");
        }

        [RelayCommand]
        public async Task AcknowledgeTopAlarmAsync()
        {
            if (TopActiveAlarm != null)
            {
                await _alarmService.AcknowledgeAlarmAsync(TopActiveAlarm.Id, "Kỹ sư trực ban");
            }
        }

        [RelayCommand]
        public async Task ResolveAlarmAsync(AlarmEvent? alarm)
        {
            if (alarm == null) return;
            await _alarmService.ResolveAlarmAsync(alarm.Id);
        }

        [RelayCommand]
        public void ToggleMute()
        {
            _alarmService.ToggleMute();
            IsAudioMuted = _alarmService.IsAudioMuted;
        }

        // --- SIMULATION COMMANDS FOR TESTING ---

        [RelayCommand]
        public async Task SimulateFireAlarmAsync()
        {
            await _alarmService.RaiseAlarmAsync(new AlarmEvent
            {
                DeviceName = "Tủ Báo Cháy Hochiki (FAS)",
                SubSystem = SubSystemType.FireAlarm,
                Severity = AlarmSeverity.Critical,
                Title = "BÁO ĐỘNG CHÁY: Đầu báo khói kích hoạt tại Tầng 2!",
                Description = "Khu vực Hành lang Đông Tầng 2 phát hiện nồng độ khói vượt ngưỡng khẩn cấp.",
                Location = "Hành Lang Đông - Tầng 2"
            });
        }

        [RelayCommand]
        public async Task SimulateSwitchDownAsync()
        {
            await _alarmService.RaiseAlarmAsync(new AlarmEvent
            {
                DeviceName = "Switch Core Cisco 3850",
                SubSystem = SubSystemType.LanTel,
                Severity = AlarmSeverity.Critical,
                Title = "MẤT KẾT NỐI SWITCH CORE TRUNG TÂM",
                Description = "Giao thức SNMP gửi Trap linkDown: Cổng GigabitEthernet 1/0/1 bị ngắt kết nối.",
                Location = "Phòng Máy Chủ - Tầng 2"
            });
        }

        [RelayCommand]
        public async Task SimulatePaFaultAsync()
        {
            await _alarmService.RaiseAlarmAsync(new AlarmEvent
            {
                DeviceName = "Tủ Âm Thanh PA Bosch",
                SubSystem = SubSystemType.PublicAddress,
                Severity = AlarmSeverity.Major,
                Title = "SỰ CỐ ĐỨT ĐƯỜNG DÂY LOA (PA LINE FAULT)",
                Description = "Phát hiện thay đổi trở kháng bất thường tại Vùng loa Tầng 3 (Zone 3). Đường dây loa có thể bị đứt.",
                Location = "Phòng Điều Hành - Tầng 1"
            });
        }

        [RelayCommand]
        public async Task SimulateChillerFaultAsync()
        {
            await _alarmService.RaiseAlarmAsync(new AlarmEvent
            {
                DeviceName = "BMS - Chiller Trane",
                SubSystem = SubSystemType.BMS,
                Severity = AlarmSeverity.Major,
                Title = "CẢNH BÁO BMS: Nhiệt độ nước lạnh Chiller vượt ngưỡng",
                Description = "Thanh ghi Modbus 40012 ghi nhận nhiệt độ nước cấp đạt 18.5°C (Ngưỡng an toàn: 7-12°C).",
                Location = "Phòng Kỹ Thuật - Tầng Hầm"
            });
        }

        [RelayCommand]
        public async Task SimulateCameraLossAsync()
        {
            await _alarmService.RaiseAlarmAsync(new AlarmEvent
            {
                DeviceName = "Camera PTZ Sảnh Chính NVR",
                SubSystem = SubSystemType.CCTV,
                Severity = AlarmSeverity.Minor,
                Title = "Mất tín hiệu Camera Sảnh (Video Loss)",
                Description = "Không nhận được phản hồi gói tin heartbeat qua chuẩn ONVIF cổng 80.",
                Location = "Sảnh Tòa Nhà - Tầng 1"
            });
        }
    }
}
