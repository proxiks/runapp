namespace RunApp.Server.Models;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty; // created, authorized, captured, failed, refunded
    public string Method { get; set; } = string.Empty; // card, upi, netbanking, wallet
    public string? Email { get; set; }
    public string? Contact { get; set; }
    public string? CardId { get; set; }
    public string? Bank { get; set; }
    public string? Wallet { get; set; }
    public string? Vpa { get; set; } // UPI ID
    public int? EmiMonths { get; set; }
    public decimal? Fee { get; set; }
    public decimal? Tax { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CapturedAt { get; set; }

    public User User { get; set; } = null!;
}