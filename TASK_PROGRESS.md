# KẾ HOẠCH PHÁT TRIỂN MODULE 1: GIÁM SÁT SỨC KHỎE HẠ TẦNG ĐA PHÂN HỆ
*(MULTI-SUBSYSTEM HEALTH MONITORING - IOC MANAGEMENT SOFTWARE)*

## 1. Mục tiêu
Hiện thực hóa Module 1 theo tài liệu `Danh_sach_tinh_nang_phan_mem_IOC.docx` với kiến trúc Clean Architecture, chuẩn MVVM (CommunityToolkit.Mvvm) và giao diện WPF Tab-based: **mỗi 1 tab giám sát 1 hệ thống riêng biệt** (LAN/Tel, PA, CCTV, Access Control, BMS, Fire Alarm PCCC).

---

## 2. Danh sách Atomic Tasks (Quy trình TDD Red-Green-Refactor)

- [x] **Task 1: Core Domain Entities & Telemetry Models**
  - [x] 1.1: Tạo các Enum: `SubsystemType`, `DeviceStatus`, `AlarmSeverity`, `RaidHealthStatus`
  - [x] 1.2: Tạo Model chi tiết cho 6 phân hệ:
    - `LanTelDevice` & `SwitchPortInfo` & `PbxTrunkInfo`
    - `PublicAddressDevice` & `SpeakerZoneInfo` & `AmplifierHealthInfo`
    - `CctvDevice` & `NvrStorageInfo`
    - `AccessControlDevice` & `DoorPointInfo`
    - `BmsDevice` & `ChillerMetrics` & `PumpMetrics` & `PowerGridMetrics` & `HazardSensorMetrics`
    - `FireAlarmDevice` & `FireLoopInfo` & `FireAlarmSensorPoint`
  - [x] 1.3: Tạo Incident & Health Summary Models (`SubsystemIncident`, `SubsystemHealthSummary`, `OverallHealthSummary`)

- [x] **Task 2: Incident Detection Engine & Health Evaluator (TDD RED -> GREEN)**
  - [x] 2.1 [RED]: Viết Test Suite cho bộ đánh giá cảnh báo 6 phân hệ (`SubsystemHealthEvaluatorTests`)
  - [x] 2.2 [GREEN]: Hiện thực `SubsystemHealthEvaluator` đánh giá tự động:
    - LAN/Tel: CPU > 90%, Link Down, SIP Trunk Down
    - PA: Quá nhiệt, đứt cầu chì, chập tải đường dây loa (Impedance Fault)
    - CCTV: Video loss, mất heartbeat, NVR ổ cứng lỗi / đầy
    - Access Control: Door Forced Open, Door Held Open, Controller ắc quy yếu
    - BMS: Chiller vượt nhiệt độ/áp suất, Bơm nhảy rơ-le nhiệt, Mất pha trạm biến áp, Cạn bể ngầm/Rò khí CO
    - PCCC: Báo động cháy khói/nhiệt, Đứt cáp vòng loop, Mất điện lưới chuyển ắc quy
  - [x] 2.3 [REFACTOR]: Chuẩn hóa Clean Code, đảm bảo test pass 100%.

- [x] **Task 3: Infrastructure Health Monitoring Service (TDD RED -> GREEN)**
  - [x] 3.1 [RED]: Viết Test Suite cho `SubsystemHealthMonitoringService`
  - [x] 3.2 [GREEN]: Hiện thực `SubsystemHealthMonitoringService` và Mock/Live data provider mô phỏng thiết bị thực tế mạng LAN cho cả 6 phân hệ.
  - [x] 3.3 [REFACTOR]: Hỗ trợ chẩn đoán thời gian thực (`RefreshAsync`, `SimulateFaultAsync`, `AcknowledgeIncidentAsync`).

- [x] **Task 4: WPF MVVM Layer (ViewModels cho 6 Tabs & Overview)**
  - [x] 4.1: `MainViewModel` điều phối chuyển tab, tổng hợp số lượng lỗi, badge thông báo từng tab.
  - [x] 4.2: 6 ViewModels chuyên biệt cho từng tab:
    - `LanTelTabViewModel`
    - `PublicAddressTabViewModel`
    - `CctvTabViewModel`
    - `AccessControlTabViewModel`
    - `BmsTabViewModel`
    - `FireAlarmTabViewModel`
    - `OverviewTabViewModel`
  - [x] 4.3: Viết Unit Test cho ViewModels kiểm tra logic lọc, thống kê và chuyển đổi tab.

- [x] **Task 5: XAML Views & Tab Control Design System**
  - [x] 5.1: Thiết kế giao diện TabControl hiện đại tông màu IOC tối (Dark Theme, High-Tech Navigation).
  - [x] 5.2: Xây dựng UserControl hoặc TabItem template cho từng phân hệ với đầy đủ Card KPI, DataGrid/Bảng giám sát chi tiết, Cảnh báo trực quan.
  - [x] 5.3: Tích hợp Top Active Alarm Banner và Status Bar.

- [x] **Task 6: LAN & TEL Deep Implementation (Switch Port Monitoring & PBX SIP Trunk)**
  - [x] 6.1: Interactive 24-Port Switch Faceplate Rack UI với hiển thị trực quan LED & Port Status.
  - [x] 6.2: Cảnh báo Link Down & ngắt kết nối cáp vật lý tức thời.
  - [x] 6.3: Giám sát phần cứng: CPU (>90%), RAM, nhiệt độ bo mạch (°C), PSU.
  - [x] 6.4: Giám sát lưu lượng băng thông cổng mạng và cảnh báo nghẽn mạng cục bộ.
  - [x] 6.5: Giám sát tổng đài IP-PBX: Cảnh báo SIP Trunk Down và mất kết nối máy nhánh.

