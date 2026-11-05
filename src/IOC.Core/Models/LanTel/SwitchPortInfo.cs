namespace IOC.Core.Models.LanTel;

public enum PortSpeed
{
    FastEthernet100M = 100,
    Gigabit1G = 1000,
    TenGigabit10G = 10000
}

public class SwitchPortInfo
{
    public int PortNumber { get; set; }
    public string PortName { get; set; } = string.Empty;
    public string PortType { get; set; } = "1000BASE-T (RJ45)";
    public bool IsUp { get; set; } = true;
    public bool IsAdminEnabled { get; set; } = true;
    public int SpeedMbps { get; set; } = 1000;
    public string Duplex { get; set; } = "Full";
    public int VlanId { get; set; } = 1;
    public string ConnectedDevice { get; set; } = string.Empty;
    public string DeviceCategory { get; set; } = "Hạ tầng chung";

    // Lưu lượng băng thông thời gian thực
    public double RxRateMbps { get; set; }
    public double TxRateMbps { get; set; }
    public double BandwidthUtilizationPercent => SpeedMbps > 0 ? Math.Round((RxRateMbps + TxRateMbps) / SpeedMbps * 100, 1) : 0;
    public bool IsCongested => BandwidthUtilizationPercent >= 85.0;

    // Đếm gói tin và phát hiện đứt cáp
    public long InOctets { get; set; }
    public long OutOctets { get; set; }
    public long CrcErrors { get; set; }
    public long PacketDrops { get; set; }
    public DateTime? CableFaultTimestamp { get; set; }

    public string PortStatusDisplay => !IsAdminEnabled ? "ADMIN DOWN" : (!IsUp ? "CÁP ĐỨT / DOWN" : (IsCongested ? "NGHẼN MẠNG" : "HOẠT ĐỘNG (UP)"));
}

public class PbxTrunkInfo
{
    public string TrunkName { get; set; } = "SIP-TRUNK-VNPT-MAIN";
    public string Provider { get; set; } = "VNPT Telecom";
    public string ServerIp { get; set; } = "10.200.1.10";
    public int Port { get; set; } = 5060;
    public bool IsSipTrunkUp { get; set; } = true;
    public int ActiveChannels { get; set; } = 6;
    public int MaxChannels { get; set; } = 30;
    public double LatencyMs { get; set; } = 4.2;
    public double CallCompletionRatePercent { get; set; } = 99.2;
    public DateTime? DownTimestamp { get; set; }
}

public enum PbxExtensionStatus
{
    Registered,   // Sẵn sàng nhận/gọi cuộc gọi
    InCall,       // Đang đàm thoại
    Ringing,      // Đang đổ chuông
    Disconnected  // Mất kết nối mạng / Rớt máy nhánh
}

public class PbxExtensionInfo
{
    public string ExtensionNumber { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string AssignedPerson { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string PhoneModel { get; set; } = "Grandstream GRP2602";
    public PbxExtensionStatus Status { get; set; } = PbxExtensionStatus.Registered;
    public bool IsConnected => Status != PbxExtensionStatus.Disconnected;
    public bool IsCriticalDepartment { get; set; }
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
