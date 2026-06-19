namespace RunApp.Mobile.Views;

public partial class VerifiedPage : ContentPage
{
    private readonly IPaymentService _payment;
    private readonly ILogger<VerifiedPage> _logger;

    public VerifiedPage(IPaymentService payment, ILogger<VerifiedPage> logger)
    {
        InitializeComponent();
        _payment = payment;
        _logger = logger;
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        PayButton.IsEnabled = false;

        try
        {
            var result = await _payment.InitiateVerifiedPayment();

            if (result.Success)
            {
                await DisplayAlert("🎉 Welcome to Verified!", 
                    "Your blue badge is now active. Enjoy ad-free Reels!", "OK");
                
                // Navigate to profile or refresh
                await Shell.Current.GoToAsync("//profile");
            }
            else
            {
                await DisplayAlert("Payment Failed", 
                    result.Error ?? "Something went wrong. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment failed");
            await DisplayAlert("Error", "Payment could not be processed.", "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            PayButton.IsEnabled = true;
        }
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        // Check existing subscription with server
        var auth = Handler?.MauiContext?.Services.GetService<IAuthService>();
        var status = await auth?.GetVerifiedStatus();
        
        if (status?.IsVerified == true)
        {
            await DisplayAlert("Already Verified", 
                $"Your subscription is active until {status.ExpiresAt:MMM d, yyyy}", "OK");
        }
        else
        {
            await DisplayAlert("No Subscription", 
                "No active subscription found.", "OK");
        }
    }
}