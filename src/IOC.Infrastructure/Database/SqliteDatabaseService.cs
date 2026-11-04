using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models;
using Microsoft.Data.Sqlite;

namespace IOC.Infrastructure.Database
{
    public class SqliteDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public SqliteDatabaseService(string? dbPath = null)
        {
            dbPath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ioc_data.db");
            _connectionString = $"Data Source={dbPath}";
        }

        public async Task InitializeAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Devices (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    SubSystem INTEGER NOT NULL,
                    IpAddress TEXT NOT NULL,
                    Port INTEGER NOT NULL,
                    Location TEXT,
                    Status INTEGER NOT NULL,
                    LastSeen TEXT NOT NULL,
                    LastError TEXT
                );

                CREATE TABLE IF NOT EXISTS AlarmEvents (
                    Id TEXT PRIMARY KEY,
                    DeviceId TEXT NOT NULL,
                    DeviceName TEXT NOT NULL,
                    SubSystem INTEGER NOT NULL,
                    Severity INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    Location TEXT,
                    Timestamp TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    AcknowledgedBy TEXT,
                    AcknowledgedAt TEXT,
                    ResolvedAt TEXT
                );
            ";
            await cmd.ExecuteNonQueryAsync();

            // Seed initial devices if empty
            await SeedInitialDevicesAsync(connection);
        }

        private async Task SeedInitialDevicesAsync(SqliteConnection connection)
        {
            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Devices;";
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            if (count > 0) return;

            var seedDevices = new List<Device>
            {
                new() { Name = "Switch Core Cisco 3850", SubSystem = SubSystemType.LanTel, IpAddress = "192.168.1.1", Port = 161, Location = "Phòng Máy Chủ - Tầng 2" },
                new() { Name = "Tổng đài IP Grandstream PBX", SubSystem = SubSystemType.LanTel, IpAddress = "192.168.1.2", Port = 5060, Location = "Phòng Máy Chủ - Tầng 2" },
                new() { Name = "Tủ Trung tâm Âm thanh PA Bosch", SubSystem = SubSystemType.PublicAddress, IpAddress = "192.168.2.10", Port = 161, Location = "Phòng Điều Hành - Tầng 1" },
                new() { Name = "Camera PTZ Sảnh Chính NVR", SubSystem = SubSystemType.CCTV, IpAddress = "192.168.3.101", Port = 80, Location = "Sảnh Tòa Nhà - Tầng 1" },
                new() { Name = "Camera Hành Lang Tầng 3", SubSystem = SubSystemType.CCTV, IpAddress = "192.168.3.103", Port = 80, Location = "Hành Lang Đông - Tầng 3" },
                new() { Name = "BMS - Bộ Điều Khiển Chiller Trane", SubSystem = SubSystemType.BMS, IpAddress = "192.168.4.50", Port = 502, Location = "Phòng Kỹ Thuật - Tầng Hầm" },
                new() { Name = "BMS - Đồng Hồ Đo Điện Tổng", SubSystem = SubSystemType.BMS, IpAddress = "192.168.4.51", Port = 502, Location = "Trạm Biến Áp - Tầng Hầm" },
                new() { Name = "Cửa Kiểm Soát Vào Ra Sảnh VIP", SubSystem = SubSystemType.AccessControl, IpAddress = "192.168.5.20", Port = 8000, Location = "Khu Vực VIP - Tầng 1" },
                new() { Name = "Tủ Trung Tâm Báo Cháy Hochiki (FAS)", SubSystem = SubSystemType.FireAlarm, IpAddress = "192.168.6.1", Port = 502, Location = "Phòng Trực PCCC - Tầng 1" }
            };

            foreach (var d in seedDevices)
            {
                await SaveDeviceInternalAsync(connection, d);
            }
        }

        public async Task SaveDeviceAsync(Device device)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await SaveDeviceInternalAsync(connection, device);
        }

        private static async Task SaveDeviceInternalAsync(SqliteConnection connection, Device device)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Devices (Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError)
                VALUES ($Id, $Name, $SubSystem, $IpAddress, $Port, $Location, $Status, $LastSeen, $LastError);
            ";
            cmd.Parameters.AddWithValue("$Id", device.Id);
            cmd.Parameters.AddWithValue("$Name", device.Name);
            cmd.Parameters.AddWithValue("$SubSystem", (int)device.SubSystem);
            cmd.Parameters.AddWithValue("$IpAddress", device.IpAddress);
            cmd.Parameters.AddWithValue("$Port", device.Port);
            cmd.Parameters.AddWithValue("$Location", device.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("$Status", (int)device.Status);
            cmd.Parameters.AddWithValue("$LastSeen", device.LastSeen.ToString("o"));
            cmd.Parameters.AddWithValue("$LastError", device.LastError ?? string.Empty);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateDeviceStatusAsync(string deviceId, DeviceStatus status, string lastError)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Devices 
                SET Status = $Status, LastSeen = $LastSeen, LastError = $LastError
                WHERE Id = $Id;
            ";
            cmd.Parameters.AddWithValue("$Status", (int)status);
            cmd.Parameters.AddWithValue("$LastSeen", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("$LastError", lastError ?? string.Empty);
            cmd.Parameters.AddWithValue("$Id", deviceId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            var list = new List<Device>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError FROM Devices;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Device
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    SubSystem = (SubSystemType)reader.GetInt32(2),
                    IpAddress = reader.GetString(3),
                    Port = reader.GetInt32(4),
                    Location = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Status = (DeviceStatus)reader.GetInt32(6),
                    LastSeen = DateTime.Parse(reader.GetString(7)),
                    LastError = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }
            return list;
        }

        public async Task SaveAlarmAsync(AlarmEvent alarm)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO AlarmEvents 
                (Id, DeviceId, DeviceName, SubSystem, Severity, Title, Description, Location, Timestamp, Status, AcknowledgedBy, AcknowledgedAt, ResolvedAt)
                VALUES ($Id, $DeviceId, $DeviceName, $SubSystem, $Severity, $Title, $Description, $Location, $Timestamp, $Status, $AcknowledgedBy, $AcknowledgedAt, $ResolvedAt);
            ";
            cmd.Parameters.AddWithValue("$Id", alarm.Id);
            cmd.Parameters.AddWithValue("$DeviceId", alarm.DeviceId);
            cmd.Parameters.AddWithValue("$DeviceName", alarm.DeviceName);
            cmd.Parameters.AddWithValue("$SubSystem", (int)alarm.SubSystem);
            cmd.Parameters.AddWithValue("$Severity", (int)alarm.Severity);
            cmd.Parameters.AddWithValue("$Title", alarm.Title);
            cmd.Parameters.AddWithValue("$Description", alarm.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$Location", alarm.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("$Timestamp", alarm.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$Status", (int)alarm.Status);
            cmd.Parameters.AddWithValue("$AcknowledgedBy", (object?)alarm.AcknowledgedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$AcknowledgedAt", alarm.AcknowledgedAt.HasValue ? alarm.AcknowledgedAt.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ResolvedAt", alarm.ResolvedAt.HasValue ? alarm.ResolvedAt.Value.ToString("o") : (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAlarmAsync(AlarmEvent alarm)
        {
            await SaveAlarmAsync(alarm);
        }

        public async Task<List<AlarmEvent>> GetRecentAlarmsAsync(int limit = 100)
        {
            var list = new List<AlarmEvent>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, DeviceId, DeviceName, SubSystem, Severity, Title, Description, Location, Timestamp, Status, AcknowledgedBy, AcknowledgedAt, ResolvedAt
                FROM AlarmEvents
                ORDER BY Timestamp DESC
                LIMIT $limit;
            ";
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AlarmEvent
                {
                    Id = reader.GetString(0),
                    DeviceId = reader.GetString(1),
                    DeviceName = reader.GetString(2),
                    SubSystem = (SubSystemType)reader.GetInt32(3),
                    Severity = (AlarmSeverity)reader.GetInt32(4),
                    Title = reader.GetString(5),
                    Description = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Location = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    Timestamp = DateTime.Parse(reader.GetString(8)),
                    Status = (AlarmStatus)reader.GetInt32(9),
                    AcknowledgedBy = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    AcknowledgedAt = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
                    ResolvedAt = reader.IsDBNull(12) ? null : DateTime.Parse(reader.GetString(12))
                });
            }
            return list;
        }
    }
}