- [x] **Task 7: Code Quality & Roslyn Analyzer Diagnostics (20 Problems Cleaned)**
  - [x] 7.1: Sửa cảnh báo `RCS1146` (Use conditional access) tại kiểm tra SIP Trunk `lan.PbxTrunk?.IsSipTrunkUp == false`.
  - [x] 7.2: Sửa 12 cảnh báo `RCS1214` (Unnecessary interpolated string) loại bỏ tiền tố `$` dư thừa trên các chuỗi hằng.
  - [x] 7.3: Sửa 7 cảnh báo `CA1822` (Member does not access instance data) đánh dấu `static` cho các private helper methods trong `SubsystemHealthEvaluator` và `SubsystemHealthMonitoringService`.
  - [x] 7.4: Deterministic Verifier Gate: `dotnet build` đạt 0 Warnings, 0 Errors; 16/16 Unit Tests pass 100%.

- [x] **Task 8: Tab 2 - Âm Thanh PA Deep Implementation (TDD Red-Green-Refactor)**
  - [x] 8.1 [RED]: Viết Test Suite `PublicAddressMonitoringTests.cs` kiểm tra 3 nhóm tính năng: Nguồn AC/Ắc quy, Bộ tăng âm (quá nhiệt, quá tải, đứt cầu chì), Trở kháng đường dây loa (đứt dây Open Circuit, chập tải Short Circuit).
  - [x] 8.2 [GREEN]: Mở rộng Model `PublicAddressDevice`, `AmplifierHealthInfo`, `SpeakerZoneInfo` (thêm `SpeakerZoneFaultType`, điện áp lưới, dòng nạp ắc quy, phần trăm tải ampli).
  - [x] 8.3 [GREEN]: Nâng cấp `SubsystemHealthEvaluator` & `SubsystemHealthMonitoringService` đánh giá chi tiết sự cố nguồn, ampli và phân vùng âm thanh.
  - [x] 8.4 [UI & REFACTOR]: Nâng cấp giao diện WPF Tab 2 (`MainWindow.xaml` & `PublicAddressTabViewModel`): Dynamic KPI Cards, Amplifiers Power/Heat Bar, Speaker Zones Impedance Matrix và Diagnostic Simulation Toolbar.
  - [x] 8.5 [VERIFIER GATE]: Chạy `dotnet test` pass 100% (24/24 tests), `dotnet build` 0 cảnh báo, đóng gói Release `.exe` tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 9: Tab 3 - Camera CCTV Deep Implementation (TDD Red-Green-Refactor)**
  - [x] 9.1 [RED]: Viết Test Suite `CctvMonitoringTests.cs` kiểm tra: Heartbeat ping sống/chết camera, mất hình Video Loss / RTSP Dead, NVR dung lượng đầy / Bad Sector / RAID Degraded, và lệnh điều khiển PTZ.
  - [x] 9.2 [GREEN]: Nâng cấp Domain Models `CctvDevice` (Heartbeat latency, Bitrate, Pan/Tilt/Zoom, Floor Plan PosX/PosY, IR night vision) & `NvrStorageInfo` (BadSectorCount, PhysicalDisks).
  - [x] 9.3 [GREEN]: Nâng cấp `SubsystemHealthEvaluator` & `SubsystemHealthMonitoringService` hỗ trợ kịch bản mô phỏng CCTV và seed dữ liệu mặt bằng các tầng.
  - [x] 9.4 [UI & REFACTOR]: Nâng cấp giao diện Tab 3 (`MainWindow.xaml` & `CctvTabViewModel`): Live Camera View OSD, Floor Plan Canvas, PTZ Control Pad, và NVR Storage & RAID Gauge.
  - [x] 9.5 [VERIFIER GATE]: Kiểm thử tự động `dotnet test` pass 100% (31/31 tests), `dotnet build` 0 cảnh báo, đóng gói Release `.exe` tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 10: Tab 4 - Access Control & Elevator Deep Implementation (TDD Red-Green-Refactor)**
  - [x] 10.1 [RED]: Viết Test Suite `AccessControlMonitoringTests.cs` kiểm tra 4 yêu cầu nghiệp vụ: Cảnh báo cạy cửa cưỡng bức (`DoorForcedOpen`), chèn giữ quá giờ (`DoorHeldOpen`), mất kết nối đầu đọc thẻ (`ReaderDisconnected`), bộ điều khiển mất mạng/mất điện AC/pin yếu (<20%), và thang máy dừng khẩn cấp/quá tải/mất kết nối.
  - [x] 10.2 [GREEN]: Mở rộng Domain Models:
    - Tạo `ElevatorInfo.cs` (CurrentFloor, Direction, DoorState, SpeedMps, LoadKg, RatedLoadKg, IsEmergencyStop, IsFireServiceMode, IsAccessCardRequired).
    - Mở rộng `DoorPointInfo.cs` (FloorPlanId, PositionX, PositionY, IsConnected, ReaderStatus, LastCardNumber, LastUserName, LastSwipeTime).
    - Mở rộng `AccessControlDevice.cs` (ControllerVoltageVolt, Elevators, IsBackupBatteryActive).
  - [x] 10.3 [GREEN]: Nâng cấp `SubsystemHealthEvaluator.cs` và `SubsystemHealthMonitoringService.cs`:
    - Đánh giá sự cố cạy cửa (Major), chèn giữ cửa (Minor), mất kết nối đầu đọc (Minor), controller offline (Critical), mất nguồn điện lưới (Major), pin yếu (Minor), kẹt thang máy/dừng khẩn cấp (Critical), quá tải thang máy (Minor).
    - Bổ sung 8 kịch bản lỗi giả lập (`DOOR_FORCED`, `DOOR_HELD`, `DOOR_OFFLINE`, `CONTROLLER_OFFLINE`, `POWER_LOSS`, `BATTERY_LOW`, `ELEVATOR_EMERGENCY`, `NORMAL`).
  - [x] 10.4 [UI & REFACTOR]:
    - Hiện thực `AccessControlTabViewModel.cs`: Bộ lọc theo sơ đồ mặt bằng (All, Floor 1, Floor B1, Floor 2), quản lý cabin thang máy, lệnh đóng/mở khóa từ xa, mở khẩn cấp toàn bộ cửa tòa nhà (`EmergencyReleaseAll`) và khóa an ninh toàn tòa nhà (`LockdownAll`).
    - Nâng cấp `MainWindow.xaml`:
      * 4 Dynamic KPI Cards (Tổng cửa giám sát, Báo động cạy phá, Giám sát thang máy, Trạng thái Controllers).
      * Sơ đồ mặt bằng cửa kiến trúc kỹ thuật tương tác trực quan (Interactive Floor Plan Door Blueprint) với các Door Pins đổi màu theo trạng thái thời gian thực.
      * Thanh công cụ phản ứng nhanh: Mở khẩn cấp toàn bộ cửa (PCCC) & Khóa an ninh khẩn cấp.
      * Bảng dữ liệu chi tiết cửa (DataGrid) kèm panel thông tin quẹt thẻ và điều khiển từ xa.
      * Giàn hiển thị Cabin Thang Máy trực quan (Elevator Shaft Visualizer) với LED tầng số lớn, tốc độ di chuyển, thanh đo tải trọng và nút gọi thang/reset E-Stop.
      * Bảng giám sát điện lưới AC 220V, thanh đo ắc quy dự phòng (DC 12V) và trạng thái mạng LAN của Controller.
      * Thanh công cụ chẩn đoán giả lập 8 sự cố an ninh.
  - [x] 10.5 [VERIFIER GATE]:
    - `dotnet test`: Vượt qua 41/41 Unit Tests (10 tests mới cho Tab 4, 100% pass, 0 regression).
    - `dotnet build`: 0 cảnh báo (0 Warnings), 0 lỗi (0 Errors).
    - `dotnet publish`: Đã biên dịch và đóng gói Release tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 11: Tab 5 - BMS Cơ Điện Tòa Nhà Deep Implementation (TDD Red-Green-Refactor)**
  - [x] 11.1 [RED]: Viết Test Suite `BmsMonitoringTests.cs` kiểm tra đầy đủ 4 yêu cầu nghiệp vụ:
    - Chiller/AHU/FCU quá nhiệt nước cấp/nước hồi và áp suất vượt ngưỡng an toàn.
    - Cảnh báo sự cố máy bơm nước sinh hoạt / bơm cứu hỏa bị nhảy rơ-le nhiệt (Thermal Overload Trip).
    - Giám sát nguồn điện tổng: Mất pha, quá tải dòng điện, sụt áp trạm biến áp và cảnh báo máy phát điện dự phòng thấp dầu/pin yếu.
    - Cảnh báo cảm biến mực nước cạn bể ngầm (<15%) và rò rỉ khí gas/khí độc CO tại khu vực tầng hầm (≥50 ppm).
  - [x] 11.2 [GREEN]: Mở rộng Domain Models:
    - `BmsTelemetry.cs`: Bổ sung `AhuFcuUnitInfo` (AHU, FCU, gió cấp, gió hồi, chênh áp phin lọc), mở rộng `ChillerMetrics` (ΔT, lưu lượng, COP), `PumpMetrics` (áp lực đẩy, dòng điện A, nhiệt độ ổ bi), `PowerGridMetrics` (công suất phản kháng, tần số Hz, Cosφ), `GeneratorMetrics` (máy phát Cummins 1000kVA, dầu Diesel, ắc quy nổ, ATS status), `HazardSensorMetrics` (dung tích bể ngầm, quạt hút khói cưỡng bức).
    - `BmsDevice.cs`: Bổ sung `GeneratorMetrics? Generator`.
  - [x] 11.3 [GREEN]: Nâng cấp `SubsystemHealthEvaluator.cs` và `SubsystemHealthMonitoringService.cs`:
    - Đánh giá sự cố quá nhiệt Chiller (Critical), áp suất Chiller bất thường (Major), tắc phin lọc AHU (Minor), bơm nhảy rơ-le nhiệt (Major), mất pha điện lưới (Critical), quá tải dòng điện (Major), sụt áp (Minor), máy phát thấp dầu (Major), cạn bể ngầm (Major), rò rỉ khí độc CO/Gas hầm xe (Critical).
    - Tích hợp 10 kịch bản lỗi giả lập (`CHILLER_OVERHEAT`, `PRESSURE_FAULT`, `PUMP_TRIP`, `PHASE_LOSS`, `CURRENT_OVERLOAD`, `VOLTAGE_SAG`, `GEN_LOW_FUEL`, `SUMP_EMPTY`, `GAS_LEAK`, `NORMAL`).
  - [x] 11.4 [UI & REFACTOR]:
    - Nâng cấp `BmsTabViewModel.cs`: Quản lý danh mục thiết bị BMS, reset rơ-le nhiệt máy bơm, lệnh đề nổ chạy thử máy phát điện Cummins, dừng máy phát, điều khiển bật/tắt quạt hút gió cưỡng bức tầng hầm.
    - Nâng cấp `MainWindow.xaml`:
      * 4 Dynamic KPI Cards (Hệ thống Chiller/HVAC, Trạm bơm nước & PCCC, Trạm biến áp 3 pha & Máy phát, Môi trường & Bể ngầm).
      * Bảng thông số Chiller thời gian thực kèm ma trận giám sát dàn lạnh AHU/FCU.
      * Bảng giám sát chi tiết trạm bơm nước sinh hoạt & PCCC kèm nút Reset rơ-le nhiệt.
      * Bảng đồng hồ Volt kế/Ampe kế điện 3 pha Schneider PM8000 & Bộ điều khiển máy phát điện Cummins 1000kVA.
      * Bảng cảm biến đo mức nước bể ngầm hầm B2 và nồng độ khí CO/Gas kèm nút kích hoạt quạt hút khói cưỡng bức.
      * Thanh công cụ chẩn đoán giả lập 10 sự cố cơ điện.
  - [x] 11.5 [VERIFIER GATE]:
    - `dotnet test`: Vượt qua 51/51 Unit Tests (10 tests mới cho Tab 5 BMS, 100% pass, 0 regression).
    - `dotnet build`: 0 cảnh báo (0 Warnings), 0 lỗi (0 Errors).
    - `dotnet publish`: Đã biên dịch và đóng gói Release tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 12: Tab 6 - Báo Cháy PCCC (Fire Alarm System - Read-Only Enforcement)**
  - [x] 12.1 [RED]: Viết Test Suite `FireAlarmMonitoringTests.cs` (8 bài kiểm thử chuyên sâu) xác thực:
    - Báo cháy thời gian thực từ đầu báo khói (`SmokeDetector`), nhiệt gia tăng (`HeatDetector`), và nút ấn khẩn cấp (`ManualCallPoint`).
    - Cảnh báo đứt mạch vòng cáp tín hiệu (`Loop Open Fault` & `Loop Ground Fault`) và định vị điểm đứt giữa các địa chỉ cảm biến.
    - Cảnh báo tủ chuyển sang ắc quy dự phòng DC 24V do mất điện lưới AC 220V và cảnh báo ắc quy yếu (<20%).
    - Ràng buộc an toàn pháp lý PCCC: Khẳng định chế độ **READ-ONLY 100%**, nghiêm cấm gửi lệnh Silence/Reset vật lý từ xa qua mạng theo tiêu chuẩn TCVN 5738 & TCVN 3890.
  - [x] 12.2 [GREEN]: Mở rộng Domain Models:
    - `FireAlarmTelemetry.cs`: Bổ sung `DeviceName`, `FloorPlanId`, `PositionX`, `PositionY`, `AnalogUnit`, `AlarmTimestamp`, `SensorTypeDisplay`, `StatusText`, `FaultLocationDescription`, `IsGroundFault`.
    - `FireAlarmDevice.cs`: Bổ sung `PanelModel`, `FirmwareVersion`, `LegalComplianceNotice`, `TotalSensorPointsCount`, `BatteryLevelPercent`, `IsBatteryLow`.
  - [x] 12.3 [GREEN]: Nâng cấp `SubsystemHealthEvaluator.cs` và `SubsystemHealthMonitoringService.cs`:
    - Đánh giá sự cố báo động cháy khói/nhiệt/nút khẩn (Critical), đứt mạch vòng loop open (Major), mất nguồn điện lưới AC chuyển ắc quy (Major), ắc quy yếu (Minor).
    - Tích hợp 8 kịch bản giả lập sự cố (`FIRE_SMOKE`, `FIRE_HEAT`, `FIRE_MCP`, `LOOP_FAULT`, `POWER_LOSS`, `BATTERY_LOW`, `NORMAL`).
    - Seed danh sách thiết bị cảm biến địa chỉ đầy đủ phân bổ theo tầng và mạch vòng Loop Class A.
  - [x] 12.4 [UI & REFACTOR]:
    - Hiện thực `FireAlarmTabViewModel.cs`: Sơ đồ mặt bằng dropdown, bộ lọc cảm biến theo tầng, chọn cảm biến/mạch vòng, nút ghi nhận sự cố (`AcknowledgeFireAlarmCommand`) và nhật ký vận hành pháp lý PCCC.
    - Đăng ký Converters trong `HealthConverters.cs` & `App.xaml`: `FireAlarmStatusToBrushConverter`, `FireAlarmTypeToIconConverter`, `FireLoopStatusToBrushConverter`.
    - Nâng cấp `MainWindow.xaml`:
      * Banner pháp lý chuẩn PCCC: TCVN 5738:2021 | READ-ONLY ENFORCED.
      * 4 Dynamic KPI Cards (Tín hiệu báo cháy, Vòng cáp tín hiệu Loop Class A, Nguồn điện tủ FACP AC 220V / DC 24V, Tủ Hochiki FireNET 77 cảm biến địa chỉ).
      * Sơ đồ mặt bằng cảm biến báo cháy Blueprint real-time kèm pins đổi màu (Xanh/Đỏ/Vàng) và icon đặc thù (💨 Khói, 🌡️ Nhiệt, 🚨 Nút khẩn).
      * Bảng giám sát chi tiết đầu báo cháy địa chỉ (DataGrid) kèm giá trị đo Analog (%/ft, °C) và nút Ghi nhận sự cố.
      * Giàn giám sát Mạch vòng Class A Style 7 cách ly chống đứt và định vị vị trí đứt cáp.
      * Bộ giám sát nguồn điện lưới AC 220V và thanh đo ắc quy dự phòng DC 24V của tủ FACP.
      * Bảng chi tiết cảm biến đang chọn và trích dẫn quy định an toàn PCCC.
      * Thanh công cụ chẩn đoán giả lập 7 sự cố báo cháy PCCC.
  - [x] 12.5 [VERIFIER GATE]:
    - `dotnet test`: Vượt qua **59/59 Unit Tests** (100% pass, 0 regression).
    - `dotnet build`: **0 Warning, 0 Error**.
    - `dotnet publish`: Đã đóng gói thành công bản Release `.exe` tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 13: Full Codebase Audit & System Integrity Bug Fixing (Loop Engineering Mode 1)**
  - [x] 13.1 [AUDIT & FIX]: Tự động hóa giải quyết sự cố tồn đọng (Stale Incident Resolution Bug)
  - [x] 13.2 [AUDIT & FIX]: Định tuyến thông minh kịch bản chẩn đoán (Smart Diagnostic Fault Routing)
  - [x] 13.3 [AUDIT & FIX]: Xử lý kiểu số nguyên trong bộ chuyển đổi màu (Converter Robustness)
  - [x] 13.4 [AUDIT & FIX]: An toàn luồng & Đồng hồ thời gian thực (Thread Safety & Real-Time Clock)
  - [x] 13.5 [AUDIT & FIX]: Đồng bộ KPI âm thanh PA
  - [x] 13.6 [VERIFIER GATE]:
    - `dotnet test`: Vượt qua **62/62 Unit Tests** (100% pass, 0 regression).
    - `dotnet build`: **0 Warning, 0 Error**.
    - `dotnet publish`: Đã biên dịch và cập nhật bản thực thi Release tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 14: Tái Thiết Kế Toàn Bộ Giao Diện Theo Phong Cách Polikod Control Systems Dashboard**
  - [x] 14.1 [RESOURCE DICTIONARY]: Thiết kế lại `App.xaml` với bộ màu Polikod tím hoàng gia (`#581C87`, `#6B21A8`, `#FAF5FF`), chuẩn hóa hệ thống style cho thẻ Bento nổi bo góc mềm (`DropShadowEffect`).
  - [x] 14.2 [SIDEBAR & APP BAR]: Tái cấu trúc layout `MainWindow.xaml`: Cột điều hướng dọc bên trái (Left Navigation Rail tím) + Thanh Header trên cùng màu trắng (`DashBoards`, Search, Chuông cảnh báo kèm badge đỏ, User Avatar `D`, `Settings ⌵`) + Thanh Footer thông tin bản quyền chuẩn xác `Polimek/Polikod`.
  - [x] 14.3 [BENTO-GRID 5 CARDS]: Triển khai đúng 100% bố cục 5 thẻ Bento tại Tab 0:
    - Card 1 (Top Left): `🚨 CẢNH BÁO SỰ CỐ ĐANG KÍCH HOẠT (ACTIVE ALARMS)` (tương tự `Inside Visitors`)
    - Card 2 (Top Right): `🏢 TỔNG QUAN 6 PHÂN HỆ TÒA NHÀ (SUBSYSTEMS LIST)` (tương tự `Visitors List`)
    - Card 3 (Middle Left): `📊 In/Out Chart - Sức Khỏe & Độ Sẵn Sàng Uptime (%)` (tương tự `In/Out Chart`)
    - Card 4 (Middle Right): `⚡ Action List - Nhật Ký Điều Hành Tác Vụ` (tương tự `Action List`, dòng xen kẽ sọc hồng/tím nhạt `#FAF5FF`)
    - Card 5 (Bottom): `📋 Inside/Outside List - Thiết Bị & Cảm Biến Trọng Yếu Tòa Nhà` (tương tự `Inside/Outside List`)
  - [x] 14.4 [VIEWMODEL & DATA]: Bổ sung model `ActionLogItem`, `CriticalSensorItem` và điều phối chuyển tab linh hoạt từ thanh sidebar hoặc nút "➔ Mở Tab".
  - [x] 14.5 [VERIFIER GATE]:
    - `dotnet test`: Vượt qua **62/62 Unit Tests** (100% pass, 0 regression).
    - `dotnet build`: **0 Warning, 0 Error**.
    - `dotnet publish`: Đã biên dịch và cập nhật bản thực thi Release tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 15: Chuyên Biệt Hóa Trung Tâm Mô Phỏng & Diễn Tập Sự Cố (Dedicated Simulation & Drill Tab 7)**
  - [x] 15.1 [ANALYSIS & ARCHITECTURE]: Khảo sát toàn bộ các nút mô phỏng rải rác ở Row 2 và các thanh chẩn đoán chân trang của 6 tab (LAN, PA, CCTV, Access Control, BMS, PCCC). Thiết kế kiến trúc Tab 7 độc lập.
  - [x] 15.2 [RED]: Viết bài kiểm thử `SimulationTabViewModelTests.cs` kiểm chứng logic kích hoạt kịch bản, đếm sự cố, ghi nhật ký mô phỏng và lệnh khôi phục toàn bộ hệ thống (`CLEAR_ALL`).
  - [x] 15.3 [GREEN]: Hiện thực `SimulationTabViewModel.cs` kết nối `ISubsystemHealthMonitoringService`, quản lý nhật ký diễn tập và cung cấp RelayCommands cho 6 phân hệ + kịch bản tổng hợp.
  - [x] 15.4 [GREEN]: Tích hợp `SimulationTab` vào `MainViewModel.cs`, bổ sung index tab 7 (`🧪 MÔ PHỎNG SỰ CỐ`), cập nhật title, điều hướng.
  - [x] 15.5 [UI & REFACTOR]: 
    * Loại bỏ thanh mô phỏng Row 2 trên cùng và các thanh chẩn đoán rải rác dưới đáy các Tab 1, 2, 3, 4, 5, 6 trong `MainWindow.xaml`.
    * Thêm nút Tab 7 `🧪` trên Sidebar navigation rail.
    * Xây dựng giao diện Tab 7 chuẩn Bento-Grid hiện đại gồm: Thanh trạng thái & Khôi phục khẩn cấp, Khối diễn tập toàn tòa nhà, 6 Khối kịch bản cho 6 phân hệ, và Nhật ký mô phỏng thời gian thực.
  - [x] 15.6 [VERIFIER GATE]: Chạy `dotnet test` (vượt qua 67/67 Unit Tests 100% PASS), `dotnet build` (0 cảnh báo, 0 lỗi), xuất bản bản thực thi Release tại `bin\publish_v1\IOC.App.exe`.

