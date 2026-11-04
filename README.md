# Hệ Thống Quản Lý & Giám Sát Vận Hành Tập Trung (IOC Management Software)

Phần mềm giám sát vận hành 24/7 và báo lỗi trực tiếp trên giao diện dành cho trung tâm điều hành tòa nhà / khu đô thị thông minh.

---

## 1. Cấu Trúc Mã Nguồn (Clean Architecture)

```
IOCManagement.sln
├── src/
│   ├── IOC.Core/              # Models, Enums, Interfaces cốt lõi
│   │   ├── Enums/             # SubSystemType, AlarmSeverity, DeviceStatus, AlarmStatus
│   │   ├── Models/            # Device, AlarmEvent
│   │   └── Interfaces/        # IAlarmService, IDatabaseService, IDeviceMonitorService
│   │
│   ├── IOC.Infrastructure/    # Triển khai phần cứng, giao thức & cơ sở dữ liệu
│   │   ├── Database/          # SqliteDatabaseService (Lưu trữ cục bộ ioc_data.db)
│   │   ├── Services/          # SoundAlarmService (Còi báo loa PC), AlarmManagerService
│   │   └── Protocols/         # PingMonitorService (Ping đa luồng phát hiện lỗi rớt mạng)
│   │
│   └── IOC.App/               # Ứng dụng Desktop WPF (.exe)
│       ├── ViewModels/        # MainViewModel (MVVM với CommunityToolkit.Mvvm)
│       ├── MainWindow.xaml    # Giao diện Dark Mode trung tâm điều hành
│       └── App.xaml           # Khởi tạo MaterialDesignThemes & tài nguyên ứng dụng
│
├── bin/publish/               # Thư mục xuất file IOC.App.exe độc lập
├── Tài liệu hệ thống/         # Sơ đồ gốc và Báo cáo phân tích kiến trúc .docx
├── run.bat                    # Script 1-click chạy trực tiếp mã nguồn
└── publish.bat                # Script 1-click đóng gói ra file IOC.App.exe
```

---

## 2. Hướng Dẫn Sử Dụng Trong Antigravity IDE

### Cách 1: Chạy trực tiếp từ Terminal
Mở terminal trong Antigravity IDE và gõ:
```bash
./run.bat
```
hoặc:
```bash
dotnet run --project src/IOC.App
```

### Cách 2: Đóng gói thành file .exe hoàn chỉnh
Gõ lệnh:
```bash
./publish.bat
```
File thực thi `.exe` sẽ được tạo tại: `bin\publish\IOC.App.exe`. Bạn có thể nhấp đúp chạy trực tiếp ở bất kỳ máy tính Windows nào.

---

## 3. Các Tính Năng Đã Tích Hợp Sẵn Trong Bản MVP

1. **Top Active Alarm Banner:** Thanh báo động khẩn cấp màu đỏ rực luôn ghim đầu màn hình khi có sự cố, đi kèm nút bấm **`[ XÁC NHẬN SỰ CỐ (ACK) ]`**.
2. **Còi Báo Động Qua Loa PC:** Tự động phát âm thanh còi hú khi có sự cố Critical/Major, có nút **MUTE** tắt âm thanh tạm thời trên thanh công cụ.
3. **Cơ Sở Dữ Liệu SQLite Cục Bộ (`ioc_data.db`):** Tự động khởi tạo và nạp sẵn 9 thiết bị mẫu từ 6 phân hệ (LAN/Tel, Access Control, CCTV, PA, BMS, PCCC).
4. **Bảng Thử Nghiệm Sự Cố Nhanh (Bottom Simulation Panel):**
   - 🔥 **Test Báo Cháy (PCCC)**
   - 🔌 **Test Rớt Switch Core (LAN)**
   - 📢 **Test Đứt Dây Loa (PA)**
   - ❄️ **Test Quá Nhiệt Chiller (BMS)**
   - 📹 **Test Mất Cam Sảnh (CCTV)**
