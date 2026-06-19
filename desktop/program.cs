using RunApp.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ... existing services ...

// Add SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 64; // 64KB
});

// Add hosted service for push notifications
builder.Services.AddHostedService<PushNotificationService>();

var app = builder.Build();

// ... existing middleware ...

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();