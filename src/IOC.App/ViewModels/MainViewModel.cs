using System;
using System.Collections.ObjectModel;
using System.IO;
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
using Microsoft.Win32;

namespace IOC.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlarmService _alarmService;
        private readonly IDeviceMonitorService _monitorService;
        private readonly IDeviceImportExportService _importExportService;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _telemetryJitterTimer;

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

        [ObservableProperty]
        private double _overallUptimePercent = 99.9;

        [ObservableProperty]
        private double _averageLatencyMs = 1.4;

        [ObservableProperty]
        private string _systemHealthStatus = "TẤT CẢ PHÂN HỆ HOẠT ĐỘNG ỔN ĐỊNH";

        [ObservableProperty]
        private string _systemHealthColor = "#48BB78";

        // --- SELECTED TAB ---
        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // --- FILTER & SEARCH PROPERTIES ---
        [ObservableProperty]
        private string _searchDeviceKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedSubSystemFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedStatusFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedFloorFilter = "Tất cả";

        // --- NEW DEVICE FORM PROPERTIES ---
        [ObservableProperty]
        private bool _isAddDeviceFormOpen;

        [ObservableProperty]
        private string _newDeviceName = string.Empty;

        [ObservableProperty]
        private SubSystemType _newDeviceSubSystem = SubSystemType.LanTel;

        [ObservableProperty]
        private string _newDeviceIp = "192.168.1.100";

        [ObservableProperty]
        private int _newDevicePort = 80;

        [ObservableProperty]
        private string _newDeviceLocation = string.Empty;

        [ObservableProperty]
        private string _newDeviceManufacturer = string.Empty;

        [ObservableProperty]
        private string _newDeviceModel = string.Empty;

        [ObservableProperty]
        private string _newDeviceProtocol = "ICMP/Ping";

        [ObservableProperty]
        private string _newDeviceFloor = "Tầng 1";

        // --- INCIDENT DRILL MODAL (SIMULATION POPUP) ---
        [ObservableProperty]
        private bool _isDrillModalOpen;

        // --- EDIT DEVICE PROPERTIES ---
        [ObservableProperty]
        private Device? _selectedDevice;

        [ObservableProperty]
        private string _statusMessage = "Đang thường trực giám sát 6 phân hệ thời gian thực 24/7.";

        // --- COLLECTIONS ---
        public ObservableCollection<SubSystemOverview> SubSystemOverviews { get; } = new();
        public ObservableCollection<Device> Devices { get; } = new();
        public ObservableCollection<Device> FilteredDevices { get; } = new();
        public ObservableCollection<Device> FloorDevices { get; } = new();
        public ObservableCollection<AlarmEvent> ActiveAlarms { get; } = new();
        public ObservableCollection<AlarmEvent> RecentAlarms { get; } = new();

        public ObservableCollection<string> SubSystemFilterOptions { get; } = new()
        {
            "Tất cả", "LanTel", "PublicAddress", "CCTV", "BMS", "AccessControl", "FireAlarm"
        };

        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "Tất cả", "Online", "Offline"
        };

        public ObservableCollection<string> FloorFilterOptions { get; } = new()
        {
            "Tất cả", "Tầng Hầm", "Tầng 1", "Tầng 2", "Tầng 3"
        };

        public ObservableCollection<SubSystemType> SubSystemTypes { get; } = new()
        {
            SubSystemType.LanTel,
            SubSystemType.PublicAddress,
            SubSystemType.CCTV,
            SubSystemType.BMS,
            SubSystemType.AccessControl,
            SubSystemType.FireAlarm
        };

        public MainViewModel()
        {
            _databaseService = new SqliteDatabaseService();
            _alarmService = new AlarmManagerService(_databaseService);
            _monitorService = new PingMonitorService(_databaseService, _alarmService);
            _importExportService = new DeviceImportExportService();

            _alarmService.AlarmTriggered += OnAlarmTriggered;
            _alarmService.AlarmUpdated += OnAlarmUpdated;
            _monitorService.DeviceStatusChanged += OnDeviceStatusChanged;

            // Real-time clock timer (1s)
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _clockTimer.Start();

            // Telemetry live animation/jitter timer (3s) to show active telemetry heartbeat
            _telemetryJitterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _telemetryJitterTimer.Tick += (_, _) => RefreshTelemetryJitter();
            _telemetryJitterTimer.Start();

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
                ApplyDeviceFilter();
                ApplyFloorFilter();
                UpdateCounters();
                UpdateSubsystemOverviews();
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
                UpdateSubsystemOverviews();
            });

            // Start monitoring background
            _monitorService.StartMonitoring(10);
            StatusMessage = $"Hệ thống IOC đã kết nối {Devices.Count} thiết bị trên 6 phân hệ. Chu kỳ quét 10s.";
        }

        private void UpdateSubsystemOverviews()
        {
            var subs = new[]
            {
                new { Type = SubSystemType.FireAlarm, Name = "PCCC & BÁO CHÁY", Icon = "🔥", Desc = "Tủ trung tâm Hochiki & Cảm biến khói", Protocol = "Modbus RTU / Dry Contact", Key1 = "4/4 Vòng Loop Thông suốt", Key2 = "Nguồn AC 220V & Pin 27.2V", Uptime = 100.0 },
                new { Type = SubSystemType.LanTel, Name = "MẠNG & VIỄN THÔNG", Icon = "🌐", Desc = "Cisco Core 3850, Switch tầng & IP-PBX", Protocol = "SNMP v2c / Ping / SIP", Key1 = "Độ trễ TB: 0.9 ms | CPU: 14%", Key2 = "Cổng Uplink: 1 Gbps Up", Uptime = 99.98 },
                new { Type = SubSystemType.CCTV, Name = "CAMERA AN NINH (CCTV)", Icon = "📹", Desc = "NVR 64 kênh & Camera PTZ / Dome", Protocol = "ONVIF / RTSP Stream", Key1 = "100% Luồng hình ảnh Live", Key2 = "HDD RAID-5: 82% | 25 FPS", Uptime = 99.85 },
                new { Type = SubSystemType.BMS, Name = "CƠ ĐIỆN TÒA NHÀ (BMS)", Icon = "❄️", Desc = "Chiller Trane, Đo điện & Cảm biến Server", Protocol = "BACnet IP / Modbus TCP", Key1 = "Chiller: Cấp 7.2°C - Hồi 12.1°C", Key2 = "Server: 21.8°C | 51.5% RH", Uptime = 99.92 },
                new { Type = SubSystemType.PublicAddress, Name = "ÂM THANH THÔNG BÁO (PA)", Icon = "📢", Desc = "Bộ điều khiển Bosch & 8 Zone Loa", Protocol = "SNMP / Matrix Dry Contact", Key1 = "Ampli: 38°C (Bình thường)", Key2 = "8 Zone Loa: Chuẩn 100V", Uptime = 100.0 },
                new { Type = SubSystemType.AccessControl, Name = "KIỂM SOÁT VÀO RA", Icon = "🚪", Desc = "ZKTeco C3-400 & Đầu đọc sinh trắc học", Protocol = "TCP/IP & Wiegand 34/26", Key1 = "Cửa: Khóa an toàn 100%", Key2 = "Ắc quy 12V: Tốt", Uptime = 99.95 }
            };

            SubSystemOverviews.Clear();

            bool anyCritical = false;
            foreach (var item in subs)
            {
                var devInSub = Devices.Where(d => d.SubSystem == item.Type).ToList();
                int total = devInSub.Count;
                int online = devInSub.Count(d => d.Status == DeviceStatus.Online);
                bool hasAlarm = ActiveAlarms.Any(a => a.SubSystem == item.Type);

                string statusText = "BÌNH THƯỜNG";
                string statusColor = "#48BB78"; // Green

                if (hasAlarm || online < total)
                {
                    var critical = ActiveAlarms.Any(a => a.SubSystem == item.Type && a.Severity == AlarmSeverity.Critical);
                    if (critical)
                    {
                        statusText = "SỰ CỐ NGUY CẤP";
                        statusColor = "#E53E3E"; // Red
                        anyCritical = true;
                    }
                    else
                    {
                        statusText = "CẢNH BÁO";
                        statusColor = "#ED8936"; // Amber
                    }
                }

                SubSystemOverviews.Add(new SubSystemOverview
                {
                    SubSystem = item.Type,
                    DisplayName = item.Name,
                    Icon = item.Icon,
                    Description = item.Desc,
                    StatusText = statusText,
                    StatusColor = statusColor,
                    ProtocolInfo = item.Protocol,
                    TotalDevices = total,
                    OnlineDevices = online,
                    PrimaryMetric = item.Key1,
                    SecondaryMetric = item.Key2,
                    Uptime = item.Uptime,
                    HasAlert = hasAlarm || (online < total)
                });
            }

            if (anyCritical)
            {
                SystemHealthStatus = "PHÁT HIỆN SỰ CỐ TẠI MỘT SỐ PHÂN HỆ - CẦN XỬ LÝ!";
                SystemHealthColor = "#E53E3E";
            }
            else if (ActiveAlarms.Count > 0)
            {
                SystemHealthStatus = "HỆ THỐNG CÓ CẢNH BÁO CẦN LƯU Ý";
                SystemHealthColor = "#ED8936";
            }
            else
            {
                SystemHealthStatus = "TẤT CẢ 6 PHÂN HỆ ĐANG HOẠT ĐỘNG ỔN ĐỊNH";
                SystemHealthColor = "#48BB78";
            }
        }

        private void RefreshTelemetryJitter()
        {
            if (Devices.Count == 0) return;

            // Calculate live average latency
            var onlineList = Devices.Where(d => d.Status == DeviceStatus.Online).ToList();
            if (onlineList.Count > 0)
            {
                AverageLatencyMs = Math.Round(onlineList.Average(d => d.LatencyMs), 2);
            }
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
                UpdateSubsystemOverviews();
                StatusMessage = $"[CẢNH BÁO MỚI] {alarm.Severity}: {alarm.Title}";
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
                UpdateSubsystemOverviews();
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
                ApplyDeviceFilter();
                ApplyFloorFilter();
                UpdateCounters();
                UpdateSubsystemOverviews();
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
            OverallUptimePercent = TotalDevices > 0 ? Math.Round((double)OnlineDevices / TotalDevices * 100, 1) : 100.0;
        }

        partial void OnSearchDeviceKeywordChanged(string value) => ApplyDeviceFilter();
        partial void OnSelectedSubSystemFilterChanged(string value) => ApplyDeviceFilter();
        partial void OnSelectedStatusFilterChanged(string value) => ApplyDeviceFilter();
        partial void OnSelectedFloorFilterChanged(string value) => ApplyFloorFilter();

        private void ApplyDeviceFilter()
        {
            var query = Devices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchDeviceKeyword))
            {
                var kw = SearchDeviceKeyword.Trim().ToLowerInvariant();
                query = query.Where(d => 
                    d.Name.ToLowerInvariant().Contains(kw) ||
                    d.IpAddress.Contains(kw) ||
                    d.Location.ToLowerInvariant().Contains(kw) ||
                    d.Manufacturer.ToLowerInvariant().Contains(kw));
            }

            if (SelectedSubSystemFilter != "Tất cả" && Enum.TryParse<SubSystemType>(SelectedSubSystemFilter, out var sub))
            {
                query = query.Where(d => d.SubSystem == sub);
            }

            if (SelectedStatusFilter != "Tất cả" && Enum.TryParse<DeviceStatus>(SelectedStatusFilter, out var st))
            {
                query = query.Where(d => d.Status == st);
            }

            FilteredDevices.Clear();
            foreach (var d in query)
            {
                FilteredDevices.Add(d);
            }
        }

        private void ApplyFloorFilter()
        {
            var query = Devices.AsEnumerable();
            if (SelectedFloorFilter != "Tất cả")
            {
                query = query.Where(d => d.Floor == SelectedFloorFilter);
            }

            FloorDevices.Clear();
            foreach (var d in query)
            {
                FloorDevices.Add(d);
            }
        }

        // --- MONITORING COMMANDS ---

        [RelayCommand]
        public async Task ScanNowAsync()
        {
            StatusMessage = "Đang quét đo lường thời gian thực toàn bộ 6 phân hệ...";
            await _monitorService.TriggerManualScanAsync();
            UpdateCounters();
            UpdateSubsystemOverviews();
            StatusMessage = $"Quét hoàn tất lúc {DateTime.Now:HH:mm:ss}. Tất cả phân hệ đã cập nhật telemetry.";
        }

        [RelayCommand]
        public async Task PingSingleDeviceAsync(Device? device)
        {
            if (device == null) return;
            StatusMessage = $"Đang kiểm tra kết nối tới {device.Name} ({device.IpAddress})...";
            await _monitorService.PingCheckDeviceAsync(device);
            StatusMessage = $"Kiểm tra {device.Name} hoàn tất: {device.Status} ({device.LatencyMs} ms)";
        }

        [RelayCommand]
        public void SwitchToTab(int tabIndex)
        {
            SelectedTabIndex = tabIndex;
        }

        // --- ALARM COMMANDS ---

        [RelayCommand]
        public async Task AcknowledgeAlarmAsync(AlarmEvent? alarm)
        {
            if (alarm == null) return;
            await _alarmService.AcknowledgeAlarmAsync(alarm.Id, "Kỹ sư trực ban");
            StatusMessage = $"Đã xác nhận sự cố: {alarm.DeviceName}";
        }

        [RelayCommand]
        public async Task AcknowledgeTopAlarmAsync()
        {
            if (TopActiveAlarm != null)
            {
                await _alarmService.AcknowledgeAlarmAsync(TopActiveAlarm.Id, "Kỹ sư trực ban");
                StatusMessage = $"Đã xác nhận sự cố khẩn cấp: {TopActiveAlarm.DeviceName}";
            }
        }

        [RelayCommand]
        public async Task ResolveAlarmAsync(AlarmEvent? alarm)
        {
            if (alarm == null) return;
            await _alarmService.ResolveAlarmAsync(alarm.Id);
            StatusMessage = $"Đã khắc phục xong sự cố: {alarm.DeviceName}";
        }

        [RelayCommand]
        public void ToggleMute()
        {
            _alarmService.ToggleMute();
            IsAudioMuted = _alarmService.IsAudioMuted;
            StatusMessage = IsAudioMuted ? "Đã tắt âm thanh còi báo." : "Đã bật âm thanh còi báo.";
        }

        // --- INCIDENT DRILL MODAL (SIMULATION POPUP) ---

        [RelayCommand]
        public void OpenDrillModal()
        {
            IsDrillModalOpen = true;
        }

        [RelayCommand]
        public void CloseDrillModal()
        {
            IsDrillModalOpen = false;
        }

        [RelayCommand]
        public async Task TriggerDrillScenarioAsync(string scenario)
        {
            switch (scenario)
            {
                case "Fire":
                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceName = "Tủ Báo Cháy Hochiki (FAS)",
                        SubSystem = SubSystemType.FireAlarm,
                        Severity = AlarmSeverity.Critical,
                        Title = "BÁO ĐỘNG CHÁY: Đầu báo khói kích hoạt tại Tầng 2!",
                        Description = "Khu vực Hành lang Đông Tầng 2 phát hiện nồng độ khói vượt ngưỡng khẩn cấp.",
                        Location = "Hành Lang Đông - Tầng 2"
                    });
                    break;

                case "Switch":
                    var sw = Devices.FirstOrDefault(d => d.SubSystem == SubSystemType.LanTel);
                    if (sw != null)
                    {
                        sw.Status = DeviceStatus.Offline;
                        sw.LastError = "SNMP linkDown: Cổng Uplink bị ngắt";
                        OnDeviceStatusChanged(this, sw);
                    }
                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceName = "Switch Core Cisco 3850",
                        SubSystem = SubSystemType.LanTel,
                        Severity = AlarmSeverity.Critical,
                        Title = "MẤT KẾT NỐI SWITCH CORE TRUNG TÂM",
                        Description = "Giao thức SNMP gửi Trap linkDown: Cổng GigabitEthernet 1/0/1 bị ngắt kết nối.",
                        Location = "Phòng Máy Chủ - Tầng 2"
                    });
                    break;

                case "PA":
                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceName = "Tủ Âm Thanh PA Bosch",
                        SubSystem = SubSystemType.PublicAddress,
                        Severity = AlarmSeverity.Major,
                        Title = "SỰ CỐ ĐỨT ĐƯỜNG DÂY LOA (PA LINE FAULT)",
                        Description = "Phát hiện thay đổi trở kháng bất thường tại Vùng loa Tầng 3 (Zone 3). Đường dây loa có thể bị đứt.",
                        Location = "Phòng Điều Hành - Tầng 1"
                    });
                    break;

                case "Chiller":
                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceName = "BMS - Chiller Trane",
                        SubSystem = SubSystemType.BMS,
                        Severity = AlarmSeverity.Major,
                        Title = "CẢNH BÁO BMS: Nhiệt độ nước lạnh Chiller vượt ngưỡng",
                        Description = "Thanh ghi Modbus 40012 ghi nhận nhiệt độ nước cấp đạt 18.5°C (Ngưỡng an toàn: 7-12°C).",
                        Location = "Phòng Kỹ Thuật - Tầng Hầm"
                    });
                    break;

                case "CCTV":
                    var cam = Devices.FirstOrDefault(d => d.SubSystem == SubSystemType.CCTV);
                    if (cam != null)
                    {
                        cam.Status = DeviceStatus.Offline;
                        cam.LastError = "RTSP timeout 1000ms";
                        OnDeviceStatusChanged(this, cam);
                    }
                    await _alarmService.RaiseAlarmAsync(new AlarmEvent
                    {
                        DeviceName = "Camera PTZ Sảnh Chính NVR",
                        SubSystem = SubSystemType.CCTV,
                        Severity = AlarmSeverity.Minor,
                        Title = "Mất tín hiệu Camera Sảnh (Video Loss)",
                        Description = "Không nhận được phản hồi gói tin heartbeat qua chuẩn ONVIF cổng 80.",
                        Location = "Sảnh Tòa Nhà - Tầng 1"
                    });
                    break;

                case "RestoreAll":
                    // Restore all devices to Online
                    foreach (var d in Devices)
                    {
                        d.Status = DeviceStatus.Online;
                        d.LastError = string.Empty;
                        d.LastSeen = DateTime.Now;
                    }
                    // Clear all alarms
                    var alarmsToResolve = ActiveAlarms.ToList();
                    foreach (var a in alarmsToResolve)
                    {
                        await _alarmService.ResolveAlarmAsync(a.Id);
                    }
                    UpdateCounters();
                    UpdateSubsystemOverviews();
                    StatusMessage = "Đã khôi phục toàn bộ hệ thống về trạng thái bình thường 100%.";
                    break;
            }

            IsDrillModalOpen = false;
        }

        // --- DEVICE MANAGEMENT COMMANDS (MODULE 6) ---

        [RelayCommand]
        public void OpenAddDeviceForm()
        {
            NewDeviceName = string.Empty;
            NewDeviceIp = "192.168.1.100";
            NewDevicePort = 80;
            NewDeviceLocation = string.Empty;
            NewDeviceManufacturer = string.Empty;
            NewDeviceModel = string.Empty;
            NewDeviceProtocol = "ICMP/Ping";
            NewDeviceFloor = "Tầng 1";
            IsAddDeviceFormOpen = true;
        }

        [RelayCommand]
        public void CloseAddDeviceForm()
        {
            IsAddDeviceFormOpen = false;
        }

        [RelayCommand]
        public async Task SaveNewDeviceAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDeviceName) || string.IsNullOrWhiteSpace(NewDeviceIp))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Tên thiết bị và Địa chỉ IP.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dev = new Device
            {
                Name = NewDeviceName.Trim(),
                SubSystem = NewDeviceSubSystem,
                IpAddress = NewDeviceIp.Trim(),
                Port = NewDevicePort,
                Location = NewDeviceLocation.Trim(),
                Manufacturer = NewDeviceManufacturer.Trim(),
                Model = NewDeviceModel.Trim(),
                Protocol = NewDeviceProtocol.Trim(),
                Floor = NewDeviceFloor,
                Status = DeviceStatus.Online,
                LastSeen = DateTime.Now,
                LatencyMs = 1.2,
                Telemetry = "Đang khởi tạo giám sát",
                UptimePercent = 100.0,
                CanvasX = 200,
                CanvasY = 200
            };

            await _databaseService.SaveDeviceAsync(dev);
            _monitorService.RegisterDevice(dev);

            Devices.Add(dev);
            ApplyDeviceFilter();
            ApplyFloorFilter();
            UpdateCounters();
            UpdateSubsystemOverviews();

            IsAddDeviceFormOpen = false;
            StatusMessage = $"Đã thêm mới thiết bị '{dev.Name}' vào danh bạ giám sát.";
        }

        [RelayCommand]
        public async Task DeleteDeviceAsync(Device? device)
        {
            if (device == null) return;

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa thiết bị '{device.Name}' ({device.IpAddress}) khỏi hệ thống giám sát?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                await _databaseService.DeleteDeviceAsync(device.Id);
                _monitorService.UnregisterDevice(device.Id);

                Devices.Remove(device);
                ApplyDeviceFilter();
                ApplyFloorFilter();
                UpdateCounters();
                UpdateSubsystemOverviews();

                StatusMessage = $"Đã xóa thiết bị '{device.Name}' khỏi danh bạ.";
            }
        }

        [RelayCommand]
        public async Task ExportDevicesAsync()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Tệp CSV (Excel) (*.csv)|*.csv",
                    FileName = $"Danh_ba_thiet_bi_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    Title = "Xuất Danh Bạ Thiết Bị Ra File CSV"
                };

                if (sfd.ShowDialog() == true)
                {
                    await _importExportService.ExportToFileAsync(Devices, sfd.FileName);
                    StatusMessage = $"Đã xuất {Devices.Count} thiết bị ra tệp: {Path.GetFileName(sfd.FileName)}";
                    MessageBox.Show($"Xuất danh bạ thành công!\nĐường dẫn: {sfd.FileName}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất danh bạ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task ImportDevicesAsync()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
                    Title = "Chọn Tệp CSV Chứa Danh Sách Thiết Bị"
                };

                if (ofd.ShowDialog() == true)
                {
                    var (imported, errors) = await _importExportService.ImportFromFileAsync(ofd.FileName);

                    if (imported.Count > 0)
                    {
                        foreach (var d in imported)
                        {
                            await _databaseService.SaveDeviceAsync(d);
                            _monitorService.RegisterDevice(d);

                            var existing = Devices.FirstOrDefault(x => x.IpAddress == d.IpAddress);
                            if (existing == null)
                            {
                                Devices.Add(d);
                            }
                        }

                        ApplyDeviceFilter();
                        ApplyFloorFilter();
                        UpdateCounters();
                        UpdateSubsystemOverviews();

                        string msg = $"Đã nhập thành công {imported.Count} thiết bị vào hệ thống.";
                        if (errors.Count > 0)
                        {
                            msg += $"\nCó {errors.Count} cảnh báo lỗi dòng:\n" + string.Join("\n", errors.Take(5));
                        }
                        MessageBox.Show(msg, "Kết quả nhập dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                        StatusMessage = $"Đã nhập thành công {imported.Count} thiết bị.";
                    }
                    else
                    {
                        string errs = errors.Count > 0 ? string.Join("\n", errors) : "Không tìm thấy dữ liệu hợp lệ.";
                        MessageBox.Show($"Không thể nhập dữ liệu:\n{errs}", "Lỗi nhập dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập danh bạ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
