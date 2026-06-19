namespace RunApp.Desktop.Models;

public class AdItem
{
    public int Id { get; set; }
    public AdvertiserInfo Advertiser { get; set; } = new();
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string CtaText { get; set; } = "Learn More";
    public int Likes { get; set; }
    public int Shares { get; set; }
    public bool IsLiked { get; set; }
    public string Label { get; set; } = "Sponsored";
}

public class AdvertiserInfo
{
    public string Name { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
}