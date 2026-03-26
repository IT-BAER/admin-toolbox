using AdminToolbox.Models;
using AdminToolbox.Services;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace AdminToolbox.Views;

public partial class MainWindow : Window
{
    // -----------------------------------------------------------------------
    // Tray icon
    // -----------------------------------------------------------------------
    private Forms.NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        RefreshToolList();
        UserLabel.Text = $"Signed in as: {CredentialStore.Instance.DisplayName}";

        // Set window icon from the Content file (avoids XAML pack-URI resource requirement)
        var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AdminToolbox.ico");
        if (System.IO.File.Exists(icoPath))
            Icon = BitmapFrame.Create(new Uri(icoPath, UriKind.Absolute));

        InitTrayIcon();
    }

    // -----------------------------------------------------------------------
    // Tray icon initialisation
    // -----------------------------------------------------------------------
    private void InitTrayIcon()
    {
        // Load the embedded .ico from the Assets folder next to the exe
        var icoPath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "Assets", "AdminToolbox.ico");

        var icon = System.IO.File.Exists(icoPath)
            ? new Icon(icoPath)
            : SystemIcons.Application;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show Admin Toolbox",  null, (_, _) => RestoreFromTray());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Lock (clear credentials)", null, (_, _) => Dispatcher.Invoke(DoLock));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(DoExit));

        _trayIcon = new Forms.NotifyIcon
        {
            Icon             = icon,
            Text             = "Admin Toolbox",
            ContextMenuStrip = menu,
            Visible          = false
        };

        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    // -----------------------------------------------------------------------
    // Minimize / restore helpers
    // -----------------------------------------------------------------------
    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            HideToTray();
    }

    private void HideToTray()
    {
        if (_trayIcon is null) return;

        _trayIcon.Visible = true;
        Hide();
        _trayIcon.ShowBalloonTip(
            2000,
            "Admin Toolbox",
            "Running in the notification area.",
            Forms.ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        if (_trayIcon is not null)
            _trayIcon.Visible = false;

        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    // -----------------------------------------------------------------------
    // Lock / exit helpers (called from tray menu — may be on a non-UI thread)
    // -----------------------------------------------------------------------
    private void DoLock()
    {
        RestoreFromTray();
        Dispatcher.Invoke(() =>
        {
            CredentialStore.Instance.Clear();
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            App.ShowLoginWindow(isLockout: true);
            Close();
        });
    }

    private void DoExit()
    {
        Dispatcher.Invoke(() =>
        {
            CredentialStore.Instance.Clear();
            Close();
        });
    }

    // -----------------------------------------------------------------------
    // Tool detection
    // -----------------------------------------------------------------------
    private void RefreshToolList()
    {
        var tools = AdminTool.GetInstalled();
        ToolsItemsControl.ItemsSource = tools;
        StatusBar.Text = $"{tools.Count} of {AdminTool.Known.Count} RSAT tools detected.";
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshToolList();
    }

    // -----------------------------------------------------------------------
    // Smooth scrolling (lerp approach — runs at display refresh rate)
    // -----------------------------------------------------------------------
    private double _targetVerticalOffset;
    private bool _scrolling;

    private void ToolsScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        e.Handled = true;

        if (!_scrolling)
            _targetVerticalOffset = ToolsScrollViewer.VerticalOffset;

        _targetVerticalOffset -= e.Delta;
        _targetVerticalOffset = Math.Clamp(_targetVerticalOffset, 0, ToolsScrollViewer.ScrollableHeight);

        if (!_scrolling)
        {
            _scrolling = true;
            CompositionTarget.Rendering += SmoothScrollStep;
        }
    }

    private void SmoothScrollStep(object? sender, EventArgs e)
    {
        const double lerp = 0.15;          // smoothing factor (0 < lerp ≤ 1)
        const double epsilon = 0.5;        // close-enough threshold in pixels

        var current = ToolsScrollViewer.VerticalOffset;
        var next = current + (_targetVerticalOffset - current) * lerp;

        ToolsScrollViewer.ScrollToVerticalOffset(next);

        if (Math.Abs(_targetVerticalOffset - next) < epsilon)
        {
            ToolsScrollViewer.ScrollToVerticalOffset(_targetVerticalOffset);
            CompositionTarget.Rendering -= SmoothScrollStep;
            _scrolling = false;
        }
    }

    // -----------------------------------------------------------------------
    // Tool launch
    // -----------------------------------------------------------------------
    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AdminTool tool)
            LaunchTool(tool);
    }

    private void LaunchTool(AdminTool tool)
    {
        StatusBar.Text = $"Launching {tool.Name}…";
        try
        {
            ProcessLauncher.Launch(tool);
            StatusBar.Text = $"{tool.Name} launched successfully.";
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Failed to launch {tool.Name}.";
            MessageBox.Show(
                $"Could not launch {tool.Name}:\n\n{ex.Message}",
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // -----------------------------------------------------------------------
    // Lock button (title bar)
    // -----------------------------------------------------------------------
    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        CredentialStore.Instance.Clear();
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        App.ShowLoginWindow(isLockout: true);
        Close();
    }

    // -----------------------------------------------------------------------
    // Window closing — hide to tray unless we really want to exit
    // -----------------------------------------------------------------------
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Always exit and clean up
        CredentialStore.Instance.Clear();

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