- [x] **Task 16: Fix Startup ArgumentException & Kiên Cố Hóa Quy Chuẩn RelayCommand Type Safety**
  - [x] 16.1 [RCA - ROOT CAUSE ANALYSIS]:
    * **Hiện tượng**: Khi khởi động ứng dụng xuất hiện hộp thoại:
      `System.ArgumentException: Parameter "parameter" (object) cannot be of type System.String, as the command type requires an argument of type System.Int32. (Parameter 'parameter')` tại file log `ioc_crash.log`.
    * **Nguyên nhân cốt lõi**:
      Trong Tab 7 (`SimulationTabViewModel.cs`), phương thức điều hướng được khai báo là:
      `[RelayCommand] public void NavigateToSubsystem(int tabIndex)`.
      Bộ sinh mã nguồn của `CommunityToolkit.Mvvm` tự động tạo ra thuộc tính `IRelayCommand<int> NavigateToSubsystemCommand`.
      Tuy nhiên trong `MainWindow.xaml`, 6 nút chuyển nhanh đến từng phân hệ được khai báo dạng chuỗi hằng (String Literal):
      `<Button Command="{Binding NavigateToSubsystemCommand}" CommandParameter="1" .../>`
      `<Button Command="{Binding NavigateToSubsystemCommand}" CommandParameter="2" .../>` ...
      WPF XAML Parser mặc định phân tích các giá trị trong XML attribute là kiểu `System.String`.
      Khi cửa sổ `MainWindow` khởi tạo và kết nối Data Binding, WPF gọi `ICommand.CanExecute("1")`.
      `RelayCommand<int>` kiểm tra kiểu tham số, thấy `"1"` là `System.String` chứ không phải `System.Int32` nên đã ném ngoại lệ `ArgumentException`.
  - [x] 16.2 [FIX & CODE HARDENING]:
    * Sửa `SimulationTabViewModel.cs`: Chuyển tham số của `[RelayCommand]` thành `object? parameter`. Hỗ trợ ép kiểu an toàn:
      - Nếu là `int` -> invoke trực tiếp.
      - Nếu là `string` -> dùng `int.TryParse` để convert an toàn.
      - Giữ phương thức overload `public void NavigateToSubsystem(int tabIndex)` để không làm gãy các lời gọi code C# trực tiếp.
    * Kiên cố hóa thêm `OverviewTabViewModel.cs`: Chuyển `NavigateToSubsystem(object? parameter)` hỗ trợ `SubsystemType`, `string` (`Enum.TryParse`), và `int`.
  - [x] 16.3 [TEST FIRST & REGRESSION GUARD]:
    * Bổ sung bài kiểm thử `SimulationTab_NavigateToSubsystemCommand_AcceptsStringParameterFromXaml` trong `SimulationTabViewModelTests.cs`.
    * Kiểm tra thực thi với `CommandParameter="4"` (string), `2` (int), `null`, và chuỗi không hợp lệ `invalid_number`. Tất cả đều an toàn 100%, không ném `ArgumentException`.
  - [x] 16.4 [PERMANENT CODING RULES FOR FUTURE AGENTS & DEVELOPERS]:
    > [!IMPORTANT]
    > **3 NGUYÊN TẮC BẮT BUỘC KHI DÙNG [RelayCommand] VỚI WPF XAML COMMANDPARAMETER**:
    > 1. **Mặc định XAML Literal là String**: Bất kỳ tham số nào truyền từ XAML dạng thuộc tính XML không qua `{x:Static}` hoặc Value Converter đều là `System.String` (ví dụ: `CommandParameter="1"`, `CommandParameter="Floor1"`).
    > 2. **Sử dụng `object? parameter` cho các Command nhận tham số động hoặc số nguyên**:
    >    ```csharp
    >    [RelayCommand]
    >    public void MyCommand(object? parameter)
    >    {
    >        if (parameter is int val) { ... }
    >        else if (parameter is string s && int.TryParse(s, out var parsed)) { ... }
    >    }
    >    ```
    > 3. **Tuyệt đối không định nghĩa `[RelayCommand] public void MyCommand(int value)` nếu XAML truyền chuỗi literal**: CommunityToolkit.Mvvm sẽ ném `ArgumentException` ngay ở bước kiểm tra `CanExecute` khi khởi tạo View.
  - [x] 16.5 [VERIFIER GATE]:
    * `dotnet test`: Vượt qua **68/68 Unit Tests** (100% pass, 0 regression).
    * `dotnet build -warnaserror`: **0 Warning, 0 Error**.
    * `dotnet publish`: Đã đóng gói thành công bản Release sạch tại `bin/publish/IOC.App.exe` (và `bin/publish_v1/IOC.App.exe`).

