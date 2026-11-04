using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Models;
using IOC.Infrastructure.Database;
using IOC.Infrastructure.Protocols;
using IOC.Infrastructure.Services;
using Xunit;

namespace IOC.Tests
{
    public class DeviceManagementTests
    {
        [Fact]
        public async Task Test_Database_CRUD_Lifecycle()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_ioc_{Guid.NewGuid():N}.db");
            try
            {
                var dbService = new SqliteDatabaseService(tempDb);
                await dbService.InitializeAsync();

                // 1. Initial seed check (9 devices)
                var devices = await dbService.GetAllDevicesAsync();
                Assert.Equal(9, devices.Count);

                // Verify telemetry and protocol were seeded
                var ciscoCore = devices.FirstOrDefault(d => d.Name.Contains("Cisco 3850"));
                Assert.NotNull(ciscoCore);
                Assert.Equal("SNMP v2c / Ping", ciscoCore.Protocol);
                Assert.Contains("CPU:", ciscoCore.Telemetry);

                // 2. Add new device
                var newDevice = new Device
                {
                    Name = "Switch Tầng 4 Mới",
                    SubSystem = SubSystemType.LanTel,
                    IpAddress = "192.168.1.104",
                    Port = 161,
                    Location = "Tầng 4",
                    Manufacturer = "Cisco",
                    Model = "C2960X",
                    Protocol = "SNMP v2c",
                    Telemetry = "Cổng 24 Up, Tải 12%",
                    LatencyMs = 1.2
                };
                await dbService.SaveDeviceAsync(newDevice);

                // 3. Get by ID
                var retrieved = await dbService.GetDeviceByIdAsync(newDevice.Id);
                Assert.NotNull(retrieved);
                Assert.Equal("Switch Tầng 4 Mới", retrieved.Name);
                Assert.Equal("192.168.1.104", retrieved.IpAddress);
                Assert.Equal("SNMP v2c", retrieved.Protocol);

                // 4. Search
                var searchResults = await dbService.SearchDevicesAsync("Tầng 4", SubSystemType.LanTel, null);
                Assert.Single(searchResults);

                // 5. Update Telemetry
                await dbService.UpdateDeviceTelemetryAsync(newDevice.Id, DeviceStatus.Online, 0.9, "Cổng 24 Up, Tải 18%", string.Empty);
                var updated = await dbService.GetDeviceByIdAsync(newDevice.Id);
                Assert.NotNull(updated);
                Assert.Equal(DeviceStatus.Online, updated.Status);
                Assert.Equal(0.9, updated.LatencyMs);
                Assert.Equal("Cổng 24 Up, Tải 18%", updated.Telemetry);

                // 6. Delete
                await dbService.DeleteDeviceAsync(newDevice.Id);
                var deleted = await dbService.GetDeviceByIdAsync(newDevice.Id);
                Assert.Null(deleted);
            }
            finally
            {
                if (File.Exists(tempDb))
                {
                    try { File.Delete(tempDb); } catch { }
                }
            }
        }

        [Fact]
        public void Test_Csv_Export_And_Import_Roundtrip()
        {
            var service = new DeviceImportExportService();

            var sampleDevices = new[]
            {
                new Device
                {
                    Name = "Camera Cửa Hầm",
                    SubSystem = SubSystemType.CCTV,
                    IpAddress = "192.168.3.150",
                    Port = 80,
                    Location = "Hầm B1",
                    Manufacturer = "Dahua",
                    Model = "IPC-HFW"
                },
                new Device
                {
                    Name = "Tủ Báo Cháy Vòng 1",
                    SubSystem = SubSystemType.FireAlarm,
                    IpAddress = "192.168.6.20",
                    Port = 502,
                    Location = "Phòng PCCC",
                    Manufacturer = "Hochiki",
                    Model = "H-Loop"
                }
            };

            // 1. Export
            string csv = service.ExportToCsv(sampleDevices);
            Assert.Contains("Camera Cửa Hầm", csv);
            Assert.Contains("192.168.3.150", csv);
            Assert.Contains("Tủ Báo Cháy Vòng 1", csv);

            // 2. Import back
            var imported = service.ImportFromCsv(csv, out var errors);
            Assert.Empty(errors);
            Assert.Equal(2, imported.Count);

            Assert.Equal("Camera Cửa Hầm", imported[0].Name);
            Assert.Equal(SubSystemType.CCTV, imported[0].SubSystem);
            Assert.Equal("192.168.3.150", imported[0].IpAddress);
            Assert.Equal("Hầm B1", imported[0].Location);

            Assert.Equal("Tủ Báo Cháy Vòng 1", imported[1].Name);
            Assert.Equal(SubSystemType.FireAlarm, imported[1].SubSystem);
        }

        [Fact]
        public async Task Test_AlarmManager_Lifecycle()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_alarm_{Guid.NewGuid():N}.db");
            try
            {
                var dbService = new SqliteDatabaseService(tempDb);
                await dbService.InitializeAsync();

                var alarmManager = new AlarmManagerService(dbService);
                alarmManager.IsAudioMuted = true; // Silence audio during test

                // 1. Raise alarm
                var alarm = new AlarmEvent
                {
                    DeviceName = "Switch Core",
                    SubSystem = SubSystemType.LanTel,
                    Severity = AlarmSeverity.Critical,
                    Title = "Mất nguồn Switch Core",
                    Location = "Phòng Server"
                };

                await alarmManager.RaiseAlarmAsync(alarm);

                Assert.NotNull(alarmManager.TopActiveAlarm);
                Assert.Equal("Mất nguồn Switch Core", alarmManager.TopActiveAlarm.Title);
                Assert.Single(alarmManager.GetActiveAlarms());

                // 2. Acknowledge alarm
                await alarmManager.AcknowledgeAlarmAsync(alarm.Id, "Kỹ sư test");
                var activeList = alarmManager.GetActiveAlarms();
                Assert.Single(activeList);
                Assert.Equal(AlarmStatus.Acknowledged, activeList[0].Status);
                Assert.Equal("Kỹ sư test", activeList[0].AcknowledgedBy);

                // 3. Resolve alarm
                await alarmManager.ResolveAlarmAsync(alarm.Id);
                Assert.Empty(alarmManager.GetActiveAlarms());
                Assert.Null(alarmManager.TopActiveAlarm);
            }
            finally
            {
                if (File.Exists(tempDb))
                {
                    try { File.Delete(tempDb); } catch { }
                }
            }
        }

        [Fact]
        public async Task Test_Monitoring_Service_Manual_Scan()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_mon_{Guid.NewGuid():N}.db");
            try
            {
                var dbService = new SqliteDatabaseService(tempDb);
                await dbService.InitializeAsync();

                var alarmManager = new AlarmManagerService(dbService);
                alarmManager.IsAudioMuted = true;

                var monitorService = new PingMonitorService(dbService, alarmManager);
                monitorService.IsDemoMode = true;

                var dev = new Device
                {
                    Name = "Server DNS Nội Bộ",
                    SubSystem = SubSystemType.LanTel,
                    IpAddress = "127.0.0.1",
                    Port = 53
                };
                monitorService.RegisterDevice(dev);

                // Trigger manual scan
                await monitorService.TriggerManualScanAsync();

                Assert.Equal(DeviceStatus.Online, dev.Status);
                Assert.True(dev.LatencyMs >= 0);
            }
            finally
            {
                if (File.Exists(tempDb))
                {
                    try { File.Delete(tempDb); } catch { }
                }
            }
        }
    }
}
