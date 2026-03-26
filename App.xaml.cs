using AdminToolbox.Models;
using AdminToolbox.Views;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace AdminToolbox;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _showSignal;
    private Thread? _signalListenerThread;

    private const string MutexName = "AdminToolbox_SingleInstance_IT-BAER";
    private const string EventName = "AdminToolbox_ShowExisting_IT-BAER";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running — signal it to show
            ActivateExistingInstance();
            Shutdown(0);
            return;
        }

        // Listen for activation signals from subsequent instances
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _signalListenerThread = new Thread(ListenForShowSignal) { IsBackground = true };
        _signalListenerThread.Start();

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

    private void ListenForShowSignal()
    {
        while (_showSignal != null)
        {
            try
            {
                _showSignal.WaitOne();
                Dispatcher.Invoke(() =>
                {
                    // Find the topmost visible window and activate it;
                    // if hidden to tray, show MainWindow
                    foreach (Window w in Windows)
                    {
                        if (w.IsVisible)
                        {
                            if (w.WindowState == WindowState.Minimized)
                                w.WindowState = WindowState.Normal;
                            w.Activate();
                            return;
                        }
                    }

                    // All windows hidden (tray) — find MainWindow and restore
                    foreach (Window w in Windows)
                    {
                        if (w is MainWindow main)
                        {
                            main.Show();
                            main.WindowState = WindowState.Normal;
                            main.Activate();
                            return;
                        }
                    }
                });
            }
            catch (ObjectDisposedException) { break; }
        }
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
        _showSignal?.Dispose();
        _showSignal = null;
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void ActivateExistingInstance()
    {
        // Signal the first instance to restore its window
        try
        {
            using var signal = EventWaitHandle.OpenExisting(EventName);
            signal.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // Fallback: try to find and activate via window handle
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (proc.Id != currentProcess.Id && proc.MainWindowHandle != IntPtr.Zero)
                {
                    if (IsIconic(proc.MainWindowHandle))
                        ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(proc.MainWindowHandle);
                    break;
                }
            }
        }
    }
}