- [x] **Task 17: Toàn Diện Hóa & Nâng Cấp Giao Diện Mission-Critical IOC (UX Triage, Safety Guardrails & Vector Systems)**
  - [x] 17.1 [UX TRIAGE]: Nâng cấp Card 1 tại Tab 0 thành **Trung Tâm Điều Phối Sự Cố (Incident Triage & Response Center)**:
    * Bổ sung bộ lọc trạng thái vận hành: `Tất Cả` | `🚨 Cần Xử Lý Ngay (UNACK)` | `⏳ Đang Xử Lý (ACK)` | `✓ Đã Khắc Phục`.
    * Tự động tính toán số lượng sự cố cần xử lý tức thời và sự cố đang theo dõi.
  - [x] 17.2 [ACCESSIBILITY & MULTI-MODAL CUES]:
    * Triệt tiêu sự phụ thuộc đơn thuần vào màu sắc: Kết hợp đồng thời Ký hiệu hình học (`[!]/[▲]/[i]/[✓]`) + Nhãn văn bản Severity + Khung viền tương phản cao (`BorderBrush`) + Nền Pastel chuyên biệt.
    * Hỗ trợ người dùng mù màu (Deuteranopia) và màn hình độ tương phản thấp trong phòng trực tối.
  - [x] 17.3 [EXPANDABLE SIDEBAR & VECTOR ICONS]:
    * Thay thế toàn bộ ký tự Unicode Emoji bằng **Vector Path Geometries (SVG)** sắc nét 100% ở mọi tỷ lệ DPI: `GeoDashboard`, `GeoLanNetwork`, `GeoPublicAddress`, `GeoCctvCamera`, `GeoAccessControl`, `GeoBmsEngine`, `GeoFireAlarm`, `GeoSimulationFlask`.
    * Thanh Sidebar mở rộng/thu gọn thông minh (`64px` $\leftrightarrow$ `210px`) kèm nút Hamburger Toggle.
    * Tích hợp **Badge cảnh báo đỏ thời gian thực** hiển thị trực tiếp số lượng sự cố đang kích hoạt trên từng biểu tượng phân hệ.
  - [x] 17.4 [SAFETY GUARDRAILS - TWO-STEP CONFIRMATION]:
    * Thiết lập lớp bảo vệ an toàn 2 bước (**Two-Step Safety Confirmation Modal Dialog**) cho các thao tác nguy hiểm trên phần cứng:
      - Xả khẩn cấp toàn bộ cửa tòa nhà (`EmergencyReleaseAll`)
      - Khóa an ninh toàn tòa nhà (`LockdownAll`)
      - Đề nổ máy phát điện dự phòng Cummins 1000kVA (`StartGeneratorTest`)
      - Bật quạt thông gió hút khói cưỡng bức tầng hầm B2
    * Hộp thoại nổi cảnh báo đỏ, nêu rõ hậu quả kỹ thuật và yêu cầu xác nhận chủ động của Trực ban hoặc bấm `Esc` / `Hủy Bỏ`.
  - [x] 17.5 [KEYBOARD SHORTCUTS & KIOSK / VIDEO WALL MODE]:
    * Tích hợp hệ thống phím tắt khẩn cấp: `F1` đến `F9` (chuyển nhanh 8 tab), `F5` (làm mới toàn bộ), `F11` (Bật/Tắt chế độ Toàn Màn Hình Kiosk không viền cho Video Wall), `Esc` (Hủy bỏ/Đóng modal).
  - [x] 17.6 [TELEMETRY HEARTBEAT & DATA FRESHNESS]:
    * Top Bar hiển thị tín hiệu Telemetry thời gian thực: `● Telemetry Live (8ms)` + Giờ cập nhật gần nhất (`Cập nhật lúc: HH:mm:ss`).
  - [x] 17.7 [VERIFIER GATE]:
    * `dotnet test`: Vượt qua **72/72 Unit Tests** (100% pass, 0 regression).
    * `dotnet build -warnaserror`: **0 Warning, 0 Error**.
    * `dotnet publish`: Đã đóng gói thành công bản dựng Release hoàn thiện tại `bin/publish/IOC.App.exe`.

