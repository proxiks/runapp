using System.Net.Http.Json;
using System.Text.Json;

namespace RunApp.Mobile.Services;

public interface IPaymentService
{
    Task<PaymentResult> InitiateVerifiedPayment();
    Task<PaymentResult> InitiateAdCreditPayment(decimal amount);
    Task<bool> VerifyPayment(string paymentId, string orderId, string signature);
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IHttpClientFactory factory, IAuthService auth, ILogger<PaymentService> logger)
    {
        _http = factory.CreateClient("Api");
        _auth = auth;
        _logger = logger;
    }

    public async Task<PaymentResult> InitiateVerifiedPayment()
    {
        var user = await _auth.GetCurrentUser();
        if (user == null) return new PaymentResult { Success = false, Error = "Not logged in" };

        // Create order on server
        var orderResponse = await _http.PostAsJsonAsync("payment/create-order", new
        {
            userId = user.Id,
            amount = 30000,  // ₹300 in paise
            type = "verified",
            plan = "monthly"
        });

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        if (order?.Success != true) return new PaymentResult { Success = false, Error = "Failed to create order" };

        // Open Razorpay checkout
        var payment = await OpenRazorpayCheckout(new CheckoutOptions
        {
            Key = order.KeyId,
            Amount = order.Amount,
            Currency = order.Currency,
            OrderId = order.OrderId,
            Name = "RunApp Verified",
            Description = "Monthly Verified Subscription",
            Image = "https://runapp.in/assets/logo.png",
            Prefill = new Prefill
            {
                Name = user.Name,
                Email = user.Email,
                Contact = user.Phone
            },
            Theme = new Theme { Color = "#1877F2" },
            Retry = new Retry { Enabled = true, MaxCount = 3 },
            SendSmsHash = true,
            RememberName = true,
            External = new External
            {
                Wallets = new[] { "paytm", "phonepe", "amazonpay" }
            }
        });

        if (payment.Success)
        {
            // Verify on server
            var verified = await VerifyPayment(payment.PaymentId, order.OrderId, payment.Signature);
            return new PaymentResult
            {
                Success = verified,
                PaymentId = payment.PaymentId,
                Amount = 300
            };
        }

        return new PaymentResult { Success = false, Error = payment.Error };
    }

    public async Task<PaymentResult> InitiateAdCreditPayment(decimal amount)
    {
        var user = await _auth.GetCurrentUser();
        var amountInPaise = (int)(amount * 100);

        var orderResponse = await _http.PostAsJsonAsync("payment/create-order", new
        {
            userId = user.Id,
            amount = amountInPaise,
            type = "ad_credit"
        });

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        
        var payment = await OpenRazorpayCheckout(new CheckoutOptions
        {
            Key = order.KeyId,
            Amount = order.Amount,
            Currency = order.Currency,
            OrderId = order.OrderId,
            Name = "RunApp Ads",
            Description = $"Add ₹{amount} ad credit",
            Prefill = new Prefill { Name = user.Name, Email = user.Email }
        });

        if (payment.Success)
        {
            await VerifyPayment(payment.PaymentId, order.OrderId, payment.Signature);
            return new PaymentResult { Success = true, Amount = amount };
        }

        return new PaymentResult { Success = false };
    }

    public async Task<bool> VerifyPayment(string paymentId, string orderId, string signature)
    {
        var user = await _auth.GetCurrentUser();
        
        var response = await _http.PostAsJsonAsync("payment/verify", new
        {
            userId = user.Id,
            paymentId,
            orderId,
            signature
        });

        var result = await response.Content.ReadFromJsonAsync<VerifyResponse>();
        return result?.Success == true;
    }

    private async Task<CheckoutResult> OpenRazorpayCheckout(CheckoutOptions options)
    {
        var tcs = new TaskCompletionSource<CheckoutResult>();

        // Platform-specific Razorpay implementation
#if ANDROID
        var activity = Platform.CurrentActivity;
        var co = new Com.Razorpay.Checkout(activity);
        
        var jsonOptions = JsonSerializer.Serialize(options);
        co.Open(activity, new Org.Json.JSONObject(jsonOptions));
        
        // Handle result via callback
        RazorpayPaymentCallback.RegisterCallback(result =>
        {
            if (result.Success)
                tcs.SetResult(new CheckoutResult { Success = true, PaymentId = result.PaymentId, Signature = result.Signature });
            else
                tcs.SetResult(new CheckoutResult { Success = false, Error = result.Error });
        });
#elif IOS
        // iOS implementation using Razorpay iOS SDK
        var razorpay = new RazorpayCheckout();
        razorpay.Open(options, (paymentId, signature) =>
        {
            tcs.SetResult(new CheckoutResult { Success = true, PaymentId = paymentId, Signature = signature });
        }, error =>
        {
            tcs.SetResult(new CheckoutResult { Success = false, Error = error });
        });
#else
        // Desktop fallback - open browser
        await Browser.OpenAsync($"https://checkout.razorpay.com/v1/checkout?order_id={options.OrderId}");
        tcs.SetResult(new CheckoutResult { Success = false, Error = "Use mobile app for payments" });
#endif

        return await tcs.Task;
    }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Error { get; set; }
}

public class CheckoutResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? Signature { get; set; }
    public string? Error { get; set; }
}

public class OrderResponse
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
}

public class VerifyResponse
{
    public bool Success { get; set; }
}

public class CheckoutOptions
{
    public string Key { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Name { get; set; } = "RunApp";
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public Prefill Prefill { get; set; } = new();
    public Theme Theme { get; set; } = new();
    public Retry Retry { get; set; } = new();
    public bool SendSmsHash { get; set; }
    public bool RememberName { get; set; }
    public External External { get; set; } = new();
}

public class Prefill
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
}

public class Theme
{
    public string Color { get; set; } = "#1877F2";
}

public class Retry
{
    public bool Enabled { get; set; }
    public int MaxCount { get; set; }
}

public class External
{
    public string[] Wallets { get; set; } = Array.Empty<string>();
}