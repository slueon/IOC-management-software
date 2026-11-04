using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models;

namespace IOC.Infrastructure.Services
{
    public class DeviceImportExportService : IDeviceImportExportService
    {
        private const string CsvHeader = "Mã,Tên thiết bị,Phân hệ,Địa chỉ IP,Cổng,Vị trí,Hãng sản xuất,Model,Địa chỉ MAC,Ghi chú";

        public string ExportToCsv(IEnumerable<Device> devices)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CsvHeader);

            foreach (var d in devices)
            {
                var line = string.Join(",",
                    EscapeCsv(d.Id),
                    EscapeCsv(d.Name),
                    EscapeCsv(d.SubSystem.ToString()),
                    EscapeCsv(d.IpAddress),
                    d.Port.ToString(),
                    EscapeCsv(d.Location),
                    EscapeCsv(d.Manufacturer),
                    EscapeCsv(d.Model),
                    EscapeCsv(d.MacAddress),
                    EscapeCsv(d.Notes)
                );
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        public async Task ExportToFileAsync(IEnumerable<Device> devices, string filePath)
        {
            var csv = ExportToCsv(devices);
            // Write with UTF-8 with BOM for Excel compatibility
            var encoding = new UTF8Encoding(true);
            await File.WriteAllTextAsync(filePath, csv, encoding);
        }

        public List<Device> ImportFromCsv(string csvContent, out List<string> errors)
        {
            var devices = new List<Device>();
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errors.Add("Tệp CSV rỗng hoặc không có dữ liệu.");
                return devices;
            }

            using var reader = new StringReader(csvContent);
            string? line = reader.ReadLine(); // Skip header
            int lineNumber = 1;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = ParseCsvLine(line);
                if (parts.Count < 4)
                {
                    errors.Add($"Dòng {lineNumber}: Dữ liệu không đủ trường (cần tối thiểu Tên, Phân hệ, IP, Cổng).");
                    continue;
                }

                // Index 0: Id (optional), Index 1: Name, Index 2: SubSystem, Index 3: IP, Index 4: Port
                string name = parts.Count > 1 ? parts[1].Trim() : string.Empty;
                string subSystemStr = parts.Count > 2 ? parts[2].Trim() : string.Empty;
                string ip = parts.Count > 3 ? parts[3].Trim() : string.Empty;
                string portStr = parts.Count > 4 ? parts[4].Trim() : "80";
                string location = parts.Count > 5 ? parts[5].Trim() : string.Empty;
                string manufacturer = parts.Count > 6 ? parts[6].Trim() : string.Empty;
                string model = parts.Count > 7 ? parts[7].Trim() : string.Empty;
                string mac = parts.Count > 8 ? parts[8].Trim() : string.Empty;
                string notes = parts.Count > 9 ? parts[9].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"Dòng {lineNumber}: Tên thiết bị không được để trống.");
                    continue;
                }

                if (!IPAddress.TryParse(ip, out _))
                {
                    errors.Add($"Dòng {lineNumber}: Địa chỉ IP '{ip}' không hợp lệ.");
                    continue;
                }

                if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
                {
                    port = 80;
                }

                if (!Enum.TryParse<SubSystemType>(subSystemStr, true, out var subSystem))
                {
                    // Fallback map
                    subSystem = subSystemStr.ToLowerInvariant() switch
                    {
                        var s when s.Contains("lan") || s.Contains("tel") || s.Contains("switch") => SubSystemType.LanTel,
                        var s when s.Contains("pa") || s.Contains("loa") || s.Contains("âm") => SubSystemType.PublicAddress,
                        var s when s.Contains("cam") || s.Contains("cctv") || s.Contains("nvr") => SubSystemType.CCTV,
                        var s when s.Contains("cửa") || s.Contains("access") => SubSystemType.AccessControl,
                        var s when s.Contains("cháy") || s.Contains("pccc") || s.Contains("fire") => SubSystemType.FireAlarm,
                        _ => SubSystemType.BMS
                    };
                }

                devices.Add(new Device
                {
                    Name = name,
                    SubSystem = subSystem,
                    IpAddress = ip,
                    Port = port,
                    Location = location,
                    Manufacturer = manufacturer,
                    Model = model,
                    MacAddress = mac,
                    Notes = notes,
                    Status = DeviceStatus.Online
                });
            }

            return devices;
        }

        public async Task<(List<Device> Devices, List<string> Errors)> ImportFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (new List<Device>(), new List<string> { $"Không tìm thấy tệp: {filePath}" });
            }

            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var devices = ImportFromCsv(content, out var errors);
            return (devices, errors);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}
