using AdminToolbox.Models;
using AdminToolbox.Views;
using System.Windows;

namespace AdminToolbox;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global handler so crashes surface as a message box instead of a silent exit
        DispatcherUnhandledException += (_, ex) =>
        {
            System.Windows.MessageBox.Show(
                $"Unhandled error:\n\n{ex.Exception.GetType().Name}: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                "Admin Toolbox — Fatal Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            ex.Handled = true;
            Shutdown(1);
        };

        // Prevent any window from auto-showing before login completes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        ShowLoginWindow();
    }

    internal static void ShowLoginWindow(bool isLockout = false)
    {
        var login = new LoginWindow(isLockout);
        login.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Zero-out credentials from memory on exit
        CredentialStore.Instance.Dispose();
        base.OnExit(e);
    }
}
