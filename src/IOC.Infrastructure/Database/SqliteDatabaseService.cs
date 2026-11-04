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
                    LastError TEXT,
                    MacAddress TEXT,
                    Manufacturer TEXT,
                    Model TEXT,
                    InstallDate TEXT,
                    Notes TEXT,
                    Protocol TEXT,
                    LatencyMs REAL,
                    Telemetry TEXT,
                    UptimePercent REAL,
                    Floor TEXT,
                    CanvasX REAL,
                    CanvasY REAL
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

            // Run migration for existing databases
            await RunMigrationAsync(connection);

            // Seed initial devices if empty
            await SeedInitialDevicesAsync(connection);
        }

        private static async Task RunMigrationAsync(SqliteConnection connection)
        {
            string[] textColumns = ["MacAddress", "Manufacturer", "Model", "InstallDate", "Notes", "Protocol", "Telemetry", "Floor"];
            foreach (var col in textColumns)
            {
                try
                {
                    var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE Devices ADD COLUMN {col} TEXT;";
                    await alterCmd.ExecuteNonQueryAsync();
                }
                catch { }
            }

            string[] realColumns = ["LatencyMs", "UptimePercent", "CanvasX", "CanvasY"];
            foreach (var col in realColumns)
            {
                try
                {
                    var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE Devices ADD COLUMN {col} REAL DEFAULT 0;";
                    await alterCmd.ExecuteNonQueryAsync();
                }
                catch { }
            }
        }

        private async Task SeedInitialDevicesAsync(SqliteConnection connection)
        {
            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Devices;";
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            if (count > 0) return;

            var seedDevices = new List<Device>
            {
                new() 
                { 
                    Name = "Switch Core Cisco 3850", 
                    SubSystem = SubSystemType.LanTel, 
                    IpAddress = "192.168.1.1", 
                    Port = 161, 
                    Location = "Phòng Máy Chủ - Tầng 2", 
                    Manufacturer = "Cisco", 
                    Model = "WS-C3850-24T", 
                    MacAddress = "00:1A:2B:3C:4D:01",
                    Protocol = "SNMP v2c / Ping",
                    LatencyMs = 0.8,
                    Telemetry = "CPU: 14% | RAM: 35% | Cổng: 24/24 Up | Lưu lượng: 450 Mbps",
                    UptimePercent = 99.98,
                    Floor = "Tầng 2",
                    CanvasX = 220,
                    CanvasY = 120,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Tổng đài IP Grandstream PBX", 
                    SubSystem = SubSystemType.LanTel, 
                    IpAddress = "192.168.1.2", 
                    Port = 5060, 
                    Location = "Phòng Máy Chủ - Tầng 2", 
                    Manufacturer = "Grandstream", 
                    Model = "UCM6208", 
                    MacAddress = "00:1A:2B:3C:4D:02",
                    Protocol = "SIP / Ping",
                    LatencyMs = 1.1,
                    Telemetry = "SIP Trunk: OK | Máy nhánh online: 32/32 | Cuộc gọi đồng thời: 4",
                    UptimePercent = 99.95,
                    Floor = "Tầng 2",
                    CanvasX = 380,
                    CanvasY = 120,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Tủ Trung tâm Âm thanh PA Bosch", 
                    SubSystem = SubSystemType.PublicAddress, 
                    IpAddress = "192.168.2.10", 
                    Port = 161, 
                    Location = "Phòng Điều Hành - Tầng 1", 
                    Manufacturer = "Bosch", 
                    Model = "Praesideo PRS-NCO3", 
                    MacAddress = "00:1A:2B:3C:4D:03",
                    Protocol = "SNMP / Dry Contact",
                    LatencyMs = 1.8,
                    Telemetry = "Ampli: 38°C | Nguồn chính: OK | Trở kháng 8 Zone: Chuẩn 100V",
                    UptimePercent = 100.0,
                    Floor = "Tầng 1",
                    CanvasX = 480,
                    CanvasY = 100,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Camera PTZ Sảnh Chính NVR", 
                    SubSystem = SubSystemType.CCTV, 
                    IpAddress = "192.168.3.101", 
                    Port = 80, 
                    Location = "Sảnh Tòa Nhà - Tầng 1", 
                    Manufacturer = "Hikvision", 
                    Model = "DS-2DE4425IW-DE", 
                    MacAddress = "00:1A:2B:3C:4D:04",
                    Protocol = "ONVIF / RTSP",
                    LatencyMs = 2.4,
                    Telemetry = "Luồng 1080p 30fps | IR Night Vision: Auto | Bitrate: 4.2 Mbps",
                    UptimePercent = 99.7,
                    Floor = "Tầng 1",
                    CanvasX = 320,
                    CanvasY = 240,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Camera Hành Lang Tầng 3", 
                    SubSystem = SubSystemType.CCTV, 
                    IpAddress = "192.168.3.103", 
                    Port = 80, 
                    Location = "Hành Lang Đông - Tầng 3", 
                    Manufacturer = "Hikvision", 
                    Model = "DS-2CD2143G2-I", 
                    MacAddress = "00:1A:2B:3C:4D:05",
                    Protocol = "ONVIF / RTSP",
                    LatencyMs = 2.2,
                    Telemetry = "Luồng RTSP 25fps | Mã hóa H.265 | Ghi hình: Liên tục",
                    UptimePercent = 99.8,
                    Floor = "Tầng 3",
                    CanvasX = 350,
                    CanvasY = 180,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "BMS - Bộ Điều Khiển Chiller Trane", 
                    SubSystem = SubSystemType.BMS, 
                    IpAddress = "192.168.4.50", 
                    Port = 502, 
                    Location = "Phòng Kỹ Thuật - Tầng Hầm", 
                    Manufacturer = "Trane", 
                    Model = "Tracer AdaptiView", 
                    MacAddress = "00:1A:2B:3C:4D:06",
                    Protocol = "BACnet IP / Modbus",
                    LatencyMs = 3.5,
                    Telemetry = "Nhiệt độ cấp: 7.2°C | Hồi: 12.1°C | Áp suất: 3.4 bar | Tải: 68%",
                    UptimePercent = 99.85,
                    Floor = "Tầng Hầm",
                    CanvasX = 180,
                    CanvasY = 140,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "BMS - Đồng Hồ Đo Điện Tổng", 
                    SubSystem = SubSystemType.BMS, 
                    IpAddress = "192.168.4.51", 
                    Port = 502, 
                    Location = "Trạm Biến Áp - Tầng Hầm", 
                    Manufacturer = "Schneider", 
                    Model = "PM5350", 
                    MacAddress = "00:1A:2B:3C:4D:07",
                    Protocol = "Modbus RTU / TCP",
                    LatencyMs = 2.8,
                    Telemetry = "Điện áp: 380V (3 Pha) | Dòng: 215A | Công suất: 142 kW | Cos phi: 0.96",
                    UptimePercent = 99.99,
                    Floor = "Tầng Hầm",
                    CanvasX = 380,
                    CanvasY = 140,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Cửa Kiểm Soát Vào Ra Sảnh VIP", 
                    SubSystem = SubSystemType.AccessControl, 
                    IpAddress = "192.168.5.20", 
                    Port = 8000, 
                    Location = "Khu Vực VIP - Tầng 1", 
                    Manufacturer = "Suprema", 
                    Model = "BioStation 3", 
                    MacAddress = "00:1A:2B:3C:4D:08",
                    Protocol = "TCP/IP & Wiegand",
                    LatencyMs = 1.3,
                    Telemetry = "Cửa: Khóa an toàn | Ắc quy 12V: Tốt | Thẻ quẹt gần nhất: Hợp lệ",
                    UptimePercent = 99.95,
                    Floor = "Tầng 1",
                    CanvasX = 440,
                    CanvasY = 260,
                    Status = DeviceStatus.Online
                },
                new() 
                { 
                    Name = "Tủ Trung Tâm Báo Cháy Hochiki (FAS)", 
                    SubSystem = SubSystemType.FireAlarm, 
                    IpAddress = "192.168.6.1", 
                    Port = 502, 
                    Location = "Phòng Trực PCCC - Tầng 1", 
                    Manufacturer = "Hochiki", 
                    Model = "FireFinder Plus", 
                    MacAddress = "00:1A:2B:3C:4D:09",
                    Protocol = "Modbus RTU / Dry Contact",
                    LatencyMs = 1.2,
                    Telemetry = "Loop 1-4: Bình thường | Nguồn AC: 220V OK | Pin dự phòng: 27.2V",
                    UptimePercent = 100.0,
                    Floor = "Tầng 1",
                    CanvasX = 160,
                    CanvasY = 120,
                    Status = DeviceStatus.Online
                }
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
                INSERT OR REPLACE INTO Devices 
                (Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError, MacAddress, Manufacturer, Model, InstallDate, Notes, Protocol, LatencyMs, Telemetry, UptimePercent, Floor, CanvasX, CanvasY)
                VALUES 
                ($Id, $Name, $SubSystem, $IpAddress, $Port, $Location, $Status, $LastSeen, $LastError, $MacAddress, $Manufacturer, $Model, $InstallDate, $Notes, $Protocol, $LatencyMs, $Telemetry, $UptimePercent, $Floor, $CanvasX, $CanvasY);
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
            cmd.Parameters.AddWithValue("$MacAddress", device.MacAddress ?? string.Empty);
            cmd.Parameters.AddWithValue("$Manufacturer", device.Manufacturer ?? string.Empty);
            cmd.Parameters.AddWithValue("$Model", device.Model ?? string.Empty);
            cmd.Parameters.AddWithValue("$InstallDate", device.InstallDate.ToString("o"));
            cmd.Parameters.AddWithValue("$Notes", device.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("$Protocol", device.Protocol ?? "ICMP/Ping");
            cmd.Parameters.AddWithValue("$LatencyMs", device.LatencyMs);
            cmd.Parameters.AddWithValue("$Telemetry", device.Telemetry ?? string.Empty);
            cmd.Parameters.AddWithValue("$UptimePercent", device.UptimePercent);
            cmd.Parameters.AddWithValue("$Floor", device.Floor ?? "Tầng 1");
            cmd.Parameters.AddWithValue("$CanvasX", device.CanvasX);
            cmd.Parameters.AddWithValue("$CanvasY", device.CanvasY);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteDeviceAsync(string id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Devices WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);

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

        public async Task UpdateDeviceTelemetryAsync(string deviceId, DeviceStatus status, double latencyMs, string telemetry, string lastError)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Devices 
                SET Status = $Status, LastSeen = $LastSeen, LatencyMs = $LatencyMs, Telemetry = $Telemetry, LastError = $LastError
                WHERE Id = $Id;
            ";
            cmd.Parameters.AddWithValue("$Status", (int)status);
            cmd.Parameters.AddWithValue("$LastSeen", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("$LatencyMs", latencyMs);
            cmd.Parameters.AddWithValue("$Telemetry", telemetry ?? string.Empty);
            cmd.Parameters.AddWithValue("$LastError", lastError ?? string.Empty);
            cmd.Parameters.AddWithValue("$Id", deviceId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Device?> GetDeviceByIdAsync(string id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError, MacAddress, Manufacturer, Model, InstallDate, Notes, Protocol, LatencyMs, Telemetry, UptimePercent, Floor, CanvasX, CanvasY FROM Devices WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapDeviceFromReader(reader);
            }
            return null;
        }

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            var list = new List<Device>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError, MacAddress, Manufacturer, Model, InstallDate, Notes, Protocol, LatencyMs, Telemetry, UptimePercent, Floor, CanvasX, CanvasY FROM Devices ORDER BY SubSystem, Name;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapDeviceFromReader(reader));
            }
            return list;
        }

        public async Task<List<Device>> SearchDevicesAsync(string? keyword, SubSystemType? subSystem, DeviceStatus? status)
        {
            var list = new List<Device>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            var conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(Name LIKE $keyword OR IpAddress LIKE $keyword OR Location LIKE $keyword OR Manufacturer LIKE $keyword OR Model LIKE $keyword)");
                cmd.Parameters.AddWithValue("$keyword", $"%{keyword.Trim()}%");
            }

            if (subSystem.HasValue)
            {
                conditions.Add("SubSystem = $subSystem");
                cmd.Parameters.AddWithValue("$subSystem", (int)subSystem.Value);
            }

            if (status.HasValue)
            {
                conditions.Add("Status = $status");
                cmd.Parameters.AddWithValue("$status", (int)status.Value);
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            cmd.CommandText = $"SELECT Id, Name, SubSystem, IpAddress, Port, Location, Status, LastSeen, LastError, MacAddress, Manufacturer, Model, InstallDate, Notes, Protocol, LatencyMs, Telemetry, UptimePercent, Floor, CanvasX, CanvasY FROM Devices {whereClause} ORDER BY SubSystem, Name;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapDeviceFromReader(reader));
            }
            return list;
        }

        private static Device MapDeviceFromReader(SqliteDataReader reader)
        {
            var dev = new Device
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                SubSystem = (SubSystemType)reader.GetInt32(2),
                IpAddress = reader.GetString(3),
                Port = reader.GetInt32(4),
                Location = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Status = (DeviceStatus)reader.GetInt32(6),
                LastSeen = DateTime.Parse(reader.GetString(7)),
                LastError = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                MacAddress = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Manufacturer = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                Model = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                InstallDate = reader.IsDBNull(12) ? DateTime.Now : DateTime.Parse(reader.GetString(12)),
                Notes = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
            };

            if (reader.FieldCount > 14 && !reader.IsDBNull(14)) dev.Protocol = reader.GetString(14);
            if (reader.FieldCount > 15 && !reader.IsDBNull(15)) dev.LatencyMs = reader.GetDouble(15);
            if (reader.FieldCount > 16 && !reader.IsDBNull(16)) dev.Telemetry = reader.GetString(16);
            if (reader.FieldCount > 17 && !reader.IsDBNull(17)) dev.UptimePercent = reader.GetDouble(17);
            if (reader.FieldCount > 18 && !reader.IsDBNull(18)) dev.Floor = reader.GetString(18);
            if (reader.FieldCount > 19 && !reader.IsDBNull(19)) dev.CanvasX = reader.GetDouble(19);
            if (reader.FieldCount > 20 && !reader.IsDBNull(20)) dev.CanvasY = reader.GetDouble(20);

            return dev;
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
