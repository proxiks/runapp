using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace RunApp.Desktop.Views;

public partial class LoginView : Window
{
    private readonly HttpClient _http;
    private int _currentUserId;

    public LoginView()
    {
        InitializeComponent();
        _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/")
        };
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text;
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Enter email and password");
            return;
        }

        try
        {
            var response = await _http.PostAsJsonAsync("auth/login", new
            {
                email,
                password
            });

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result?.RequiresVerification == true)
            {
                _currentUserId = result.UserId;
                ShowVerificationPanel(result.Message);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void ShowVerificationPanel(string message)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        VerifyPanel.Visibility = Visibility.Visible;
        CodeSentText.Text = message;
        CodeBox.Focus();
    }

    private async void OnVerifyClick(object sender, RoutedEventArgs e)
    {
        var code = CodeBox.Text.Trim();

        try
        {
            var response = await _http.PostAsJsonAsync("auth/verify", new
            {
                userId = _currentUserId,
                code
            });

            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>();

            if (!string.IsNullOrEmpty(result?.Token))
            {
                // Save token
                Properties.Settings.Default.AuthToken = result.Token;
                Properties.Settings.Default.Save();

                // Open main app
                var mainWindow = new MainWindow(result.Token);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid code");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Verification failed: {ex.Message}");
        }
    }

    private async void OnResendCode(object sender, MouseButtonEventArgs e)
    {
        await _http.PostAsJsonAsync("auth/resend-code", new
        {
            userId = _currentUserId
        });
        MessageBox.Show("New code sent!");
    }

    private void OnBackToLogin(object sender, RoutedEventArgs e)
    {
        VerifyPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
    }

    private void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        var registerView = new RegisterView();
        registerView.ShowDialog();
    }

    private void OnForgotPassword(object sender, MouseButtonEventArgs e)
    {
        MessageBox.Show("Contact support@runapp.in");
    }
}

// Response models
public class LoginResponse
{
    public string Message { get; set; }
    public int UserId { get; set; }
    public bool RequiresVerification { get; set; }
}

public class VerifyResponse
{
    public string Token { get; set; }
    public UserInfo User { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}