using AdminToolbox.Models;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace AdminToolbox.Views;

public partial class LoginWindow : FluentWindow
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AdminToolbox");
    private static readonly string LastUserFile = Path.Combine(SettingsDir, "lastuser.txt");

    public LoginWindow(bool isLockout = false)
    {
        InitializeComponent();

        if (isLockout)
            SubtitleText.Text = "Session locked. Re-enter your password to continue.";

        // Restore last used username
        try
        {
            if (File.Exists(LastUserFile))
                UsernameBox.Text = File.ReadAllText(LastUserFile).Trim();
        }
        catch { /* non-critical */ }

        Loaded += (_, _) =>
        {
            if (string.IsNullOrEmpty(UsernameBox.Text))
                UsernameBox.Focus();
            else
                PasswordBox.Focus();
        };
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SignIn();
    }

    private void SignInButton_Click(object sender, RoutedEventArgs e) => SignIn();

    private void SignIn()
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var raw = UsernameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            ShowError("Please enter a username.");
            return;
        }

        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            ShowError("Please enter a password.");
            return;
        }

        // Parse username / domain
        string username;
        string? domain;
        if (raw.Contains('@'))
        {
            // UPN format: user@domain — pass full UPN as username, no separate domain
            username = raw;
            domain   = null;
        }
        else if (raw.Contains('\\'))
        {
            // SAM format: DOMAIN\user
            var parts = raw.Split('\\', 2);
            domain    = parts[0];
            username  = parts[1];
        }
        else
        {
            ShowError("Enter username as user@domain or DOMAIN\\user.");
            return;
        }

        // WPF-UI's PasswordBox doesn't expose SecurePassword natively, so we must build it manually here.
        // While not fully memory-hardened at the UI edge, DPAPI still protects the password while resting in the CredentialStore.
        var secPwd = new SecureString();
        foreach (var c in PasswordBox.Password)
            secPwd.AppendChar(c);
        secPwd.MakeReadOnly();

        CredentialStore.Instance.Store(
            username:    username,
            domain:      domain,
            displayName: raw,
            password:    secPwd);

        secPwd.Dispose();

        // Save username for next session
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(LastUserFile, raw);
        }
        catch { /* non-critical */ }

        var main = new MainWindow();
        main.Show();

        // Switch shutdown mode now that the main window is open
        Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
