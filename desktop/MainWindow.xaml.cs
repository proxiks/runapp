using System.Windows;
using System.Windows.Controls;
using RunApp.Desktop.Controls;
using RunApp.Desktop.Services;
using RunApp.Desktop.Views;

namespace RunApp.Desktop;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private readonly NotificationClient _notifications;
    private readonly FeedView _feedView;
    private readonly ReelsView _reelsView;

    public MainWindow(string token)
    {
        InitializeComponent();
        Instance = this;

        // Apply saved theme
        ThemeService.Instance.ThemeChanged += OnThemeChanged;

        // Setup notification client
        _notifications = new NotificationClient(
            token,
            OnNotificationReceived,
            () => Debug.WriteLine("Connected to notifications"));

        _ = _notifications.ConnectAsync();

        // Initialize views
        _feedView = new FeedView();
        _reelsView = new ReelsView();

        // Show feed by default
        MainContent.Content = _feedView;

        LoadContacts();
    }

    private void OnThemeChanged(AppTheme theme)
    {
        // Resources auto-update via DynamicResource
        InvalidateVisual();
    }

    // Search handlers
    private void OnSearchResult(object? sender, SearchResult result)
    {
        switch (result.Type)
        {
            case "user":
                NavigateToProfile(result.Id);
                break;
            case "reel":
                OpenReel(int.Parse(result.Id));
                break;
            case "hashtag":
                // Show hashtag feed
                break;
        }
    }

    private void OnSearchFilter(object? sender, EventArgs e)
    {
        // Show advanced search filters
    }

    // Navigation
    private void OnHomeClick(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _feedView;
    }

    private void OnReelsClick(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _reelsView;
    }

    private void OnVerifiedClick(object sender, RoutedEventArgs e)
    {
        var verifiedModal = new VerifiedModal();
        verifiedModal.ShowDialog();
    }

    private void OnSavedClick(object sender, RoutedEventArgs e)
    {
        // Show saved items
    }

    // Settings
    private void OnSettings(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private void OnSettingsBack(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Log out?", "Confirm", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            Properties.Settings.Default.AuthToken = "";
            Properties.Settings.Default.Save();
            
            var login = new LoginView();
            login.Show();
            Close();
        }
    }

    // Notifications
    private void OnNotifications(object sender, RoutedEventArgs e)
    {
        // Show notification panel
    }

    private void OnNotificationReceived(NotificationPayload payload)
    {
        Dispatcher.Invoke(() =>
        {
            // Show toast
            var toast = new NotificationToast(payload);
            toast.Show();

            // Update badge
            // Update notification list
        });
    }

    // Profile
    private void OnProfileClick(object sender, MouseButtonEventArgs e)
    {
        NavigateToProfile("current");
    }

    public void NavigateToProfile(string userId)
    {
        var profile = new ProfileView(userId);
        MainContent.Content = profile;
    }

    public void OpenReel(int reelId)
    {
        _reelsView.LoadReel(reelId);
        MainContent.Content = _reelsView;
    }

    public void NavigateToFeed()
    {
        MainContent.Content = _feedView;
    }

    public void ShowVerifiedModal()
    {
        OnVerifiedClick(this, new RoutedEventArgs());
    }

    private void LoadContacts()
    {
        // Load from API
        var contacts = new[]
        {
            new { Name = "Ananya", Status = "online" },
            new { Name = "Rahul", Status = "2h ago" },
            new { Name = "Priya", Status = "online" }
        };

        foreach (var c in contacts)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("ContactButtonStyle"),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 32, Height = 32,
                            CornerRadius = new CornerRadius(16),
                            Background = new SolidColorBrush(Color.FromRgb(24, 119, 242)),
                            Child = new TextBlock
                            {
                                Text = c.Name[0].ToString(),
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new StackPanel
                        {
                            Margin = new Thickness(10, 0, 0, 0),
                            Children =
                            {
                                new TextBlock { Text = c.Name, Foreground = (Brush)FindResource("TextPrimaryBrush") },
                                new TextBlock { Text = c.Status, FontSize = 12, Foreground = (Brush)FindResource("TextMutedBrush") }
                            }
                        }
                    }
                }
            };
            ContactsList.Children.Add(btn);
        }
    }
}