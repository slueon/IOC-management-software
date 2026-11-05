using IOC.Core.Enums;
using IOC.Core.Interfaces;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.Base;
using IOC.Core.Models.Bms;
using IOC.Core.Models.Cctv;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;
using IOC.Core.Models.PublicAddress;

namespace IOC.Infrastructure.Services;

public class SubsystemHealthEvaluator : ISubsystemHealthEvaluator
{
    public List<SubsystemIncident> EvaluateDevice(SubsystemDevice device)
    {
        var incidents = new List<SubsystemIncident>();

        switch (device)
        {
            case LanTelDevice lan:
                EvaluateLanTel(lan, incidents);
                break;
            case PublicAddressDevice pa:
                EvaluatePublicAddress(pa, incidents);
                break;
            case CctvDevice cctv:
                EvaluateCctv(cctv, incidents);
                break;
            case AccessControlDevice ac:
                EvaluateAccessControl(ac, incidents);
                break;
            case BmsDevice bms:
                EvaluateBms(bms, incidents);
                break;
            case FireAlarmDevice fa:
                EvaluateFireAlarm(fa, incidents);
                break;
        }

        UpdateDeviceStatus(device, incidents);
        return incidents;
    }

    private static void EvaluateLanTel(LanTelDevice lan, List<SubsystemIncident> incidents)
    {
        // 1. Giám sát hiệu năng phần cứng: Quá tải CPU (>90%)
        if (lan.CpuUsagePercent >= 90.0)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Quá tải CPU Switch ({lan.CpuUsagePercent:F0}%)",
                Description = $"Thiết bị {lan.Name} có mức sử dụng CPU vượt ngưỡng an toàn 90% (Hiện tại: {lan.CpuUsagePercent:F1}%)."
            });
        }

        // 2. Giám sát hiệu năng phần cứng: Tràn bộ nhớ RAM (>90%)
        if (lan.MemoryUsagePercent >= 90.0)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Tràn bộ nhớ RAM Switch/Router ({lan.MemoryUsagePercent:F0}%)",
                Description = $"Bộ nhớ RAM của {lan.Name} đạt {lan.MemoryUsedMb:F0}/{lan.MemoryTotalMb:F0} MB ({lan.MemoryUsagePercent:F1}%). Nguy cơ tràn bộ nhớ gây treo thiết bị."
            });
        }

        // 3. Giám sát hiệu năng phần cứng: Nhiệt độ bo mạch
        if (lan.TemperatureCelsius >= 75.0)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Critical,
                Title = $"Nhiệt độ bo mạch quá nhiệt khẩn cấp ({lan.TemperatureCelsius:F1}°C)",
                Description = $"Nhiệt độ phần cứng của {lan.Name} đạt {lan.TemperatureCelsius:F1}°C (Ngưỡng nguy hiểm >= 75°C). Quạt tản nhiệt hoặc môi trường rack gặp sự cố."
            });
        }
        else if (lan.TemperatureCelsius >= 60.0)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Minor,
                Title = $"Nhiệt độ bo mạch cao ({lan.TemperatureCelsius:F1}°C)",
                Description = $"Nhiệt độ phần cứng của {lan.Name} đạt {lan.TemperatureCelsius:F1}°C (Ngưỡng cảnh báo >= 60°C)."
            });
        }

        // 4. Theo dõi trạng thái cổng mạng Switch (Link Up/Down), phát hiện đứt cáp ngay tức thì
        foreach (var port in lan.Ports.Where(p => !p.IsUp && p.IsAdminEnabled))
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Link Down: Phát hiện đứt cáp cổng mạng {port.PortName}",
                Description = $"Cổng {port.PortName} ({port.ConnectedDevice}) trên {lan.Name} bị mất tín hiệu Link Down. Nghi vấn đứt cáp mạng hoặc thiết bị đầu cuối mất nguồn."
            });
        }

        // 5. Giám sát lưu lượng băng thông cổng mạng, cảnh báo nghẽn mạng cục bộ
        foreach (var port in lan.Ports.Where(p => p.IsCongested))
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = port.BandwidthUtilizationPercent >= 95.0 ? AlarmSeverity.Major : AlarmSeverity.Minor,
                Title = $"Cảnh báo nghẽn mạng cục bộ cổng {port.PortName} ({port.BandwidthUtilizationPercent:F0}%)",
                Description = $"Lưu lượng trên cổng {port.PortName} ({port.ConnectedDevice}) đạt {port.RxRateMbps + port.TxRateMbps:F0}/{port.SpeedMbps} Mbps ({port.BandwidthUtilizationPercent:F0}%). Nguy cơ rớt gói tin."
            });
        }

        if (lan.IsCongested && !lan.Ports.Any(p => p.IsCongested))
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Minor,
                Title = $"Cảnh báo nghẽn mạng tổng thể ({lan.BandwidthUtilizationPercent:F0}%)",
                Description = $"Lưu lượng băng thông cổng uplink của {lan.Name} đạt {lan.BandwidthUtilizationPercent:F0}%."
            });
        }

        // 6. Giám sát tổng đài IP-PBX: Cảnh báo rớt trung kế cuộc gọi (SIP Trunk Down)
        if (lan.PbxTrunk?.IsSipTrunkUp == false)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = lan.Name,
                Location = lan.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Rớt trung kế cuộc gọi tổng đài IP-PBX ({lan.PbxTrunk.TrunkName})",
                Description = $"Mất kết nối SIP Trunk {lan.PbxTrunk.TrunkName} từ nhà mạng {lan.PbxTrunk.Provider}, không thể tiếp nhận hoặc thực hiện cuộc gọi ra ngoài."
            });
        }

        // 7. Giám sát tổng đài IP-PBX: Mất kết nối máy nhánh
        foreach (var ext in lan.Extensions.Where(e => e.Status == PbxExtensionStatus.Disconnected))
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.LanTel,
                DeviceId = lan.Id,
                DeviceName = $"{lan.Name} - Ext {ext.ExtensionNumber}",
                Location = lan.Location,
                Severity = ext.IsCriticalDepartment ? AlarmSeverity.Major : AlarmSeverity.Minor,
                Title = $"Mất kết nối máy nhánh IP-PBX ({ext.ExtensionNumber} - {ext.DepartmentName})",
                Description = $"Máy nhánh {ext.ExtensionNumber} ({ext.DepartmentName} - {ext.AssignedPerson}) tại IP {ext.IpAddress} bị mất kết nối mạng / Unregistered."
            });
        }
    }

    private static void EvaluatePublicAddress(PublicAddressDevice pa, List<SubsystemIncident> incidents)
    {
        // 1. Giám sát trạng thái nguồn điện chính AC 220V
        if (!pa.IsMainPowerOk || pa.MainPowerVoltageVolt < 50.0)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.PublicAddress,
                DeviceId = pa.Id,
                DeviceName = pa.Name,
                Location = pa.Location,
                Severity = AlarmSeverity.Major,
                Title = "Mất nguồn điện chính tủ PA (Đang dùng ắc quy)",
                Description = $"Tủ âm thanh {pa.Name} mất nguồn lưới 220V, chuyển sang ắc quy dự phòng ({pa.BatteryVoltageVolt:F1}V)."
            });
        }

        // 2. Giám sát trạng thái ắc quy dự phòng DC 24V
        if (pa.IsBatteryFault || !pa.IsBackupBatteryOk || pa.BatteryVoltageVolt < 21.6)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.PublicAddress,
                DeviceId = pa.Id,
                DeviceName = pa.Name,
                Location = pa.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Sụt áp ắc quy dự phòng tủ PA ({pa.BatteryVoltageVolt:F1}V)",
                Description = $"Ắc quy dự phòng {pa.Name} sụt áp ({pa.BatteryVoltageVolt:F1}V, Pin: {pa.BatteryLevelPercent:F0}%). Nguy cơ tê liệt âm thanh khi mất điện lưới."
            });
        }

        // 3. Cảnh báo sự cố bộ tăng âm (Amplifiers): Quá nhiệt, quá tải, đứt cầu chì
        foreach (var amp in pa.Amplifiers)
        {
            if (amp.IsFuseBlown)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = $"{pa.Name} - {amp.AmplifierName}",
                    Location = pa.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"Đứt cầu chì bộ tăng âm ({amp.AmplifierName})",
                    Description = $"Cầu chì bảo vệ công suất của {amp.AmplifierName} bị đứt, ngừng phát âm thanh."
                });
            }

            if (amp.IsCriticalOverheated)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = $"{pa.Name} - {amp.AmplifierName}",
                    Location = pa.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"Bộ tăng âm quá nhiệt khẩn cấp ({amp.AmplifierName}: {amp.TemperatureCelsius:F0}°C)",
                    Description = $"Ampli {amp.AmplifierName} đạt nhiệt độ {amp.TemperatureCelsius:F1}°C (Ngưỡng khẩn cấp >= 85°C). Nguy cơ chập cháy biến áp."
                });
            }
            else if (amp.IsOverheated)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = $"{pa.Name} - {amp.AmplifierName}",
                    Location = pa.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Bộ tăng âm quá nhiệt ({amp.AmplifierName}: {amp.TemperatureCelsius:F0}°C)",
                    Description = $"Ampli {amp.AmplifierName} nhiệt độ vượt 75°C ({amp.TemperatureCelsius:F1}°C), quạt tản nhiệt hoặc tải quá nặng."
                });
            }

            if (amp.IsOverloaded)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = $"{pa.Name} - {amp.AmplifierName}",
                    Location = pa.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Quá tải công suất ampli ({amp.AmplifierName}: {amp.OutputLoadWatt:F0}W/{amp.RatedPowerWatt:F0}W)",
                    Description = $"Tải loa vượt công suất định mức của bộ tăng âm ({amp.LoadPercentage:F0}%)."
                });
            }
        }

        // 4. Phát hiện đứt đường dây loa hoặc chập tải theo từng phân vùng (Zone Impedance Fault)
        foreach (var zone in pa.SpeakerZones.Where(z => z.HasImpedanceFault))
        {
            if (zone.FaultType == SpeakerZoneFaultType.ShortCircuit)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = pa.Name,
                    Location = zone.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"CHẬP TẢI đường dây loa - Lỗi trở kháng {zone.ZoneName} (Short Circuit: {zone.ImpedanceOhms:F1}Ω)",
                    Description = $"Phát hiện chập tải/ngắn mạch đường dây loa tại {zone.ZoneName} ({zone.Location}) (Trở kháng: {zone.ImpedanceOhms:F1}Ω, Chuẩn: {zone.NormalImpedanceOhms:F0}Ω). Nguy cơ gây cháy biến áp loa hoặc đứt cầu chì ampli!"
                });
            }
            else if (zone.FaultType == SpeakerZoneFaultType.OpenCircuit)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = pa.Name,
                    Location = zone.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Đứt đường dây loa - Lỗi trở kháng {zone.ZoneName} (Open Circuit: {zone.ImpedanceOhms:F1}Ω)",
                    Description = $"Trở kháng đo được {zone.ImpedanceOhms:F1}Ω (Chuẩn: {zone.NormalImpedanceOhms:F0}Ω). Đường dây phân vùng {zone.ZoneName} ({zone.Location}) bị đứt hoặc mất kết nối cuối tuyến."
                });
            }
            else
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.PublicAddress,
                    DeviceId = pa.Id,
                    DeviceName = pa.Name,
                    Location = zone.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Lỗi trở kháng đường dây loa ({zone.ZoneName}: {zone.ImpedanceOhms:F1}Ω)",
                    Description = $"Sai lệch trở kháng đường dây loa {zone.ZoneName}: {zone.ImpedanceOhms:F1}Ω so với chuẩn {zone.NormalImpedanceOhms:F0}Ω (> 30%)."
                });
            }
        }
    }

    private static void EvaluateCctv(CctvDevice cctv, List<SubsystemIncident> incidents)
    {
        if (cctv.DeviceType == CctvDeviceType.IpCamera)
        {
            if (cctv.Status == DeviceStatus.Offline || cctv.MissedHeartbeats >= 3)
            {
                cctv.Status = DeviceStatus.Offline;
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Cctv,
                    DeviceId = cctv.Id,
                    DeviceName = cctv.Name,
                    Location = cctv.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"Mất tín hiệu Heartbeat Camera ({cctv.Name})",
                    Description = $"Camera không phản hồi lệnh Ping/Heartbeat chu kỳ {cctv.HeartbeatCycleSeconds}s (Mất {cctv.MissedHeartbeats} nhịp liên tiếp)."
                });
            }

            if (cctv.IsVideoLoss || !cctv.IsRtspStreamActive)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Cctv,
                    DeviceId = cctv.Id,
                    DeviceName = cctv.Name,
                    Location = cctv.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Mất tín hiệu hình ảnh Video Loss ({cctv.Name})",
                    Description = $"Luồng RTSP của camera {cctv.Name} không phản hồi dữ liệu video (Bitrate: {cctv.BitrateKbps} Kbps)."
                });
            }
        }
        else if (cctv.DeviceType == CctvDeviceType.NvrRecorder && cctv.NvrStorage != null)
        {
            var st = cctv.NvrStorage;
            if (st.HasBadSector)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Cctv,
                    DeviceId = cctv.Id,
                    DeviceName = cctv.Name,
                    Location = cctv.Location,
                    Severity = AlarmSeverity.Major,
                    Title = "Hỏng ổ cứng NVR (Bad Sector phát hiện)",
                    Description = $"Phát hiện lỗi vật lý Bad Sector ({st.BadSectorCount} bad sectors) trên mảng ổ cứng đầu ghi {cctv.Name}."
                });
            }

            if (st.RaidStatus != RaidHealthStatus.Healthy)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Cctv,
                    DeviceId = cctv.Id,
                    DeviceName = cctv.Name,
                    Location = cctv.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Sự cố mảng RAID ({st.RaidStatus})",
                    Description = $"Mảng đĩa RAID của NVR ở trạng thái {st.RaidStatus}, nguy cơ mất dữ liệu ghi hình."
                });
            }

            if (st.DiskUsagePercent >= 95.0)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Cctv,
                    DeviceId = cctv.Id,
                    DeviceName = cctv.Name,
                    Location = cctv.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Đầu ghi NVR sắp đầy dung lượng ({st.DiskUsagePercent:F1}%)",
                    Description = $"Dung lượng ổ cứng NVR đã dùng {st.StorageUsedGb}/{st.StorageTotalGb} GB."
                });
            }
        }
    }

    private static void EvaluateAccessControl(AccessControlDevice ac, List<SubsystemIncident> incidents)
    {
        if (!ac.IsControllerOnline || ac.Status == DeviceStatus.Offline)
        {
            ac.Status = DeviceStatus.Critical;
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.AccessControl,
                DeviceId = ac.Id,
                DeviceName = ac.Name,
                Location = ac.Location,
                Severity = AlarmSeverity.Critical,
                Title = $"Mất kết nối Bộ điều khiển cửa ({ac.Name})",
                Description = $"Bộ điều khiển cửa {ac.Name} mất liên lạc mạng LAN hoặc ngắt phản hồi."
            });
        }

        if (ac.IsBatteryLow)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.AccessControl,
                DeviceId = ac.Id,
                DeviceName = ac.Name,
                Location = ac.Location,
                Severity = AlarmSeverity.Minor,
                Title = $"Bộ điều khiển cửa ắc quy yếu ({ac.BatteryLevelPercent:F0}%)",
                Description = $"Pin dự phòng của controller {ac.Name} dưới 20% ({ac.BatteryLevelPercent:F0}%)."
            });
        }

        if (!ac.IsMainPowerOk)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.AccessControl,
                DeviceId = ac.Id,
                DeviceName = ac.Name,
                Location = ac.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Mất nguồn điện bộ điều khiển ({ac.Name})",
                Description = $"Bộ điều khiển cửa {ac.Name} mất nguồn điện lưới AC 220V, đang chạy pin dự phòng."
            });
        }

        foreach (var door in ac.DoorPoints)
        {
            if (!door.IsConnected)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = door.DoorName,
                    Location = door.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Mất kết nối đầu đọc thẻ cửa ({door.DoorName})",
                    Description = $"Đầu đọc thẻ / cảm biến từ cửa {door.DoorName} tại {door.Location} mất kết nối."
                });
            }

            if (door.IsForcedOpen)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = door.DoorName,
                    Location = door.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Báo động CỬA BỊ CẠY PHÁ (Door Forced Open: {door.DoorName})",
                    Description = $"Cửa {door.DoorName} tại {door.Location} bị mở cưỡng bức khi chưa quẹt thẻ."
                });
            }

            if (door.IsHeldOpen)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = door.DoorName,
                    Location = door.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Cửa bị chèn giữ mở quá thời gian ({door.HeldDurationSeconds}s)",
                    Description = $"Cửa {door.DoorName} mở quá ngưỡng cho phép {door.HeldThresholdSeconds}s."
                });
            }
        }

        foreach (var el in ac.Elevators)
        {
            if (!el.IsConnected)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = el.ElevatorName,
                    Location = ac.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"Mất kết nối bộ điều khiển thang máy ({el.ElevatorName})",
                    Description = $"Mất kết nối điều khiển phân tầng thang máy {el.ElevatorName}."
                });
            }

            if (el.IsEmergencyStop)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = el.ElevatorName,
                    Location = ac.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"CẢNH BÁO: Dừng khẩn cấp / Kẹt thang ({el.ElevatorName})",
                    Description = $"Cabin thang máy {el.ElevatorName} kích hoạt phanh dừng khẩn cấp tại tầng {el.CurrentFloorDisplay}."
                });
            }

            if (el.IsOverloaded)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.AccessControl,
                    DeviceId = ac.Id,
                    DeviceName = el.ElevatorName,
                    Location = ac.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Thang máy quá tải trọng ({el.LoadKg:F0}kg / {el.RatedLoadKg:F0}kg)",
                    Description = $"Cabin thang {el.ElevatorName} vượt quá tải trọng định mức ({el.LoadPercent:F0}%)."
                });
            }
        }
    }

    private static void EvaluateBms(BmsDevice bms, List<SubsystemIncident> incidents)
    {
        if (bms.Chiller != null)
        {
            if (bms.Chiller.IsTempExceeded)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"Quá nhiệt hệ thống Chiller (Cấp: {bms.Chiller.SupplyWaterTempCelsius:F1}°C / Hồi: {bms.Chiller.ReturnWaterTempCelsius:F1}°C)",
                    Description = "Nhiệt độ nước hệ thống Chiller vượt ngưỡng tối đa an toàn."
                });
            }

            if (bms.Chiller.IsPressureExceeded)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Bất thường áp suất hệ thống Chiller ({bms.Chiller.SystemPressureBar:F1} bar)",
                    Description = "Áp suất hệ thống Chiller ngoài dải vận hành tiêu chuẩn."
                });
            }

            foreach (var unit in bms.Chiller.HvacUnits.Where(u => u.IsFilterClogged))
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = unit.UnitName,
                    Location = unit.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Tắc phin lọc không khí ({unit.UnitName})",
                    Description = $"Cảm biến chênh áp phin lọc của {unit.UnitName} phát hiện bụi bẩn tắc nghẽn."
                });
            }
        }

        foreach (var pump in bms.Pumps.Where(p => p.IsThermalOverloadTripped))
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.Bms,
                DeviceId = bms.Id,
                DeviceName = pump.PumpName,
                Location = bms.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Máy bơm nhảy rơ-le nhiệt (Thermal Overload Trip: {pump.PumpName})",
                Description = $"Máy bơm {pump.PumpName} ({pump.PumpType}) dừng hoạt động do nhảy rơ-le nhiệt."
            });
        }

        if (bms.PowerGrid != null)
        {
            if (bms.PowerGrid.IsPhaseLossDetected)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = "Mất pha nguồn điện tổng (Phase Loss Alarm)",
                    Description = $"Phát hiện mất pha trạm điện: Pha A: {bms.PowerGrid.VoltagePhaseA:F0}V, Pha B: {bms.PowerGrid.VoltagePhaseB:F0}V, Pha C: {bms.PowerGrid.VoltagePhaseC:F0}V."
                });
            }

            if (bms.PowerGrid.IsCurrentOverload)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Major,
                    Title = "Quá tải dòng điện nguồn tổng",
                    Description = "Dòng điện pha vượt ngưỡng tối đa an toàn 220A."
                });
            }

            if (bms.PowerGrid.IsVoltageSag)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = "Sụt áp nguồn điện lưới (< 195V)",
                    Description = "Điện áp cung cấp sụt dưới ngưỡng quy chuẩn."
                });
            }
        }

        if (bms.Generator != null)
        {
            if (bms.Generator.IsFuelLow)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Generator.GeneratorName,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Mức nhiên liệu dầu máy phát điện thấp ({bms.Generator.FuelLevelPercent:F0}%)",
                    Description = $"Dầu Diesel dự phòng cho máy phát điện {bms.Generator.GeneratorName} dưới mức an toàn 25%."
                });
            }

            if (bms.Generator.IsStarterBatteryLow)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Generator.GeneratorName,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Minor,
                    Title = $"Ắc quy đề nổ máy phát yếu ({bms.Generator.BatteryVoltageVolt:F1}V)",
                    Description = "Điện áp ắc quy khởi động máy phát dự phòng dưới 22V, nguy cơ không thể tự động đề nổ khi mất điện lưới."
                });
            }
        }

        if (bms.Hazard != null)
        {
            if (bms.Hazard.IsSumpPitEmptyAlarm)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Cảnh báo cạn bể nước ngầm ({bms.Hazard.SumpPitWaterLevelPercent:F0}%)",
                    Description = "Mực nước bể ngầm dưới mức tối thiểu 15%, nguy cơ cháy bơm hút."
                });
            }

            if (bms.Hazard.IsGasLeakDetected)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.Bms,
                    DeviceId = bms.Id,
                    DeviceName = bms.Name,
                    Location = bms.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"NGUY HIỂM: Rò rỉ khí Gas/CO hầm xe ({bms.Hazard.GasCoConcentrationPpm:F0} ppm)",
                    Description = "Nồng độ khí độc Gas/CO vượt ngưỡng nguy hiểm 50 ppm tại khu vực tầng hầm."
                });
            }
        }
    }

    private static void EvaluateFireAlarm(FireAlarmDevice fa, List<SubsystemIncident> incidents)
    {
        foreach (var loop in fa.Loops)
        {
            if (loop.IsLoopOpenFault)
            {
                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.FireAlarm,
                    DeviceId = fa.Id,
                    DeviceName = fa.Name,
                    Location = fa.Location,
                    Severity = AlarmSeverity.Major,
                    Title = $"Đứt mạch vòng cáp tín hiệu PCCC (Loop Open Fault: {loop.LoopName})",
                    Description = $"Vòng cáp {loop.LoopName} bị đứt hoặc mất kết nối cuối tuyến (Loop Open)."
                });
            }

            foreach (var s in loop.Sensors.Where(x => x.IsAlarmTriggered))
            {
                var sensorTypeName = s.SensorType switch
                {
                    FireAlarmSensorType.SmokeDetector => "Đầu Báo Khói",
                    FireAlarmSensorType.HeatDetector => "Đầu Báo Nhiệt",
                    FireAlarmSensorType.ManualCallPoint => "Nút Ấn Khẩn Cấp",
                    _ => "Cảm biến PCCC"
                };

                incidents.Add(new SubsystemIncident
                {
                    Subsystem = SubsystemType.FireAlarm,
                    DeviceId = fa.Id,
                    DeviceName = $"{sensorTypeName} [{s.PointAddress}]",
                    Location = s.Location,
                    Severity = AlarmSeverity.Critical,
                    Title = $"BÁO CHÁY KHẨN CẤP: {sensorTypeName} kích hoạt tại {s.Location}!",
                    Description = $"Tín hiệu báo động cháy từ {s.PointAddress} ({s.Location}). Chế độ giám sát Read-Only."
                });
            }
        }

        if (fa.IsBatteryBackupActive || !fa.IsAcPowerOk)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.FireAlarm,
                DeviceId = fa.Id,
                DeviceName = fa.Name,
                Location = fa.Location,
                Severity = AlarmSeverity.Major,
                Title = "Tủ báo cháy chuyển sang nguồn pin phụ do mất điện lưới",
                Description = $"Tủ trung tâm PCCC mất nguồn AC 220V, đang duy trì bằng ắc quy dự phòng ({fa.BatteryVoltageVolt:F1}V)."
            });
        }

        if (fa.IsBatteryLow)
        {
            incidents.Add(new SubsystemIncident
            {
                Subsystem = SubsystemType.FireAlarm,
                DeviceId = fa.Id,
                DeviceName = fa.Name,
                Location = fa.Location,
                Severity = AlarmSeverity.Major,
                Title = $"Ắc quy dự phòng tủ báo cháy yếu ({fa.BatteryVoltageVolt:F1}V / {fa.BatteryLevelPercent:F0}%)",
                Description = $"Điện áp ắc quy dự phòng của tủ PCCC dưới ngưỡng an toàn 21.0V ({fa.BatteryVoltageVolt:F1}V)."
            });
        }
    }

    private static void UpdateDeviceStatus(SubsystemDevice device, List<SubsystemIncident> incidents)
    {
        if (incidents.Any(i => i.Severity == AlarmSeverity.Critical))
        {
            device.Status = DeviceStatus.Critical;
            device.StatusSummary = incidents.First(i => i.Severity == AlarmSeverity.Critical).Title;
        }
        else if (incidents.Any(i => i.Severity == AlarmSeverity.Major))
        {
            device.Status = DeviceStatus.Critical;
            device.StatusSummary = incidents.First(i => i.Severity == AlarmSeverity.Major).Title;
        }
        else if (incidents.Any(i => i.Severity == AlarmSeverity.Minor))
        {
            device.Status = DeviceStatus.Warning;
            device.StatusSummary = incidents.First(i => i.Severity == AlarmSeverity.Minor).Title;
        }
        else if (device.Status != DeviceStatus.Offline && device.Status != DeviceStatus.Maintenance)
        {
            device.Status = DeviceStatus.Online;
            device.StatusSummary = "Hoạt động bình thường";
        }
    }

    public SubsystemHealthSummary CalculateSummary(SubsystemType type, IEnumerable<SubsystemDevice> devices, IEnumerable<SubsystemIncident> incidents)
    {
        var devList = devices.Where(d => d.Subsystem == type).ToList();
        var incList = incidents.Where(i => i.Subsystem == type && !i.IsResolved).ToList();

        var name = type switch
        {
            SubsystemType.LanTel => "Mạng LAN & Viễn thông",
            SubsystemType.PublicAddress => "Âm thanh Thông báo (PA)",
            SubsystemType.Cctv => "Camera An ninh (CCTV)",
            SubsystemType.AccessControl => "Kiểm soát Vào Ra (Access)",
            SubsystemType.Bms => "Cơ điện & BMS",
            SubsystemType.FireAlarm => "Báo cháy PCCC",
            _ => type.ToString()
        };

        return new SubsystemHealthSummary
        {
            Subsystem = type,
            SubsystemName = name,
            TotalDevices = devList.Count,
            OnlineDevices = devList.Count(d => d.Status == DeviceStatus.Online),
            WarningDevices = devList.Count(d => d.Status == DeviceStatus.Warning),
            CriticalDevices = devList.Count(d => d.Status == DeviceStatus.Critical),
            OfflineDevices = devList.Count(d => d.Status == DeviceStatus.Offline),
            ActiveIncidentsCount = incList.Count,
            CriticalIncidentsCount = incList.Count(i => i.Severity == AlarmSeverity.Critical)
        };
    }

    public OverallHealthSummary CalculateOverallSummary(IEnumerable<SubsystemHealthSummary> subsystemSummaries)
    {
        var list = subsystemSummaries.ToList();
        return new OverallHealthSummary
        {
            TotalSubsystems = list.Count,
            TotalDevices = list.Sum(s => s.TotalDevices),
            TotalOnline = list.Sum(s => s.OnlineDevices),
            TotalWarning = list.Sum(s => s.WarningDevices),
            TotalCritical = list.Sum(s => s.CriticalDevices),
            TotalOffline = list.Sum(s => s.OfflineDevices),
            TotalActiveIncidents = list.Sum(s => s.ActiveIncidentsCount),
            TotalCriticalIncidents = list.Sum(s => s.CriticalIncidentsCount),
            LastUpdated = DateTime.UtcNow,
            SubsystemSummaries = list
        };
    }
}

