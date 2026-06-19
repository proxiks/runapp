using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(AppDbContext db, IConfiguration config, ILogger<PaymentController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // POST api/payment/create-order
    [HttpPost("create-order")]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest req)
    {
        try
        {
            var client = new RazorpayClient(
                _config["Razorpay:KeyId"], 
                _config["Razorpay:KeySecret"]);

            var options = new Dictionary<string, object>
            {
                { "amount", req.Amount },        // Amount in paise (30000 = ₹300)
                { "currency", "INR" },
                { "receipt", $"order_{req.UserId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" },
                { "payment_capture", 1 },        // Auto capture
                { "notes", new Dictionary<string, string>
                    {
                        { "user_id", req.UserId.ToString() },
                        { "type", req.Type },           // "verified", "ad_credit", etc.
                        { "plan", req.Plan ?? "monthly" }
                    }
                }
            };

            // Add transfer if splitting with partners
            if (req.Transfers != null && req.Transfers.Any())
            {
                options.Add("transfers", req.Transfers);
            }

            var order = client.Order.Create(options);

            _logger.LogInformation("Order created: {OrderId} for user {UserId}", 
                order["id"], req.UserId);

            return Ok(new
            {
                success = true,
                orderId = order["id"].ToString(),
                amount = req.Amount,
                currency = "INR",
                keyId = _config["Razorpay:KeyId"],
                notes = order["notes"]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // POST api/payment/verify
    [HttpPost("verify")]
    public IActionResult VerifyPayment([FromBody] VerifyPaymentRequest req)
    {
        try
        {
            // Verify signature
            var payload = $"{req.OrderId}|{req.PaymentId}";
            var secret = _config["Razorpay:KeySecret"];
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (generatedSignature != req.Signature)
            {
                _logger.LogWarning("Invalid payment signature");
                return BadRequest(new { success = false, error = "Invalid signature" });
            }

            // Fetch payment from Razorpay to confirm
            var client = new RazorpayClient(
                _config["Razorpay:KeyId"], 
                _config["Razorpay:KeySecret"]);
            
            var payment = client.Payment.Fetch(req.PaymentId);

            if (payment["status"].ToString() != "captured")
            {
                return BadRequest(new { success = false, error = "Payment not captured" });
            }

            // Save to database
            var transaction = new PaymentTransaction
            {
                UserId = req.UserId,
                OrderId = req.OrderId,
                PaymentId = req.PaymentId,
                Signature = req.Signature,
                Amount = decimal.Parse(payment["amount"].ToString()) / 100, // Convert paise to rupees
                Currency = "INR",
                Status = "captured",
                Method = payment["method"]?.ToString() ?? "unknown",
                Email = payment["email"]?.ToString(),
                Contact = payment["contact"]?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _db.PaymentTransactions.Add(transaction);
            _db.SaveChanges();

            _logger.LogInformation("Payment verified: {PaymentId}, Amount: ₹{Amount}", 
                req.PaymentId, transaction.Amount);

            return Ok(new
            {
                success = true,
                paymentId = req.PaymentId,
                amount = transaction.Amount,
                status = "captured"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // POST api/payment/webhook
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var webhookSecret = _config["Razorpay:WebhookSecret"];
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature)) return BadRequest();

        // Verify webhook signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        if (signature != expectedSignature)
        {
            _logger.LogWarning("Invalid webhook signature");
            return BadRequest();
        }

        var eventData = System.Text.Json.JsonDocument.Parse(payload);
        var eventType = eventData.RootElement.GetProperty("event").GetString();

        _logger.LogInformation("Webhook received: {EventType}", eventType);

        switch (eventType)
        {
            case "payment.captured":
                await HandlePaymentCaptured(eventData);
                break;
            case "subscription.charged":
                await HandleSubscriptionCharged(eventData);
                break;
            case "refund.processed":
                await HandleRefundProcessed(eventData);
                break;
        }

        return Ok();
    }

    private async Task HandlePaymentCaptured(System.Text.Json.JsonDocument eventData)
    {
        var payment = eventData.RootElement.GetProperty("payload")
            .GetProperty("payment").GetProperty("entity");
        
        var notes = payment.GetProperty("notes");
        var userId = notes.GetProperty("user_id").GetInt32();
        var type = notes.GetProperty("type").GetString();

        // Activate based on type
        switch (type)
        {
            case "verified":
                await ActivateVerified(userId, payment);
                break;
            case "ad_credit":
                await AddAdCredit(userId, payment);
                break;
        }
    }

    private async Task ActivateVerified(int userId, System.Text.Json.JsonElement payment)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        user.IsVerified = true;

        var subscription = new VerifiedSubscription
        {
            UserId = userId,
            PaymentId = payment.GetProperty("id").GetString(),
            AmountPaid = payment.GetProperty("amount").GetDecimal() / 100,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths(1),
            AutoRenew = true
        };

        _db.VerifiedSubscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Verified activated for user {UserId}", userId);
    }

    private async Task AddAdCredit(int userId, System.Text.Json.JsonElement payment)
    {
        // For advertisers adding budget
        var amount = payment.GetProperty("amount").GetDecimal() / 100;
        var advertiser = await _db.Advertisers.FirstOrDefaultAsync(a => a.UserId == userId);
        if (advertiser != null)
        {
            advertiser.Balance += amount;
            await _db.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionCharged(System.Text.Json.JsonDocument eventData)
    {
        // Handle recurring subscription payments
    }

    private async Task HandleRefundProcessed(System.Text.Json.JsonDocument eventData)
    {
        // Handle refunds
    }
}

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public int Amount { get; set; }  // In paise
    public string Type { get; set; } = "verified";
    public string? Plan { get; set; }
    public List<TransferRequest>? Transfers { get; set; }
}

public class TransferRequest
{
    public string Account { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public Dictionary<string, string> Notes { get; set; } = new();
    public List<string> LinkedAccountNotes { get; set; } = new();
    public bool OnHold { get; set; }
    public int? OnHoldUntil { get; set; }
}

public class VerifyPaymentRequest
{
    public int UserId { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}