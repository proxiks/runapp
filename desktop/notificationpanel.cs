using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RunApp.Desktop.Views;

public partial class NotificationPanel : UserControl
{
    private readonly HttpClient _http;
    private readonly NotificationClient _signalR;

    public NotificationPanel(string token, NotificationClient signalR)
    {
        InitializeComponent();
        _http = new HttpClient { DefaultRequestHeaders = { { "Authorization", $"Bearer {token}" } } };
        _signalR = signalR;
        
        LoadNotifications();
    }

    private async void LoadNotifications()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<NotificationResponse>("http://localhost:5000/api/notifications");
            if (response?.Notifications != null)
            {
                NotificationList.ItemsSource = response.Notifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    DeepLink = n.DeepLink,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    Icon = GetIconForType(n.Type),
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load notifications: {ex.Message}");
        }
    }

    private void OnNotificationClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is NotificationViewModel notif)
        {
            // Mark as read
            _ = _signalR.MarkAsRead(notif.Id);
            
            // Handle deep link
            if (!string.IsNullOrEmpty(notif.DeepLink))
            {
                HandleDeepLink(notif.DeepLink);
            }
            
            // Update UI
            notif.IsRead = true;
        }
    }

    private void HandleDeepLink(string deepLink)
    {
        // Parse runapp:// protocol
        if (deepLink.StartsWith("runapp://"))
        {
            var path = deepLink.Replace("runapp://", "");
            var parts = path.Split('/');
            
            switch (parts[0])
            {
                case "user":
                    // Navigate to user profile
                    MainWindow.Instance?.NavigateToProfile(parts[1]);
                    break;
                case "reel":
                    // Open reel player
                    MainWindow.Instance?.OpenReel(int.Parse(parts[1]));
                    break;
                case "feed":
                    // Navigate to feed
                    MainWindow.Instance?.NavigateToFeed();
                    break;
                case "verified":
                    // Show verified modal
                    MainWindow.Instance?.ShowVerifiedModal();
                    break;
            }
        }
        else if (deepLink.StartsWith("http"))
        {
            // Open external link in browser
            Process.Start(new ProcessStartInfo(deepLink) { UseShellExecute = true });
        }
    }

    private void OnMarkAllRead(object sender, RoutedEventArgs e)
    {
        _ = _http.PostAsync("http://localhost:5000/api/notifications/read-all", null);
        
        foreach (var item in NotificationList.ItemsSource.OfType<NotificationViewModel>())
        {
            item.IsRead = true;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }

    private static string GetIconForType(string type) => type switch
    {
        "Like" => "♥",
        "Comment" => "💬",
        "Follow" => "👤",
        "Mention" => "@",
        "Verified" => "✓",
        _ => "🔔"
    };

    private static string GetTimeAgo(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d";
        return date.ToString("MMM d");
    }
}

public class NotificationViewModel : INotifyPropertyChanged
{
    private bool _isRead;
    
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    
    public bool IsRead 
    { 
        get => _isRead; 
        set { _isRead = value; OnPropertyChanged(); }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class NotificationResponse
{
    public List<NotificationItem> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
    public bool HasMore { get; set; }
}

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}