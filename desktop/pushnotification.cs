namespace RunApp.Server.Services;

public class PushNotificationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IServiceProvider services,
        ILogger<PushNotificationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Push notification service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Process scheduled notifications
                var scheduled = await db.ScheduledNotifications
                    .Where(n => n.SendAt <= DateTime.UtcNow && !n.Sent)
                    .ToListAsync(stoppingToken);

                foreach (var item in scheduled)
                {
                    await notifications.SendToUser(
                        item.UserId, 
                        item.Title, 
                        item.Message,
                        item.DeepLink);
                    
                    item.Sent = true;
                }

                await db.SaveChangesAsync(stoppingToken);

                // Check for inactive users and send re-engagement
                var inactiveUsers = await db.Users
                    .Where(u => u.LastActive < DateTime.UtcNow.AddDays(-7))
                    .ToListAsync(stoppingToken);

                foreach (var user in inactiveUsers)
                {
                    await notifications.SendToUser(
                        user.Id,
                        "Miss you on RunApp!",
                        "See what you missed this week",
                        "runapp://feed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in push notification service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}