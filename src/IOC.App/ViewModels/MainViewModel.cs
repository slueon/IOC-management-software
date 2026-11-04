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

        // --- FILTER & SEARCH PROPERTIES ---
        [ObservableProperty]
        private string _searchDeviceKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedSubSystemFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedStatusFilter = "Tất cả";

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

        // --- EDIT DEVICE PROPERTIES ---
        [ObservableProperty]
        private Device? _selectedDevice;

        [ObservableProperty]
        private string _statusMessage = "Hệ thống sẵn sàng giám sát 24/7.";

        public ObservableCollection<AlarmEvent> ActiveAlarms { get; } = new();
        public ObservableCollection<AlarmEvent> RecentAlarms { get; } = new();
        public ObservableCollection<Device> Devices { get; } = new();
        public ObservableCollection<Device> FilteredDevices { get; } = new();

        public ObservableCollection<string> SubSystemFilterOptions { get; } = new()
        {
            "Tất cả", "LanTel", "PublicAddress", "CCTV", "BMS", "AccessControl", "FireAlarm"
        };

        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "Tất cả", "Online", "Offline"
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
                ApplyDeviceFilter();
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
            StatusMessage = $"Đã nạp {Devices.Count} thiết bị. Bắt đầu chu trình giám sát ngầm.";
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

        partial void OnSearchDeviceKeywordChanged(string value) => ApplyDeviceFilter();
        partial void OnSelectedSubSystemFilterChanged(string value) => ApplyDeviceFilter();
        partial void OnSelectedStatusFilterChanged(string value) => ApplyDeviceFilter();

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
                Status = DeviceStatus.Online,
                LastSeen = DateTime.Now
            };

            await _databaseService.SaveDeviceAsync(dev);
            _monitorService.RegisterDevice(dev);

            Devices.Add(dev);
            ApplyDeviceFilter();
            UpdateCounters();

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
                UpdateCounters();

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
                        UpdateCounters();

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