- [x] **Task 18: Khắc Phục Triệt Để 7 Hạn Chế Giao Diện Thực Tế (UI Evaluation Fixes & Industrial SCADA Ergonomics)**
  - [x] 18.1 [P0 FIX - TRIAGE ACK STATUS DISPLAY]:
    * Khắc phục triệt để lỗi nhãn trạng thái sự cố chưa tiếp nhận bị tàng hình (`Transparent`) do dùng nhầm `TabSelectedToBrushConverter`.
    * Bổ sung bộ 3 Converter chuyên dụng: `AcknowledgementStatusToBackgroundBrushConverter` (`#ECFDF5`/`#FEE2E2`), `AcknowledgementStatusToBorderBrushConverter` (`#10B981`/`#EF4444`), `AcknowledgementStatusToTextBrushConverter` (`#065F46`/`#991B1B`).
    * Gán tham số nhãn tường minh `ConverterParameter='ĐÃ TIẾP NHẬN|CHƯA TIẾP NHẬN'` trên `BooleanToStatusTextConverter`.
  - [x] 18.2 [P1 FIX - DYNAMIC TELEMETRY & LATENCY METRICS]:
    * Triệt tiêu dữ liệu hardcode "Telemetry Live (8ms)" và "Trực Tuyến 100% | Latency < 1ms".
    * Bổ sung tính toán độ trễ động trong `MainViewModel.cs`: `TelemetryLatencyMs`, `TelemetryHeartbeatText`, `TelemetryFooterStatus` phản ánh tải hạ tầng thực tế theo thời gian thực (jitter, cảnh báo bận tải khi quá 10 sự cố).
  - [x] 18.3 [P1 FIX - BI-DIRECTIONAL TOGGLE BUTTONS]:
    * Cập nhật nút Mute còi cảnh báo và Fullscreen Kiosk phản ánh đúng 2 chiều trạng thái:
      - Đổi icon vector linh hoạt: `GeoVolumeHigh` $\leftrightarrow$ `GeoVolumeMute`, `GeoFullscreen` $\leftrightarrow$ `GeoFullscreenExit`.
      - Tooltip động: "Tắt Còi Cảnh Báo (Đang Bật)" $\leftrightarrow$ "Bật Lại Còi Cảnh Báo (Đang Tắt)", "Toàn Màn Hình / Video Wall (F11)" $\leftrightarrow$ "Thoát Toàn Màn Hình (F11)".
      - Đổi màu viền và nền active/inactive rõ ràng (`BoolToMuteBgConverter`, `BoolToKioskBgConverter`).
  - [x] 18.4 [P1 FIX - OVERFLOW-FREE SIDEBAR BADGES]:
    * Thiết kế hệ thống hiển thị badge thông minh đa trạng thái với `CollapsedBadgeVisibilityConverter` và `ExpandedBadgeVisibilityConverter`.
    * Thêm `CountToBadgeStringConverter` giới hạn `99+` chống tràn số.
    * Khi Sidebar thu gọn 64px: Badge tự động neo dạng mini-overlay ở góc trên bên phải icon vector (`Margin="0,3,4,0"`), triệt tiêu hoàn toàn hiện tượng ép ngang hoặc tràn nút.
  - [x] 18.5 [P2 FIX - SLEEK CONSOLIDATED KPI CAPSULE]:
    * Thu nhỏ thanh Header từ 5 thẻ KPI rời rạc (~550px) thành một **Sleek Consolidated KPI Capsule** (~320px) bo góc 14px với vạch chia thanh mảnh `#CBD5E1`.
    * Hoàn toàn thích ứng trên mọi độ phân giải từ 1100px đến 1366px và màn hình Ultrawide mà không bị xô lệch layout.
  - [x] 18.6 [P1 FIX - TWO-STEP SAFETY CONFIRMATION WITH OPERATOR AUDIT]:
    * Nâng cấp hộp thoại xác nhận các thao tác nguy hiểm (Xả cửa, Lockdown, Test máy phát, Quạt hút khói tầng hầm) từ 1-click thành cơ chế bảo vệ cấp 2:
      - Hộp thông tin định danh cán bộ trực ban: `OperatorIdentity` ("Nguyễn Văn An - OP-8821").
      - Mốc thời gian yêu cầu lệnh chính xác đến từng giây: `ConfirmationTimestamp`.
      - Checkbox pháp lý bắt buộc: *"Tôi đã xác minh hiện trường và chịu hoàn toàn trách nhiệm khi thực thi lệnh này."*
      - Nút Xác nhận chỉ được kích hoạt (`IsEnabled`) khi cán bộ đã tích chọn checkbox, có guard clause chặn đứng mọi lời gọi bypass.
  - [x] 18.7 [P3 FIX - INDUSTRIAL SCADA LOOK & NIGHT SHIFT MODE]:
    * Tinh chỉnh bán kính bo góc card (`PolikodCardBorderStyle`, `KpiCardStyle`) từ 8px xuống 4px cho giao diện phẳng, sắc nét công nghiệp.
    * Tích hợp Chế độ Ca Trực Đêm (Night Shift Dark Mode) với nút bấm toggle `GeoMoon` $\leftrightarrow$ `GeoSun`, chuyển đổi nền làm việc từ `#F1F3F9` sang nền tối sâu `#0B0F19` / `#1E293B` chống mỏi mắt cho trực ban đêm.
  - [x] 18.8 [DETERMINISTIC VERIFIER GATE]:
    * `dotnet test`: Vượt qua **97/97 Unit Tests** (100% pass, 0 regression, bổ sung `UiConverterTests.cs`).
    * `dotnet build -warnaserror`: **0 Warning, 0 Error**.
    * `dotnet publish`: Đã đóng gói thành công bản dựng Release tại `bin/publish/IOC.App.exe`.

