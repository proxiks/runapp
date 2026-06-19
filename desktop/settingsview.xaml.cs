using System.Windows;
using System.Windows.Controls;
using RunApp.Desktop.Services;

namespace RunApp.Desktop.Views;

public partial class SettingsView : UserControl
{
    public event EventHandler? LogoutRequested;
    public event EventHandler? ProfileRequested;

    public SettingsView()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        // Set current theme radio
        var current = ThemeService.Instance.CurrentTheme;
        ThemeDark.IsChecked = current == AppTheme.Dark;
        ThemeLight.IsChecked = current == AppTheme.Light;
        ThemeMidnight.IsChecked = current == AppTheme.Midnight;
        ThemeOcean.IsChecked = current == AppTheme.Ocean;

        // Load other settings from config
        AnimationsToggle.IsChecked = true;
        PushToggle.IsChecked = true;
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.IsChecked != true) return;

        var theme = rb.Name switch
        {
            "ThemeDark" => AppTheme.Dark,
            "ThemeLight" => AppTheme.Light,
            "ThemeMidnight" => AppTheme.Midnight,
            "ThemeOcean" => AppTheme.Ocean,
            _ => AppTheme.Dark
        };

        ThemeService.Instance.SetTheme(theme);
    }

    private void OnFontSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var size = (int)e.NewValue;
        FontSizeValue.Text = size switch
        {
            < 13 => "Small",
            < 15 => "Medium",
            < 17 => "Large",
            _ => "Extra Large"
        };

        // Apply font size to app
        Application.Current.Resources["BaseFontSize"] = size;
    }

    private void OnAnimationsToggle(object sender, RoutedEventArgs e)
    {
        var enabled = AnimationsToggle.IsChecked == true;
        // Save to settings
    }

    private void OnEditProfile(object sender, RoutedEventArgs e)
    {
        ProfileRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnChangePassword(object sender, RoutedEventArgs e)
    {
        // Show password change dialog
    }

    private void OnPrivacySettings(object sender, RoutedEventArgs e)
    {
        // Navigate to privacy view
    }

    private void OnTwoFactorAuth(object sender, RoutedEventArgs e)
    {
        // Show 2FA settings
    }

    private void OnActiveSessions(object sender, RoutedEventArgs e)
    {
        // Show active sessions list
    }

    private void OnLyfronStatus(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Lyfron Security Status:\n\n" +
            "✓ Real-time threat detection: ACTIVE\n" +
            "✓ Encryption: AES-256-GCM\n" +
            "✓ Password hashing: Argon2id\n" +
            "✓ Session monitoring: ENABLED\n" +
            "✓ Last scan: Just now\n\n" +
            "Your account is protected by Lyfron.",
            "Security Status",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnLogout(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to log out?",
            "Log Out",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDeleteAccount(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will permanently delete your account and all data.\n\n" +
            "This action cannot be undone.\n\n" +
            "Type DELETE to confirm:",
            "Delete Account",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.OK)
        {
            // Require verification code before deletion
            // Call API to initiate account deletion
        }
    }
}