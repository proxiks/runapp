using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RunApp.Server.Hubs;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        AppDbContext db,
        INotificationService notifications,
        ILogger<NotificationsController> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    // GET api/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.DeepLink,
                n.Type,
                n.IsRead,
                n.CreatedAt
            })
            .ToListAsync();

        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new
        {
            notifications,
            unreadCount,
            hasMore = notifications.Count == pageSize
        });
    }

    // POST api/notifications/{id}/read
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // POST api/notifications/read-all
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

        return Ok(new { success = true });
    }

    // DELETE api/notifications/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // POST api/notifications/test (admin only - sends test notification)
    [HttpPost("test")]
    [AllowAnonymous] // Remove in production
    public async Task<IActionResult> SendTest([FromBody] TestNotificationRequest req)
    {
        await _notifications.SendToUser(req.UserId, req.Title, req.Message, req.DeepLink);
        return Ok(new { sent = true });
    }
}

public record TestNotificationRequest(int UserId, string Title, string Message, string? DeepLink);