namespace RunApp.Server.Models;

public class Ad
{
    public int Id { get; set; }
    public string AdvertiserName { get; set; } = string.Empty;
    public string AdvertiserLogo { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string CtaText { get; set; } = "Learn More";
    public int Likes { get; set; }
    public int Shares { get; set; }
    public int Views { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public decimal BudgetRemaining { get; set; }
    public decimal CostPerView { get; set; } = 0.50m; // ₹0.50 per view
}
