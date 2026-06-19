namespace RunApp.Server.Services;

public class AdService : IAdService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdService> _logger;

    public AdService(AppDbContext db, ILogger<AdService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Ad?> GetNextAd(int userId)
    {
        // Check if user is verified - skip ads
        var user = await _db.Users.FindAsync(userId);
        if (user?.IsVerified == true)
        {
            _logger.LogDebug("Skipping ad for verified user {UserId}", userId);
            return null;
        }

        // Check if user has active subscription
        var hasActiveSub = await _db.VerifiedSubscriptions
            .AnyAsync(v => v.UserId == userId && v.ExpiresAt > DateTime.UtcNow);

        if (hasActiveSub)
        {
            return null;
        }

        // Get ad with budget
        var ad = await _db.Ads
            .Where(a => a.IsActive && a.BudgetRemaining > a.CostPerView)
            .OrderBy(a => a.Views)
            .FirstOrDefaultAsync();

        if (ad == null) return null;

        // Deduct and track
        ad.BudgetRemaining -= ad.CostPerView;
        ad.Views++;
        await _db.SaveChangesAsync();

        return ad;
    }

    public async Task<RevenueReport> GetRevenueReport(DateTime start, DateTime end)
    {
        var adRevenue = await _db.AdClicks
            .Where(c => c.ClickedAt >= start && c.ClickedAt <= end)
            .SumAsync(c => c.Revenue);

        var verifiedRevenue = await _db.VerifiedSubscriptions
            .Where(v => v.StartedAt >= start && v.StartedAt <= end)
            .SumAsync(v => v.AmountPaid);

        return new RevenueReport
        {
            AdRevenue = adRevenue,
            VerifiedRevenue = verifiedRevenue,
            TotalRevenue = adRevenue + verifiedRevenue,
            AdViews = await _db.Ads.SumAsync(a => a.Views),
            VerifiedUsers = await _db.Users.CountAsync(u => u.IsVerified),
            ActiveVerifiedUsers = await _db.VerifiedSubscriptions
                .CountAsync(v => v.ExpiresAt > DateTime.UtcNow)
        };
    }
}

public class RevenueReport
{
    public decimal AdRevenue { get; set; }
    public decimal VerifiedRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int AdViews { get; set; }
    public int VerifiedUsers { get; set; }
    public int ActiveVerifiedUsers { get; set; }
}