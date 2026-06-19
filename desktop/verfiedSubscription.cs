namespace RunApp.Server.Models;

public class VerifiedSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string PaymentId { get; set; } = string.Empty; // Razorpay payment ID
    public string OrderId { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; } = 300.00m;
    public bool IsActive => DateTime.UtcNow < ExpiresAt;

    public User User { get; set; } = null!;
}