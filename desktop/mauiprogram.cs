using Microsoft.Extensions.Logging;
using RunApp.Mobile.Services;

namespace RunApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        // HTTP client
        builder.Services.AddHttpClient("Api", client =>
        {
            client.BaseAddress = new Uri("https://api.runapp.in/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IPaymentService, PaymentService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();

        // Views
        builder.Services.AddTransient<VerifiedPage>();
        builder.Services.AddTransient<FeedPage>();
        builder.Services.AddTransient<ReelsPage>();

        // ViewModels
        builder.Services.AddTransient<VerifiedViewModel>();
        builder.Services.AddTransient<FeedViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}