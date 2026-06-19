namespace RunApp.Server.Models;

public class VerificationCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public VerificationType Type { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
}

public enum VerificationType
{
    EmailLogin,
    SmsLogin,
    PasswordReset,
    AccountDeletion
}