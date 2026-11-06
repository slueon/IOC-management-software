using System.IO;
using IOC.App.ViewModels;
using IOC.Infrastructure.Persistence;
using IOC.Infrastructure.Services;
using IOC.Infrastructure.Session;
using Xunit;

namespace IOC.Tests;

public class StartupFailureAndOperatorSessionTests
{
    [Fact]
    public void OperatorSession_DefaultOperator_IsCorrect()
    {
        var session = new OperatorSessionService();
        Assert.NotNull(session.CurrentOperator);
        Assert.Equal("OP-8821", session.CurrentOperator.OperatorId);
        Assert.Contains("Nguyễn Văn An", session.CurrentOperator.FullName);
        Assert.NotEmpty(session.AvailableOperators);
    }

    [Fact]
    public void OperatorSession_SwitchOperator_UpdatesAndFiresEvent()
    {
        var session = new OperatorSessionService();
        bool eventFired = false;
        session.CurrentOperatorChanged += op =>
        {
            if (op.OperatorId == "OP-8822") eventFired = true;
        };

        session.SetCurrentOperator("OP-8822");

        Assert.True(eventFired);
        Assert.Equal("OP-8822", session.CurrentOperator.OperatorId);
        Assert.Contains("Trần Thị Mai", session.CurrentOperator.FullName);
    }

    [Fact]
    public void MainViewModel_SwitchOperator_UpdatesOperatorIdentity()
    {
        var evaluator = new SubsystemHealthEvaluator();
        var service = new SubsystemHealthMonitoringService(evaluator);
        var session = new OperatorSessionService();
        var vm = new MainViewModel(service, session);

        Assert.Contains("OP-8821", vm.OperatorIdentity);

        vm.SwitchOperator("OP-8823");

        Assert.Contains("OP-8823", vm.OperatorIdentity);
        Assert.Contains("Lê Hoàng Nam", vm.OperatorIdentity);
    }

    [Fact]
    public async Task MonitoringService_StartupWithCorruptedStorage_RecoversSafely()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "ioc_corrupted_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        try
        {
            // Write corrupt JSON to active_incidents.json
            await File.WriteAllTextAsync(Path.Combine(testDir, "active_incidents.json"), "CORRUPTED CONTENT @@@@");

            var storage = new JsonIocStorageService(testDir);
            var session = new OperatorSessionService();
            var evaluator = new SubsystemHealthEvaluator();
            var service = new SubsystemHealthMonitoringService(evaluator, storage, session);

            // Must initialize without throwing JsonException
            var exception = await Record.ExceptionAsync(() => service.InitializeAsync());
            Assert.Null(exception);

            var summary = await service.GetOverallSummaryAsync();
            Assert.NotNull(summary);
            Assert.True(summary.TotalDevices > 0);
        }
        finally
        {
            try { Directory.Delete(testDir, true); } catch { }
        }
    }
}
