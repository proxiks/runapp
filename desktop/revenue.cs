public class RevenueCalculator
{
    public RevenueProjection Calculate(int totalUsers, decimal verifiedConversionRate, decimal adCpv)
    {
        var dau = (int)(totalUsers * 0.30); // 30% DAU
        var verifiedUsers = (int)(totalUsers * verifiedConversionRate);
        var nonVerifiedUsers = totalUsers - verifiedUsers;

        // Verified revenue
        var monthlyVerifiedRevenue = verifiedUsers * 300m;
        var annualVerifiedRevenue = monthlyVerifiedRevenue * 12;

        // Ad revenue (only non-verified users see ads)
        var reelsPerUserPerDay = 20;
        var totalReelsPerDay = nonVerifiedUsers * reelsPerUserPerDay;
        var adSlotsPerDay = totalReelsPerDay / 10; // 1 ad per 10 reels
        var filledAds = (int)(adSlotsPerDay * 0.60m); // 60% fill rate
        var dailyAdRevenue = filledAds * adCpv;
        var annualAdRevenue = dailyAdRevenue * 365;

        return new RevenueProjection
        {
            TotalUsers = totalUsers,
            Dau = dau,
            VerifiedUsers = verifiedUsers,
            NonVerifiedUsers = nonVerifiedUsers,
            MonthlyVerifiedRevenue = monthlyVerifiedRevenue,
            AnnualVerifiedRevenue = annualVerifiedRevenue,
            DailyAdRevenue = dailyAdRevenue,
            AnnualAdRevenue = annualAdRevenue,
            TotalAnnualRevenue = annualVerifiedRevenue + annualAdRevenue,
            PlatformCut = (annualVerifiedRevenue + annualAdRevenue) * 0.30m,
            NetRevenue = (annualVerifiedRevenue + annualAdRevenue) * 0.70m
        };
    }
}

public class RevenueProjection
{
    public int TotalUsers { get; set; }
    public int Dau { get; set; }
    public int VerifiedUsers { get; set; }
    public int NonVerifiedUsers { get; set; }
    public decimal MonthlyVerifiedRevenue { get; set; }
    public decimal AnnualVerifiedRevenue { get; set; }
    public decimal DailyAdRevenue { get; set; }
    public decimal AnnualAdRevenue { get; set; }
    public decimal TotalAnnualRevenue { get; set; }
    public decimal PlatformCut { get; set; }
    public decimal NetRevenue { get; set; }
}