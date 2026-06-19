namespace RunApp.Server.Services;

public class VerificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        AppDbContext db,
        IEmailService email,
        ISmsService sms,
        ILogger<VerificationService> logger)
    {
        _db = db;
        _email = email;
        _sms = sms;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendCodeAsync(
        int userId, 
        string destination, 
        VerificationType type,
        bool useSms = false)
    {
        // Generate 6-digit code
        var code = new Random().Next(100000, 999999).ToString();
        
        // Save to database
        var verification = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Type = type,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };
        
        _db.VerificationCodes.Add(verification);
        await _db.SaveChangesAsync();

        // Send via email or SMS
        if (useSms && !string.IsNullOrEmpty(destination))
        {
            await _sms.SendAsync(destination, 
                $"Your RunApp verification code is: {code}. Valid for 10 minutes.");
        }
        else
        {
            await _email.SendAsync(destination, "RunApp Verification Code",
                $@"
                <html>
                <body style='font-family: Inter, sans-serif; background: #f0f2f5; padding: 40px;'>
                    <div style='max-width: 400px; margin: 0 auto; background: white; border-radius: 12px; padding: 32px;'>
                        <h2 style='color: #1877f2;'>⚡ RunApp</h2>
                        <p>Your verification code is:</p>
                        <div style='font-size: 32px; font-weight: 800; letter-spacing: 8px; 
                                    background: #f0f2f5; padding: 16px; border-radius: 8px; 
                                    text-align: center; margin: 20px 0;'>
                            {code}
                        </div>
                        <p style='color: #65676b; font-size: 14px;'>
                            This code expires in 10 minutes. If you didn't request this, ignore this email.
                        </p>
                    </div>
                </body>
                </html>");
        }

        _logger.LogInformation("Code sent to {Destination}", destination);
        return code;
    }

    public async Task<bool> VerifyCodeAsync(int userId, string code, VerificationType type)
    {
        var verification = await _db.VerificationCodes
            .Where(v => v.UserId == userId 
                     && v.Code == code 
                     && v.Type == type 
                     && !v.IsUsed 
                     && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification == null) return false;

        verification.IsUsed = true;
        await _db.SaveChangesAsync();
        return true;
    }
}