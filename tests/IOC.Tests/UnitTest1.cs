using IOC.App.Converters;
using IOC.Core.Enums;
using IOC.Infrastructure.Services;
using System.Windows.Media;
using Xunit;

namespace IOC.Tests;

public class SystemIntegrityAuditTests
{
    [Fact]
    public async Task Audit_ClearAllFaults_ResetsEntireSystemToHealthyState()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        // 1. Kích hoạt hàng loạt sự cố trên nhiều phân hệ khác nhau
        await service.TriggerDiagnosticFaultAsync("SW-CORE-01", "PORT_DOWN");
        await service.TriggerDiagnosticFaultAsync("FAS-PANEL-01", "FIRE_ALARM");
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "PUMP_TRIP"); // Smart route to BMS-PUMP-STATION
        await service.TriggerDiagnosticFaultAsync("CAM-S101", "NVR_BAD_SECTOR"); // Smart route to NVR

        var activeBefore = await service.GetActiveIncidentsAsync();
        Assert.NotEmpty(activeBefore);
        Assert.True(activeBefore.Count >= 4);

        // 2. Kích hoạt CLEAR_ALL
        await service.TriggerDiagnosticFaultAsync("ANY_DEVICE", "CLEAR_ALL");

        // 3. Toàn bộ sự cố phải được tự động giải quyết (auto-resolved)
        var activeAfter = await service.GetActiveIncidentsAsync();
        Assert.Empty(activeAfter);

        var overall = await service.GetOverallSummaryAsync();
        Assert.Equal(0, overall.TotalCritical);
        Assert.Equal(0, overall.TotalWarning);
        Assert.Equal(0, overall.TotalActiveIncidents);
        Assert.Equal(overall.TotalDevices, overall.TotalOnline);
    }

    [Fact]
    public async Task Audit_NormalFault_AutoResolvesActiveIncidents()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        // Kích hoạt lỗi quá nhiệt Chiller
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "CHILLER_OVERHEAT");
        var activeBefore = await service.GetActiveIncidentsAsync(SubsystemType.Bms);
        Assert.NotEmpty(activeBefore);

        // Khôi phục bình thường
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "NORMAL");
        var activeAfter = await service.GetActiveIncidentsAsync(SubsystemType.Bms);
        Assert.Empty(activeAfter);
    }

    [Fact]
    public async Task Audit_SmartRouting_DirectsNvrAndPumpFaultsToCorrectDevices()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        await service.InitializeAsync();

        // Gọi lệnh NVR lỗi ổ cứng với deviceId là Camera
        await service.TriggerDiagnosticFaultAsync("CAM-S101", "NVR_BAD_SECTOR");
        var cctvIncidents = await service.GetActiveIncidentsAsync(SubsystemType.Cctv);
        Assert.Contains(cctvIncidents, i => i.Title.Contains("Bad Sector", StringComparison.OrdinalIgnoreCase));

        // Gọi lệnh Pump Trip với deviceId là Chiller
        await service.TriggerDiagnosticFaultAsync("BMS-CHILLER-01", "PUMP_TRIP");
        var bmsIncidents = await service.GetActiveIncidentsAsync(SubsystemType.Bms);
        Assert.Contains(bmsIncidents, i => i.Title.Contains("nhảy rơ-le nhiệt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Audit_Converters_HandleIntegerCountsGracefullyWithoutCrashing()
    {
        var boolConverter = new BooleanToBrushConverter();
        var alarmBoolConverter = new AlarmBoolToBrushConverter();
        var fireAlarmConverter = new FireAlarmStatusToBrushConverter();
        var fireLoopConverter = new FireLoopStatusToBrushConverter();

        // 0 incidents / alarms must return Green brush (not crash, not return Red)
        var zeroBrush = boolConverter.Convert(0, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(zeroBrush);
        Assert.Equal(Color.FromRgb(0x38, 0xA1, 0x69), zeroBrush.Color);

        var alarmZero = alarmBoolConverter.Convert(0, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(alarmZero);
        Assert.Equal(Color.FromRgb(0x10, 0xB9, 0x81), alarmZero.Color);

        var fireZero = fireAlarmConverter.Convert(0, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(fireZero);
        Assert.Equal(Color.FromRgb(0x10, 0xB9, 0x81), fireZero.Color);

        var loopZero = fireLoopConverter.Convert(0, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(loopZero);
        Assert.Equal(Color.FromRgb(0x10, 0xB9, 0x81), loopZero.Color);

        // > 0 incidents / alarms must return Red alert brush
        var activeBrush = boolConverter.Convert(3, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(activeBrush);
        Assert.Equal(Color.FromRgb(0xE5, 0x3E, 0x3E), activeBrush.Color);

        var alarmActive = alarmBoolConverter.Convert(2, typeof(Brush), null!, null!) as SolidColorBrush;
        Assert.NotNull(alarmActive);
        Assert.Equal(Color.FromRgb(0xEF, 0x44, 0x44), alarmActive.Color);
    }
}
