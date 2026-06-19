using Microsoft.AspNetCore.Mvc;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("")]
public class HyperlinkController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<HyperlinkController> _logger;

    public HyperlinkController(AppDbContext db, ILogger<HyperlinkController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Deep link handler: runapp.in/u/jatin
    [HttpGet("u/{username}")]
    public async Task<IActionResult> UserProfile(string username)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Name.ToLower() == username.ToLower());

        if (user == null) return NotFound();

        // If request accepts HTML, return profile page
        if (Request.Headers.Accept.Any(h => h.Contains("text/html")))
        {
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>{user.Name} on RunApp</title>
    <meta property='og:title' content='{user.Name} on RunApp'>
    <meta property='og:description' content='Check out {user.Name} Reels on RunApp'>
    <meta name='twitter:card' content='summary'>
    <style>
        body {{ font-family: Inter, sans-serif; background: #18191A; color: white; text-align: center; padding: 40px; }}
        .avatar {{ width: 120px; height: 120px; border-radius: 50%; background: linear-gradient(135deg, #1877f2, #6b4c9a); display: flex; align-items: center; justify-content: center; font-size: 48px; margin: 0 auto 20px; }}
        .btn {{ background: #1877f2; color: white; padding: 14px 32px; border-radius: 24px; text-decoration: none; display: inline-block; margin-top: 20px; font-weight: 600; }}
    </style>
</head>
<body>
    <div class='avatar'>{user.Name[0]}</div>
    <h1>@{user.Name}</h1>
    <p>Follow {user.Name} on RunApp to see their Reels</p>
    <a href='runapp://user/{user.Id}' class='btn'>Open in RunApp</a>
    <br><br>
    <a href='https://play.google.com/store/apps/details?id=com.runapp' class='btn' style='background: #42b72a;'>Get the App</a>
</body>
</html>", "text/html");
        }

        // API response
        return Ok(new
        {
            user.Id,
            user.Name,
            user.IsVerified,
            profileUrl = $"https://runapp.in/u/{user.Name.ToLower()}"
        });
    }

    // Deep link handler: runapp.in/reel/123
    [HttpGet("reel/{id}")]
    public async Task<IActionResult> ReelPage(int id)
    {
        var reel = await _db.Reels
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reel == null) return NotFound();

        return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>Reel by {reel.User.Name}</title>
    <meta property='og:title' content='Reel by {reel.User.Name}'>
    <meta property='og:type' content='video.other'>
    <style>
        body {{ font-family: Inter, sans-serif; background: #000; color: white; margin: 0; }}
        .video-container {{ max-width: 480px; margin: 0 auto; }}
        video {{ width: 100%; height: 100vh; object-fit: cover; }}
        .overlay {{ position: fixed; bottom: 0; left: 0; right: 0; padding: 40px 20px; background: linear-gradient(to top, rgba(0,0,0,0.9), transparent); }}
        .user {{ display: flex; align-items: center; gap: 12px; }}
        .avatar {{ width: 40px; height: 40px; border-radius: 50%; background: linear-gradient(135deg, #1877f2, #6b4c9a); display: flex; align-items: center; justify-content: center; font-weight: 700; }}
        .btn {{ background: #1877f2; color: white; padding: 12px 24px; border-radius: 20px; text-decoration: none; display: inline-block; margin-top: 12px; font-weight: 600; }}
    </style>
</head>
<body>
    <div class='video-container'>
        <video src='{reel.VideoUrl}' autoplay loop playsinline></video>
        <div class='overlay'>
            <div class='user'>
                <div class='avatar'>{reel.User.Name[0]}</div>
                <div>
                    <div style='font-weight: 700;'>@{reel.User.Name}</div>
                    <div style='opacity: 0.8; font-size: 14px;'>{reel.Caption}</div>
                </div>
            </div>
            <a href='runapp://reel/{reel.Id}' class='btn'>Watch on RunApp</a>
        </div>
    </div>
</body>
</html>", "text/html");
    }

    // Short link redirect: runapp.in/s/abc123
    [HttpGet("s/{shortCode}")]
    public async Task<IActionResult> ShortLink(string shortCode)
    {
        // Look up short code in database
        var link = await _db.ShortLinks
            .FirstOrDefaultAsync(s => s.Code == shortCode);

        if (link == null) return NotFound();

        link.Clicks++;
        await _db.SaveChangesAsync();

        // If mobile, try deep link first
        var isMobile = Request.Headers.UserAgent.Any(u => 
            u.Contains("Android") || u.Contains("iPhone"));

        if (isMobile && !string.IsNullOrEmpty(link.DeepLink))
        {
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <script>
        setTimeout(function() {{
            window.location.href = '{link.DestinationUrl}';
        }}, 2000);
    </script>
</head>
<body>
    <p>Opening RunApp...</p>
    <a href='{link.DeepLink}'>Click here if nothing happens</a>
</body>
</html>", "text/html");
        }

        return Redirect(link.DestinationUrl);
    }

    // QR code generation for profiles
    [HttpGet("qr/{username}")]
    public IActionResult GenerateQr(string username)
    {
        var url = $"https://runapp.in/u/{username.ToLower()}";
        // Return QR code image (implement with QRCoder library)
        return Ok(new { qrUrl = url });
    }
}