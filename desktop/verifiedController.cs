using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VerifiedController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly INotificationService _notifications;
    private readonly ILogger<VerifiedController> _logger;

    public VerifiedController(
        AppDbContext db,
        IConfiguration config,
        INotificationService notifications,
        ILogger<VerifiedController> logger)
    {
        _db = db;
        _config = config;
        _notifications = notifications;
        _logger = logger;
    }

    // GET api/verified/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        var subscription = await _db.VerifiedSubscriptions
            .Where(v => v.UserId == userId && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.ExpiresAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            isVerified = user.IsVerified,
            badgeColor = user.IsVerified ? "#1877F2" : null,
            subscription = subscription == null ? null : new
            {
                startedAt = subscription.StartedAt,
                expiresAt = subscription.ExpiresAt,
                autoRenew = subscription.AutoRenew,
                daysRemaining = (subscription.ExpiresAt - DateTime.UtcNow).Days
            },
            benefits = user.IsVerified ? new[]
            {
                "Blue verified badge",
                "No ads in Reels",
                "Priority algorithm ranking",
                "Advanced analytics",
                "Verified-only DMs",
                "24/7 priority support"
            } : null
        });
    }

    // POST api/verified/create-order
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();
        if (user.IsVerified) return BadRequest(new { error = "Already verified" });

        try
        {
            var client = new RazorpayClient(
                _config["Razorpay:KeyId"],
                _config["Razorpay:KeySecret"]);

            var options = new Dictionary<string, object>
            {
                { "amount", 30000 }, // ₹300 in paise
                { "currency", "INR" },
                { "receipt", $"verified_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" },
                { "notes", new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "type", "verified_subscription" }
                    }
                }
            };

            var order = client.Order.Create(options);

            return Ok(new
            {
                orderId = order["id"].ToString(),
                amount = 30000,
                currency = "INR",
                keyId = _config["Razorpay:KeyId"],
                prefill = new
                {
                    name = user.Name,
                    email = user.Email
                },
                theme = new { color = "#1877F2" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Razorpay order");
            return StatusCode(500, new { error = "Payment initialization failed" });
        }
    }

    // POST api/verified/verify-payment
    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        try
        {
            var client = new RazorpayClient(
                _config["Razorpay:KeyId"],
                _config["Razorpay:KeySecret"]);

            var attributes = new Dictionary<string, string>
            {
                { "razorpay_payment_id", req.PaymentId },
                { "razorpay_order_id", req.OrderId },
                { "razorpay_signature", req.Signature }
            };

            Utils.verifyPaymentSignature(attributes);

            // Payment verified - activate subscription
            var subscription = new VerifiedSubscription
            {
                UserId = userId,
                PaymentId = req.PaymentId,
                OrderId = req.OrderId,
                ExpiresAt = DateTime.UtcNow.AddMonths(1),
                AmountPaid = 300.00m
            };

            _db.VerifiedSubscriptions.Add(subscription);

            var user = await _db.Users.FindAsync(userId);
            user!.IsVerified = true;

            await _db.SaveChangesAsync();

            // Send notification
            await _notifications.SendToUser(userId, "Welcome to Verified! 🎉",
                "Your blue badge is now active. Enjoy ad-free Reels and priority ranking.",
                "runapp://verified/success");

            _logger.LogInformation("User {UserId} verified", userId);

            return Ok(new
            {
                success = true,
                verified = true,
                expiresAt = subscription.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed for user {UserId}", userId);
            return BadRequest(new { error = "Payment verification failed" });
        }
    }

    // POST api/verified/cancel
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var activeSub = await _db.VerifiedSubscriptions
            .Where(v => v.UserId == userId && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.ExpiresAt)
            .FirstOrDefaultAsync();

        if (activeSub == null) return BadRequest(new { error = "No active subscription" });

        activeSub.AutoRenew = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Auto-renew disabled. Badge expires at end of period." });
    }
}

public record VerifyPaymentRequest(string PaymentId, string OrderId, string Signature);