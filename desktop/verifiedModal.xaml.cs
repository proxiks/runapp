using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;

namespace RunApp.Desktop.Views;

public partial class VerifiedModal : Window
{
    private readonly HttpClient _http;
    private string? _currentOrderId;

    public VerifiedModal()
    {
        InitializeComponent();
        _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/"),
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {Properties.Settings.Default.AuthToken}" }
            }
        };

        CheckExistingStatus();
    }

    private async void CheckExistingStatus()
    {
        try
        {
            var status = await _http.GetFromJsonAsync<VerifiedStatus>("verified/status");
            if (status?.IsVerified == true)
            {
                ShowAlreadyVerified(status);
            }
        }
        catch { /* Not verified */ }
    }

    private async void OnPayClick(object sender, RoutedEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        PayButton.IsEnabled = false;

        try
        {
            var order = await _http.PostAsJsonAsync("verified/create-order", new { })
                .ContinueWith(r => r.Result.Content.ReadFromJsonAsync<OrderResponse>()).Result;

            if (order == null) throw new Exception("Failed to create order");

            _currentOrderId = order.OrderId;

            // Open Razorpay checkout in WebView2 or browser
            var checkoutUrl = BuildCheckoutUrl(order);
            OpenRazorpayCheckout(checkoutUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Payment failed: {ex.Message}");
            LoadingOverlay.Visibility = Visibility.Collapsed;
            PayButton.IsEnabled = true;
        }
    }

    private string BuildCheckoutUrl(OrderResponse order)
    {
        // Return HTML for Razorpay checkout
        return $@"
<!DOCTYPE html>
<html>
<head>
    <script src='https://checkout.razorpay.com/v1/checkout.js'></script>
</head>
<body>
    <script>
        var options = {{
            'key': '{order.KeyId}',
            'amount': '{order.Amount}',
            'currency': '{order.Currency}',
            'name': 'RunApp',
            'description': 'Verified Monthly Subscription',
            'order_id': '{order.OrderId}',
            'handler': function(response) {{
                window.location.href = 'runapp://verified?payment_id=' + response.razorpay_payment_id 
                    + '&order_id=' + response.razorpay_order_id
                    + '&signature=' + response.razorpay_signature;
            }},
            'prefill': {{
                'name': '{order.Prefill.Name}',
                'email': '{order.Prefill.Email}'
            }},
            'theme': {{
                'color': '#1877F2'
            }}
        }};
        var rzp = new Razorpay(options);
        rzp.open();
    </script>
</body>
</html>";
    }

    private void OpenRazorpayCheckout(string html)
    {
        // Use WebView2 or open browser
        var webView = new WebView2();
        webView.CoreWebView2Initialized += (s, e) =>
        {
            webView.CoreWebView2.NavigateToString(html);
            webView.CoreWebView2.WebMessageReceived += OnWebMessage;
        };
        
        var window = new Window
        {
            Content = webView,
            Width = 500,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        window.Show();
    }

    private async void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Handle runapp:// callback
        var url = e.TryGetWebMessageAsString();
        if (url?.StartsWith("runapp://verified") == true)
        {
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
            
            var verifyRequest = new
            {
                paymentId = query["payment_id"],
                orderId = query["order_id"],
                signature = query["signature"]
            };

            var result = await _http.PostAsJsonAsync("verified/verify-payment", verifyRequest);
            var verifyResult = await result.Content.ReadFromJsonAsync<VerifyResult>();

            if (verifyResult?.Success == true)
            {
                MessageBox.Show("🎉 Welcome to RunApp Verified!");
                DialogResult = true;
                Close();
            }
        }
    }

    private void ShowAlreadyVerified(VerifiedStatus status)
    {
        BenefitsList.Children.Add(new TextBlock
        {
            Text = $"Your badge expires in {status.Subscription.DaysRemaining} days",
            Foreground = new SolidColorBrush(Color.FromRgb(66, 183, 42)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        });

        PayButton.Content = "Manage Subscription";
        PayButton.Click -= OnPayClick;
        PayButton.Click += OnManageClick;
    }

    private void OnManageClick(object sender, RoutedEventArgs e)
    {
        // Show manage subscription dialog
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class VerifiedStatus
{
    public bool IsVerified { get; set; }
    public SubscriptionInfo Subscription { get; set; } = new();
}

public class SubscriptionInfo
{
    public int DaysRemaining { get; set; }
}

public class OrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public PrefillInfo Prefill { get; set; } = new();
}

public class PrefillInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class VerifyResult
{
    public bool Success { get; set; }
}