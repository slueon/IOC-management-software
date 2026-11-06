# Trung Tâm Giám Sát & Điều Hành Tập Trung (IOC Enterprise Management Software)

[![Build & Verify Gate](https://github.com/slueon/IOC-management-software/actions/workflows/ci.yml/badge.svg)](https://github.com/slueon/IOC-management-software/actions/workflows/ci.yml)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WPF Desktop](https://img.shields.io/badge/Platform-WPF%20Windows-purple.svg)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Tests](https://img.shields.io/badge/Unit%20Tests-117%20Passed-brightgreen.svg)](tests/IOC.Tests/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)]()

Hệ thống phần mềm giám sát vận hành 24/7 và cảnh báo sự cố thời gian thực dành cho **Trung tâm Điều hành Tập trung (IOC - Intelligent Operations Center)** của tòa nhà cao tầng và khu đô thị thông minh. Được thiết kế theo chuẩn SCADA công nghiệp với công nghệ .NET 8 WPF, kiến trúc Clean Architecture, bộ giao thức kết nối thiết bị đa tầng và cơ chế an toàn vận hành 2 cấp.

---

## 1. Kiến Trúc Hệ Thống (Clean Architecture)

```
IOCManagement.sln
├── .github/
│   └── workflows/ci.yml               # CI Pipeline tự động (Restore -> Build -warnaserror -> Test Coverage)
├── src/
│   ├── IOC.Core/                      # Domain Entities, Enums, Abstraction & Contracts cốt lõi
│   │   ├── Contracts/
│   │   │   ├── Adapters/              # IDeviceAdapter, IPingAdapter, ISnmpAdapter, IModbusAdapter, ...
│   │   │   ├── Persistence/           # IIocStorageService, AuditLogEntry, IncidentAcknowledgementRecord
│   │   │   └── Session/               # IOperatorSessionService, OperatorProfile
│   │   ├── Enums/                     # SubsystemType, AlarmSeverity, DeviceStatus, DeviceHealthState
│   │   ├── Interfaces/                # ISubsystemHealthEvaluator, ISubsystemHealthMonitoringService
│   │   └── Models/                    # Mô hình thiết bị 6 phân hệ (LanTel, PA, CCTV, Access, BMS, Fire)
│   │
│   ├── IOC.Infrastructure/            # Triển khai tầng hạ tầng, giao thức phần cứng & lưu trữ
│   │   ├── Adapters/                  # Ping, SNMP, Modbus TCP, RTSP NVR, Fire Panel, Wiegand Adapters
│   │   │                              # (Tích hợp Timeout, Retry Backoff, Cancellation Token)
│   │   ├── Persistence/               # JsonIocStorageService (Ghi tệp nguyên tử Atomic File Write)
│   │   ├── Services/                  # SubsystemHealthMonitoringService, SubsystemHealthEvaluator
│   │   └── Session/                   # OperatorSessionService (Quản lý hồ sơ cán bộ trực ban động)
│   │
│   └── IOC.App/                       # Giao diện người dùng WPF Desktop (.NET 8)
│       ├── Converters/                # Value Converters tương phản cao (ACK Status, Multi-State Badge)
│       ├── ViewModels/                # MainViewModel, 7 Subsystem ViewModels & SimulationTabViewModel
│       ├── MainWindow.xaml            # Bố cục Bento-Grid hiện đại phong cách Polikod Control Systems
│       └── App.xaml                   # Từ điển Vector Path SVG, Bộ màu SCADA công nghiệp, Flat 4px
│
├── tests/
│   └── IOC.Tests/                     # Bộ kiểm thử tự động toàn diện (117 Tests 100% PASS)
│       ├── ConcurrencyAndRefreshTests.cs          # Kiểm tra an toàn đa luồng & Semaphore Lock
│       ├── DeviceAdapterTests.cs                  # Kiểm tra kết nối Adapter, Retry, Timeout, Cancel
│       ├── StoragePersistenceTests.cs             # Kiểm tra lưu trữ bền vững & khôi phục dữ liệu lỗi
│       ├── IncidentLifecycleAndSuppressionTests.cs# Kiểm tra chặn trùng lặp lỗi & vòng đời sự cố
│       └── StartupFailureAndOperatorSessionTests.cs# Kiểm tra an toàn khởi động & chuyển đổi trực ban
│
├── bin/publish/                       # Thư mục chứa gói Release độc lập (IOC.App.exe)
├── run.bat                            # Script 1-click khởi chạy ứng dụng
└── publish.bat                        # Script 1-click đóng gói bản phát hành Release
```

---

## 2. Các Tính Năng Trọng Yếu & Cải Tiến Kỹ Thuật

### 2.1. Lớp Thiết Bị & Giao Thức (Hardware Adapters with Polling & Retry)
- **Tách bạch Abstraction**: Mọi giao thức giao tiếp thiết bị đều kế thừa từ `IDeviceAdapter<TDevice, TTelemetry>` với chính sách `AdapterRetryPolicy`:
  - **Ping Adapter (`IPingAdapter`)**: Giám sát nhịp độ mạng LAN, độ trễ ICMP (ms), tỷ lệ rớt gói.
  - **SNMP Adapter (`ISnmpAdapter`)**: Truy vấn OID tiêu chuẩn đọc tải CPU, RAM, trạng thái cổng Switch và lưu lượng byte ra/vào.
  - **Modbus TCP Adapter (`IModbusAdapter`)**: Đọc thanh ghi cảm biến Chiller HVAC, bơm nước sinh hoạt, máy phát điện Cummins 1000kVA.
  - **Camera/NVR Adapter (`ICameraNvrAdapter`)**: Kiểm tra luồng video RTSP thời gian thực, FPS, bitrate và mảng ổ cứng lưu trữ RAID.
  - **Fire Alarm Panel Adapter (`IFireAlarmPanelAdapter`)**: Giám sát mạch vòng Class A tủ Hochiki FireNET Plus, điện áp ắc quy dự phòng và 77 cảm biến khói/nhiệt địa chỉ.
  - **Access Control Adapter (`IAccessControlAdapter`)**: Kiểm tra tín hiệu Wiegand, tình trạng khóa điện và báo động cạy phá cửa (tamper/door forced).
- **Cơ chế Chống Treo**: Toàn bộ lời gọi được bọc `CancellationTokenSource` với Timeout xác định, tự động thử lại (Retry Backoff) và phản hồi rõ ràng `DeviceHealthState` (`Online`, `Warning`, `Critical`, `Offline`, `Timeout`).

### 2.2. Xử Lý Bất Đồng Bộ & Chống Nghẽn Đồng Thời (Concurrency Safety)
- **Triệt tiêu `async void`**: 100% các phương thức bất đồng bộ được chuẩn hóa trả về `Task`, khởi tạo ứng dụng an toàn tại `App.xaml.cs`.
- **Chống Refresh Đồng Thời (`_refreshLock`)**: Áp dụng `SemaphoreSlim(1, 1)` tại `RefreshAllAsync()`. Khi người dùng bấm liên tục phím `F5` hoặc nhiều tác vụ kích hoạt cùng lúc, hệ thống sẽ bỏ qua các lệnh trùng lặp thay vì sinh ra xung đột dữ liệu (race conditions).
- **Bắt Lỗi Tại Biên (Boundary Exception Handling)**: Mọi thao tác ngoại vi đều được bao bọc `try-catch`, ghi log vào `ioc_crash.log` và hiển thị trạng thái nhẹ nhàng trên thanh Telemetry thay vì làm sập ứng dụng.

### 2.3. Lưu Trữ Bền Vững & Quản Lý Cán Bộ Trực Ban Động (Persistence & Audit)
- **Lưu Trữ Nguyên Tử (`JsonIocStorageService`)**: Tự động lưu thiết bị, sự cố và lịch sử tiếp nhận. Kỹ thuật ghi tệp tạm thời `.tmp` sau đó thay thế nguyên tử (`File.Replace`/`File.Move`) ngăn chặn hoàn toàn nguy cơ hỏng tệp khi mất điện đột ngột.
- **Tự Phục Hồi Dữ Liệu**: Nếu tệp dữ liệu lưu trữ bị lỗi cấu trúc, hệ thống tự động phát hiện, nạp dữ liệu mặc định an toàn và không gây crash.
- **Nhật Ký Kiểm Toán Tác Vụ (Audit Logs)**: Mọi thao tác kích hoạt chẩn đoán, tiếp nhận sự cố (ACK), khắc phục sự cố (Resolve) và lệnh nguy hiểm đều được ghi nhận đầy đủ với Mã cán bộ, Tên, Ca trực và Mốc thời gian.
- **Quản Lý Phiên Trực Động (`OperatorSessionService`)**: Xóa bỏ hoàn toàn định danh cán bộ cố định (hard-code). Hỗ trợ chuyển đổi linh hoạt giữa các cán bộ trực ban (`OP-8821`, `OP-8822`, `OP-8823`, `ADMIN-01`).

### 2.4. Công Thái Học Vận Hành & Giao Diện IOC Chuyên Nghiệp
- **Trung Tâm Triage Tiếp Nhận Sự Cố**: Bảng phân loại sự cố với 4 chế độ lọc (`Tất Cả` | `Cần Xử Lý` | `Đang Xử Lý` | `Đã Khắc Phục`). Sửa triệt để lỗi màu trạng thái ACK tàng hình bằng bộ converter tương phản cao (`#ECFDF5` / `#FEE2E2`).
- **Khử Trùng Lặp Cảnh Báo (Duplicate Incident Suppression)**: Kích hoạt cùng một sự cố nhiều lần sẽ chỉ cập nhật mốc thời gian của sự cố hiện có chứ không sinh thêm các bản ghi rác.
- **Khung Điều Khiển An Toàn Cấp 2 (Two-Step Safety Confirmation)**: Khi thực thi các lệnh nguy hiểm (Xả cửa khẩn cấp toàn tòa nhà, Lockdown, Test máy phát điện, Bật quạt hút khói tầng hầm B2), hộp thoại bắt buộc cán bộ trực ban phải tích chọn checkbox xác nhận trách nhiệm pháp lý trước khi nút bấm được kích hoạt.
- **Sidebar Thu Gọn 64px Sắc Nét**: Badge đếm sự cố tự động neo dạng Mini-Overlay ở góc icon và giới hạn hiển thị `99+`, không làm tràn thanh điều hướng.
- **Sleek Consolidated KPI Capsule**: Gộp 5 thẻ KPI rời rạc thành 1 cụm bo góc 14px thanh mảnh (~320px), hiển thị hoàn hảo trên các màn hình từ 1100px đến 4K.
- **Chế Độ Ca Trực Đêm (Night Shift Dark Mode)**: Chuyển đổi một chạm giữa giao diện sáng và giao diện tối sâu SCADA (`#0B0F19`), giảm mỏi mắt cho kíp trực đêm.
- **Kiosk / Video Wall Mode (F11)**: Phóng to toàn màn hình không viền chuyên dụng cho trung tâm màn hình ghép Video Wall.

---

## 3. Danh Mục 8 Phân Hệ Điều Hành (Tabs)

| Tab | Tên Phân Hệ | Chức Năng Giám Sát Trọng Yếu |
| :---: | :--- | :--- |
| **F1** | **Tổng Quan (Dashboard)** | 5 thẻ Bento-Grid: Cảnh báo Triage, Sức khỏe 6 phân hệ, Biểu đồ In/Out Uptime, Nhật ký điều hành, Bảng cảm biến trọng yếu. |
| **F2** | **Mạng LAN & Viễn Thông** | Giám sát Core Switch, Edge Switch, Router, tổng đài IP-PBX, tình trạng cổng mạng Up/Down, tải CPU và nhiệt độ. |
| **F3** | **Âm Thanh Thông Báo PA** | Kiểm soát tủ Bosch Praesideo, 4 vùng loa (Sảnh, Tầng 2-10, Hầm, Sân vườn), công suất âm ly và trở kháng loa. |
| **F4** | **Camera An Ninh CCTV** | Giám sát 8 camera IP, phân tích luồng RTSP, FPS, Bitrate, cảnh báo mất hình (Video Loss) và mảng đĩa NVR RAID. |
| **F5** | **Kiểm Soát Cửa & Thang Máy** | Trạng thái cửa Access Control, phát hiện cạy cửa, chèn cửa, điện áp ắc quy dự phòng và điều khiển buồng thang máy. |
| **F6** | **Cơ Điện BMS Tòa Nhà** | Chiller trung tâm Trane, bơm nước sinh hoạt, trạm biến áp, máy phát Cummins 1000kVA, cảm biến ngập hầm và rò rỉ khí độc. |
| **F7** | **Báo Cháy & PCCC** | Tủ Hochiki FireNET 4-Loop Class A, điện áp 24VDC, sơ đồ bố trí cảm biến và định vị đứt cáp mạch vòng. |
| **F8** | **Mô Phỏng & Diễn Tập Sự Cố** | Bảng điều khiển kịch bản giả lập sự cố riêng biệt cho 6 phân hệ và nút khôi phục hệ thống (`CLEAR_ALL`). |

---

## 4. Hướng Dẫn Cài Đặt & Vận Hành

### Yêu Cầu Hệ Thống
- Hệ điều hành: Windows 10 / Windows 11 (x64)
- Môi trường phát triển: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Phần mềm: Visual Studio 2022 / JetBrains Rider / Google Antigravity IDE

### Khởi Chạy Từ Mã Nguồn (Development Mode)
```powershell
# Di chuyển vào thư mục dự án
cd "c:\Users\ShinraX\Desktop\IOC management software"

# Khôi phục dependencies và chạy trực tiếp
dotnet run --project src/IOC.App
```

### Chạy Kiểm Thử Toàn Diện (Automated Test Suite)
```powershell
# Chạy 117 bài kiểm thử đơn vị, kiểm thử tích hợp và concurrency
dotnet test --verbosity normal
```

### Đóng Gói File .exe Độc Lập (Single-File Production Release)
```powershell
# Sử dụng script đóng gói tự động
./publish.bat

# Hoặc thông qua .NET CLI:
dotnet publish src/IOC.App/IOC.App.csproj -c Release -o bin/publish/
```
File thực thi `.exe` độc lập sẽ sẵn sàng tại `bin/publish/IOC.App.exe` để triển khai lên trạm trực IOC mà không cần cài đặt thêm thư viện phụ thuộc.

---

## 5. Quy Trình Tích Hợp Liên Tục (CI/CD Pipeline)

Dự án thiết lập quy trình kiểm chuẩn tự động hóa trên GitHub Actions thông qua `.github/workflows/ci.yml`:
1. **Restore**: Khôi phục toàn bộ các gói NuGet.
2. **Build with `-warnaserror`**: Biên dịch mã nguồn với quy chuẩn nghiêm ngặt nhất (coi mọi warning là lỗi).
3. **Execute Test Suite**: Chạy toàn bộ 117 bài test và thu thập báo cáo độ phủ mã nguồn (Code Coverage).
4. **Artifact Publishing**: Tự động lưu trữ báo cáo kiểm thử để đội ngũ kỹ thuật theo dõi chất lượng.
