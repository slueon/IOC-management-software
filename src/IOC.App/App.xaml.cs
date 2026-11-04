using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace IOC.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handler for UI thread
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Global exception handler for non-UI threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("DispatcherUnhandledException", e.Exception);
            MessageBox.Show(
                $"Đã xảy ra lỗi trong phần mềm:\n\n{e.Exception.Message}\n\nChi tiết lỗi đã được ghi vào tệp 'ioc_crash.log'.",
                "Lỗi Hệ Thống IOC",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true; // Prevent app crash if recoverable
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("UnhandledException", ex);
                MessageBox.Show(
                    $"Lỗi nghiêm trọng phần mềm:\n\n{ex.Message}\n\nXem chi tiết tại tệp 'ioc_crash.log'.",
                    "Lỗi Nghiêm Trọng IOC",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void LogError(string type, Exception ex)
        {
            try
            {
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ioc_crash.log");
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}]\n{ex}\n----------------------------------------\n";
                File.AppendAllText(logFile, message);
            }
            catch
            {
                // Ignore logging failures
            }
        }
    }
}
