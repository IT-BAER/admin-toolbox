using AdminToolbox.Models;
using System.Windows;
using System.Windows.Input;

namespace AdminToolbox.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(bool isLockout = false)
    {
        InitializeComponent();

        if (isLockout)
            SubtitleText.Text = "Session locked. Re-enter your password to continue.";

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

        if (PasswordBox.SecurePassword.Length == 0)
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

        // SecurePassword returns a new SecureString each call — dispose after copying
        using var secPwd = PasswordBox.SecurePassword;
        CredentialStore.Instance.Store(
            username:    username,
            domain:      domain,
            displayName: raw,
            password:    secPwd);

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
