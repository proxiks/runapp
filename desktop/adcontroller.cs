using Microsoft.AspNetCore.Mvc;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdsController> _logger;

    public AdsController(AppDbContext db, ILogger<AdsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET api/ads/next — called every 10th reel
    [HttpGet("next")]
    public async Task<IActionResult> GetNextAd([FromQuery] string userId)
    {
        // Check if user is verified — no ads for verified users
        var user = await _db.Users.FindAsync(int.Parse(userId));
        if (user?.IsVerified == true)
        {
            return Ok(new { skip = true, reason = "verified_user" });
        }

        // Get active ad with budget
        var ad = await _db.Ads
            .Where(a => a.IsActive && a.BudgetRemaining > a.CostPerView)
            .OrderBy(a => a.Views) // Round-robin: show least viewed first
            .FirstOrDefaultAsync();

        if (ad == null)
        {
            return Ok(new { skip = true, reason = "no_ads_available" });
        }

        // Deduct budget
        ad.BudgetRemaining -= ad.CostPerView;
        ad.Views++;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ad {AdId} served to user {UserId}", ad.Id, userId);

        return Ok(new
        {
            id = ad.Id,
            advertiser = new
            {
                name = ad.AdvertiserName,
                logo = ad.AdvertiserLogo
            },
            headline = ad.Headline,
            description = ad.Description,
            videoUrl = ad.VideoUrl,
            thumbnailUrl = ad.ThumbnailUrl,
            websiteUrl = ad.WebsiteUrl,
            ctaText = ad.CtaText,
            likes = ad.Likes,
            shares = ad.Shares,
            isAd = true,
            label = "Sponsored"
        });
    }

    // POST api/ads/{id}/like
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikeAd(int id)
    {
        var ad = await _db.Ads.FindAsync(id);
        if (ad == null) return NotFound();

        ad.Likes++;
        await _db.SaveChangesAsync();

        return Ok(new { likes = ad.Likes, liked = true });
    }

    // POST api/ads/{id}/share
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareAd(int id)
    {
        var ad = await _db.Ads.FindAsync(id);
        if (ad == null) return NotFound();

        ad.Shares++;
        await _db.SaveChangesAsync();

        // Generate shareable link
        var shareUrl = $"https://runapp.in/ad/{id}";

        return Ok(new { shares = ad.Shares, shareUrl });
    }

    // POST api/ads/{id}/click — track website click
    [HttpPost("{id}/click")]
    public async Task<IActionResult> TrackClick(int id, [FromBody] ClickRequest req)
    {
        var ad = await _db.Ads.FindAsync(id);
        if (ad == null) return NotFound();

        // Log click for analytics
        var click = new AdClick
        {
            AdId = id,
            UserId = req.UserId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent,
            ClickedAt = DateTime.UtcNow
        };

        _db.AdClicks.Add(click);
        await _db.SaveChangesAsync();

        return Ok(new { redirectUrl = ad.WebsiteUrl });
    }

    // GET api/ads/{id} — public ad page for sharing
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAdPage(int id)
    {
        var ad = await _db.Ads.FindAsync(id);
        if (ad == null) return NotFound();

        // Return HTML for web share
        return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>{ad.Headline} — {ad.AdvertiserName}</title>
    <meta property='og:title' content='{ad.Headline}'>
    <meta property='og:description' content='{ad.Description}'>
    <meta property='og:image' content='{ad.ThumbnailUrl}'>
    <meta property='og:url' content='https://runapp.in/ad/{id}'>
    <style>
        body {{ font-family: Inter, sans-serif; background: #000; color: white; margin: 0; }}
        .video {{ width: 100%; max-width: 480px; height: 80vh; object-fit: cover; }}
        .info {{ padding: 20px; }}
        .advertiser {{ display: flex; align-items: center; gap: 12px; margin-bottom: 12px; }}
        .logo {{ width: 40px; height: 40px; border-radius: 8px; background: #1877f2; display: flex; align-items: center; justify-content: center; }}
        h1 {{ font-size: 20px; margin: 0 0 8px; }}
        p {{ color: #b0b3b8; margin: 0 0 16px; }}
        .cta {{ background: #1877f2; color: white; padding: 14px 24px; border-radius: 8px; text-decoration: none; display: inline-block; font-weight: 600; }}
        .actions {{ display: flex; gap: 16px; margin-top: 16px; }}
        .action {{ background: rgba(255,255,255,0.1); border: none; color: white; padding: 10px 20px; border-radius: 20px; cursor: pointer; }}
    </style>
</head>
<body>
    <center>
        <video src='{ad.VideoUrl}' controls autoplay loop class='video'></video>
        <div class='info'>
            <div class='advertiser'>
                <div class='logo'>{ad.AdvertiserName[0]}</div>
                <div>
                    <div style='font-weight: 600;'>{ad.AdvertiserName}</div>
                    <div style='font-size: 12px; color: #65676b;'>Sponsored</div>
                </div>
            </div>
            <h1>{ad.Headline}</h1>
            <p>{ad.Description}</p>
            <a href='{ad.WebsiteUrl}' class='cta' target='_blank'>{ad.CtaText}</a>
            <div class='actions'>
                <button class='action'>♥ {ad.Likes}</button>
                <button class='action'>↗️ Share</button>
            </div>
        </div>
    </center>
</body>
</html>", "text/html");
    }
}

public class ClickRequest
{
    public string UserId { get; set; } = string.Empty;
}