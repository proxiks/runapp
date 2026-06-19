using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RunApp.Desktop.Controls;

public partial class SearchBar : UserControl
{
    private readonly HttpClient _http;
    private readonly DispatcherTimer _debounceTimer;
    private List<SearchResult> _allResults = new();

    public event EventHandler<SearchResult>? ResultSelected;
    public event EventHandler? FilterClicked;

    public SearchBar()
    {
        InitializeComponent();
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            await PerformSearch(SearchInput.Text);
        };
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = SearchInput.Text;
        PlaceholderText.Visibility = string.IsNullOrEmpty(text) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
        
        ClearButton.Visibility = string.IsNullOrEmpty(text) 
            ? Visibility.Collapsed 
            : Visibility.Visible;

        if (text.Length >= 2)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
        else
        {
            SearchPopup.IsOpen = false;
        }
    }

    private async Task PerformSearch(string query)
    {
        try
        {
            // Search users, reels, hashtags
            var response = await _http.GetFromJsonAsync<SearchResponse>(
                $"search?q={Uri.EscapeDataString(query)}");

            if (response == null) return;

            _allResults = response.Results;
            DisplayResults(response.Results);
            SearchPopup.IsOpen = response.Results.Any();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Search error: {ex.Message}");
        }
    }

    private void DisplayResults(List<SearchResult> results)
    {
        ResultsPanel.Children.Clear();

        // Group by type
        var grouped = results.GroupBy(r => r.Type);

        foreach (var group in grouped)
        {
            // Section header
            var header = new TextBlock
            {
                Text = group.Key.ToUpper(),
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(12, 8, 12, 4)
            };
            ResultsPanel.Children.Add(header);

            foreach (var result in group)
            {
                var item = CreateResultItem(result);
                ResultsPanel.Children.Add(item);
            }
        }
    }

    private Border CreateResultItem(SearchResult result)
    {
        var border = new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(12, 8),
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Icon/Avatar
        var icon = new Border
        {
            Width = 36, Height = 36,
            CornerRadius = new CornerRadius(result.Type == "user" ? 18 : 8),
            Background = result.Type == "user" 
                ? new LinearGradientBrush(
                    Color.FromRgb(24, 119, 242), 
                    Color.FromRgb(107, 76, 154), 
                    45)
                : (Brush)FindResource("SurfaceHoverBrush"),
            Child = new TextBlock
            {
                Text = result.Type == "user" ? result.Title[0].ToString() : "#",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(icon, 0);

        // Content
        var content = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        var title = new TextBlock
        {
            Text = result.Title,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 14
        };
        var subtitle = new TextBlock
        {
            Text = result.Subtitle,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontSize = 13
        };
        content.Children.Add(title);
        content.Children.Add(subtitle);
        Grid.SetColumn(content, 1);

        // Verified badge or count
        var badge = new TextBlock
        {
            Text = result.Type == "user" && result.IsVerified ? "✓" : result.Count,
            Foreground = result.Type == "user" && result.IsVerified 
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextMutedBrush"),
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(badge, 2);

        grid.Children.Add(icon);
        grid.Children.Add(content);
        grid.Children.Add(badge);
        border.Child = grid;

        border.MouseEnter += (s, e) => border.Background = (Brush)FindResource("SurfaceHoverBrush");
        border.MouseLeave += (s, e) => border.Background = Brushes.Transparent;
        border.MouseLeftButtonDown += (s, e) =>
        {
            ResultSelected?.Invoke(this, result);
            SearchPopup.IsOpen = false;
        };

        return border;
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        SearchContainer.BorderBrush = (Brush)FindResource("AccentBrush");
        if (_allResults.Any()) SearchPopup.IsOpen = true;
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        SearchContainer.BorderBrush = Brushes.Transparent;
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        SearchInput.Text = "";
        SearchInput.Focus();
    }

    private void OnFilterClick(object sender, RoutedEventArgs e)
    {
        FilterClicked?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        SearchInput.Text = "";
        SearchPopup.IsOpen = false;
    }
}

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // user, reel, hashtag, post
    public bool IsVerified { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Count { get; set; } = string.Empty;
}

public class SearchResponse
{
    public List<SearchResult> Results { get; set; } = new();
    public int TotalCount { get; set; }
}