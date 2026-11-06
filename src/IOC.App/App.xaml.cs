using System.IO;
using System.Windows;
using System.Windows.Threading;
using IOC.App.ViewModels;
using IOC.Infrastructure.Services;
using IOC.Infrastructure.Session;

namespace IOC.App;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ioc_crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        _ = InitializeApplicationAsync();
    }

    private async Task InitializeApplicationAsync()
    {
        try
        {
            var evaluator = new SubsystemHealthEvaluator();
            var monitoringService = new SubsystemHealthMonitoringService(evaluator);
            var sessionService = new OperatorSessionService();
            var mainViewModel = new MainViewModel(monitoringService, sessionService);

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            MainWindow = mainWindow;
            mainWindow.Show();
            await mainViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            LogException("Startup Exception", ex);
            MessageBox.Show($"Khởi động phần mềm gặp lỗi:\n{ex.Message}", "Lỗi Khởi Động IOC", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);
        e.Handled = true; // Prevent application from suddenly terminating
        MessageBox.Show($"Đã phát hiện lỗi ngoại lệ:\n{e.Exception.Message}\n\nXem chi tiết tại: {CrashLogPath}",
            "Cảnh Báo Lỗi Vận Hành", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException("UnhandledException", ex);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string tag, Exception ex)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}]\n{ex}\n----------------------------------------\n";
            File.AppendAllText(CrashLogPath, logMessage);
        }
        catch
        {
            // Ignore logging failures
        }
    }
}
