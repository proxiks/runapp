using Microsoft.AspNetCore.SignalR;
using RunApp.Server.Hubs;

namespace RunApp.Server.Services;

public interface INotificationService
{
    Task SendToUser(int userId, string title, string message, string? deepLink = null);
    Task SendToUsers(List<int> userIds, string title, string message);
    Task Broadcast(string title, string message);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        IHubContext<NotificationHub> hub,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task SendToUser(int userId, string title, string message, string? deepLink = null)
    {
        // Save to database
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            DeepLink = deepLink,
            Type = ParseType(title),
            IsRead = false
        };
        
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Send real-time via SignalR
        await _hub.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title,
                message,
                deepLink,
                createdAt = notification.CreatedAt,
                type = notification.Type.ToString()
            });

        _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
    }

    public async Task SendToUsers(List<int> userIds, string title, string message)
    {
        foreach (var userId in userIds)
        {
            await SendToUser(userId, title, message);
        }
    }

    public async Task Broadcast(string title, string message)
    {
        await _hub.Clients.All.SendAsync("Broadcast", new
        {
            title,
            message,
            createdAt = DateTime.UtcNow
        });
    }

    private NotificationType ParseType(string title)
    {
        if (title.Contains("like", StringComparison.OrdinalIgnoreCase)) return NotificationType.Like;
        if (title.Contains("comment", StringComparison.OrdinalIgnoreCase)) return NotificationType.Comment;
        if (title.Contains("follow", StringComparison.OrdinalIgnoreCase)) return NotificationType.Follow;
        if (title.Contains("mention", StringComparison.OrdinalIgnoreCase)) return NotificationType.Mention;
        if (title.Contains("verified", StringComparison.OrdinalIgnoreCase)) return NotificationType.Verified;
        return NotificationType.System;
    }
}