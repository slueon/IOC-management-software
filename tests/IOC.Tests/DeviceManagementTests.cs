using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Models;
using IOC.Infrastructure.Database;
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

                // 2. Add new device
                var newDevice = new Device
                {
                    Name = "Switch Tầng 4 Mới",
                    SubSystem = SubSystemType.LanTel,
                    IpAddress = "192.168.1.104",
                    Port = 161,
                    Location = "Tầng 4",
                    Manufacturer = "Cisco",
                    Model = "C2960X"
                };
                await dbService.SaveDeviceAsync(newDevice);

                // 3. Get by ID
                var retrieved = await dbService.GetDeviceByIdAsync(newDevice.Id);
                Assert.NotNull(retrieved);
                Assert.Equal("Switch Tầng 4 Mới", retrieved.Name);
                Assert.Equal("192.168.1.104", retrieved.IpAddress);

                // 4. Search
                var searchResults = await dbService.SearchDevicesAsync("Tầng 4", SubSystemType.LanTel, null);
                Assert.Single(searchResults);

                // 5. Update Status
                await dbService.UpdateDeviceStatusAsync(newDevice.Id, DeviceStatus.Offline, "Ping timeout");
                var updated = await dbService.GetDeviceByIdAsync(newDevice.Id);
                Assert.NotNull(updated);
                Assert.Equal(DeviceStatus.Offline, updated.Status);
                Assert.Equal("Ping timeout", updated.LastError);

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
    }
}