- [x] **Task 19: Triển Khai 6 Hạng Mục Cải Thiện Toàn Diện (Architecture Hardening, Device Adapters, Concurrency, Persistence, CI/CD & Testing)**
  - [x] 19.1 [README & KIẾN TRÚC THẬT]: Cập nhật `README.md` khớp 100% với kiến trúc mã nguồn Clean Architecture 3 tầng thực tế, tài liệu hóa đầy đủ các Adapter giao thức, bộ lưu trữ nguyên tử, quy chuẩn vận hành và hướng dẫn CI/CD.
  - [x] 19.2 [DEVICE ADAPTERS ABSTRACTION]:
    * Xây dựng tầng Abstraction chuẩn `IDeviceAdapter<TDevice, TTelemetry>` với `AdapterRetryPolicy`, `AdapterPollResult`, `DeviceHealthState`.
    * Triển khai cụ thể các Adapter phần cứng: `PingDeviceAdapter`, `SnmpDeviceAdapter`, `ModbusDeviceAdapter`, `CameraNvrAdapter`, `FireAlarmPanelAdapter`, `AccessControlAdapter`.
    * Tích hợp xử lý Timeout với Linked CancellationToken, Retry Backoff tự động và phân loại trạng thái thiết bị (`Online`, `Warning`, `Critical`, `Offline`, `Timeout`).
  - [x] 19.3 [CONCURRENCY & ASYNC TASK]:
    * Triệt tiêu hoàn toàn `async void` trên toàn bộ giải pháp: chuẩn hóa `App.xaml.cs` (chuyển `OnStartup` thành synchronous và đóng gói async trong `InitializeApplicationAsync()`) và `MainViewModel.cs` (`UpdateTopAlertAsync()` trả về `Task`).
    * Chống Refresh đồng thời: Tích hợp `SemaphoreSlim(1, 1)` tại `RefreshAllAsync()`, chặn đứng hiện tượng race-condition khi người dùng spam phím F5 hoặc kích hoạt nhiều trigger cùng lúc.
    * Xử lý ngoại lệ tại biên (Boundary Exception Handling): bao bọc try-catch toàn bộ tác vụ ngoại vi, ghi nhật ký lỗi chi tiết và cập nhật thanh Telemetry thay vì làm sập ứng dụng.
  - [x] 19.4 [PERSISTENCE & DYNAMIC OPERATOR SESSION]:
    * Bổ sung dịch vụ lưu trữ nguyên tử `JsonIocStorageService`: tự động ghi ra tệp tạm `.tmp` rồi hoán đổi nguyên tử (`File.Replace`/`File.Move`), chống hỏng tệp khi mất nguồn đột ngột, có cơ chế bắt lỗi JSON Exception để tự phục hồi trạng thái an toàn.
    * Triển khai ghi lưu và truy vấn `AuditLogEntry`, `IncidentAcknowledgementRecord`, và danh sách sự cố kích hoạt.
    * Triệt tiêu hard-code cán bộ trực ban: xây dựng `OperatorSessionService` quản lý danh tính động (`OP-8821`, `OP-8822`, `OP-8823`, `ADMIN-01`), tự động cập nhật thẻ tên trực ban và đóng dấu người thực hiện vào nhật ký kiểm toán.
    * Khử trùng lặp sự cố (Duplicate Incident Suppression): khi cùng 1 lỗi phần cứng kích hoạt nhiều lần, hệ thống cập nhật mốc thời gian của sự cố hiện có thay vì sinh các bản ghi rác.
  - [x] 19.5 [GITHUB ACTIONS CI/CD PIPELINE]:
    * Thiết lập `.github/workflows/ci.yml` tự động kích hoạt trên các nhánh `main` và `develop`.
    * Cấu hình môi trường Windows Server với .NET 8 SDK, thực hiện chuỗi kiểm chuẩn: `dotnet restore` $\rightarrow$ `dotnet build -warnaserror` $\rightarrow$ `dotnet test --collect:"XPlat Code Coverage"` $\rightarrow$ upload artifact kết quả kiểm thử.
  - [x] 19.6 [TOÀN DIỆN HÓA KIỂM THỬ TỰ ĐỘNG - 117 TESTS]:
    * `ConcurrencyAndRefreshTests.cs`: Kiểm tra 20+ tác vụ đồng thời gọi refresh và 25+ tác vụ đồng thời kích hoạt lỗi.
    * `DeviceAdapterTests.cs`: Kiểm tra 6 loại Adapter, chính sách Timeout, Retry và Cancellation Token.
    * `StoragePersistenceTests.cs`: Kiểm tra lưu/đọc dữ liệu, thứ tự nhật ký kiểm toán và khả năng tự phục hồi khi tệp JSON bị hỏng.
    * `IncidentLifecycleAndSuppressionTests.cs`: Kiểm tra chặn trùng lặp lỗi và vòng đời sự cố (New $\rightarrow$ ACK $\rightarrow$ Resolve).
    * `StartupFailureAndOperatorSessionTests.cs`: Kiểm tra khởi động khi tệp cấu hình lỗi và chuyển đổi cán bộ trực ban.
  - [x] 19.7 [DETERMINISTIC VERIFIER GATE]:
    * `dotnet test`: Vượt qua **117/117 Tests** (100% pass, 0 regression).
    * `dotnet build -warnaserror`: **0 Warning, 0 Error**.
    * `dotnet publish`: Cập nhật bản Release độc lập hoàn thiện tại `bin/publish/IOC.App.exe`.
