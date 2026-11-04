namespace IOC.Core.Enums
{
    public enum SubSystemType
    {
        LanTel,        // Mạng nội bộ & Viễn thông / IP-PBX (SNMP)
        AccessControl, // Kiểm soát vào ra (SNMP / OPC / API)
        CCTV,          // Camera giám sát & NVR (ONVIF / SNMP)
        PublicAddress, // Âm thanh thông báo PA (SNMP)
        BMS,           // Quản lý tòa nhà HVAC / Bơm / Điện (Modbus / BACnet)
        FireAlarm      // Hệ thống Báo cháy PCCC (Modbus / Dry Contact)
    }

    public enum AlarmSeverity
    {
        Info,     // Thông tin hoạt động bình thường
        Minor,    // Cảnh báo nhẹ (1 camera ngoại vi mất mạng, nhiệt độ tăng nhẹ)
        Major,    // Nghiêm trọng (Tủ PA đứt dây loa, NVR hỏng ổ cứng, cạy cửa)
        Critical  // Nguy cấp (Báo cháy, Chiller sập nguồn, Switch Core mất kết nối)
    }

    public enum DeviceStatus
    {
        Online,   // Đang hoạt động bình thường
        Offline,  // Mất kết nối / Rớt mạng
        Warning,  // Vượt ngưỡng cảnh báo
        Alarm     // Đang trong trạng thái báo động sự cố
    }

    public enum AlarmStatus
    {
        Active,       // Sự cố đang diễn ra (chưa ai tiếp nhận)
        Acknowledged, // Kỹ sư trực đã bấm xác nhận (ACK)
        Resolved      // Sự cố đã được khắc phục xong
    }
}
