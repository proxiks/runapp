using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RunApp.Desktop.Models;

namespace RunApp.Desktop.Controls;

public partial class AdReelView : UserControl
{
    private readonly HttpClient _http;
    private AdItem? _currentAd;
    private DispatcherTimer? _skipTimer;
    private int _skipSeconds = 5;
    private bool _canSkip = false;

    public event EventHandler? SkipRequested;
    public event EventHandler<string>? WebsiteOpened;

    public AdReelView()
    {
        InitializeComponent();
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };
    }

    public async Task LoadAd()
    {
        try
        {
            // Fetch ad from server
            var response = await _http.GetFromJsonAsync<AdItem>("ads/next?userId=current");
            
            if (response == null || response.Id == 0)
            {
                // No ad available, skip
                SkipRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            _currentAd = response;
            DisplayAd();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load ad: {ex.Message}");
            SkipRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DisplayAd()
    {
        if (_currentAd == null) return;

        // Set content
        AdvertiserInitial.Text = _currentAd.Advertiser.Name[0].ToString();
        AdvertiserName.Text = _currentAd.Advertiser.Name;
        HeadlineText.Text = _currentAd.Headline;
        DescriptionText.Text = _currentAd.Description;
        CtaText.Text = _currentAd.CtaText;
        LikeCount.Text = _currentAd.Likes.ToString();
        ShareCount.Text = _currentAd.Shares.ToString();

        // Load video
        if (!string.IsNullOrEmpty(_currentAd.VideoUrl))
        {
            AdVideo.Source = new Uri(_currentAd.VideoUrl);
            AdVideo.Play();
        }

        // Start skip timer
        StartSkipTimer();
    }

    private void StartSkipTimer()
    {
        _canSkip = false;
        _skipSeconds = 5;
        UpdateSkipText();

        _skipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _skipTimer.Tick += (s, e) =>
        {
            _skipSeconds--;
            if (_skipSeconds <= 0)
            {
                _canSkip = true;
                _skipTimer?.Stop();
                SkipTimer.Text = "Skip →";
                SkipTimer.MouseDown += (sender, args) => SkipRequested?.Invoke(this, EventArgs.Empty);
                SkipTimer.Cursor = System.Windows.Input.Cursors.Hand;
            }
            else
            {
                UpdateSkipText();
            }
        };
        _skipTimer.Start();
    }

    private void UpdateSkipText()
    {
        SkipTimer.Text = $"Skip in {_skipSeconds}s";
    }

    private async void OnLikeClick(object sender, RoutedEventArgs e)
    {
        if (_currentAd == null) return;

        try
        {
            var response = await _http.PostAsJsonAsync($"ads/{_currentAd.Id}/like", new { });
            var result = await response.Content.ReadFromJsonAsync<LikeResponse>();
            
            if (result != null)
            {
                LikeCount.Text = result.likes.ToString();
                LikeIcon.Foreground = new SolidColorBrush(Color.FromRgb(242, 82, 104)); // Red
                _currentAd.IsLiked = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Like failed: {ex.Message}");
        }
    }

    private async void OnShareClick(object sender, RoutedEventArgs e)
    {
        if (_currentAd == null) return;

        try
        {
            var response = await _http.PostAsJsonAsync($"ads/{_currentAd.Id}/share", new { });
            var result = await response.Content.ReadFromJsonAsync<ShareResponse>();
            
            if (result != null)
            {
                ShareCount.Text = result.shares.ToString();
                
                // Copy share URL to clipboard
                Clipboard.SetText(result.shareUrl);
                
                // Or use native share
                var shareWindow = new ShareWindow(result.shareUrl);
                shareWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Share failed: {ex.Message}");
        }
    }

    private async void OnCtaClick(object sender, RoutedEventArgs e)
    {
        await OpenWebsite();
    }

    private async void OnWebsiteClick(object sender, RoutedEventArgs e)
    {
        await OpenWebsite();
    }

    private async Task OpenWebsite()
    {
        if (_currentAd == null) return;

        // Track click
        try
        {
            await _http.PostAsJsonAsync($"ads/{_currentAd.Id}/click", new { userId = "current" });
        }
        catch { /* Non-critical */ }

        // Open browser
        if (!string.IsNullOrEmpty(_currentAd.WebsiteUrl))
        {
            Process.Start(new ProcessStartInfo(_currentAd.WebsiteUrl) { UseShellExecute = true });
            WebsiteOpened?.Invoke(this, _currentAd.WebsiteUrl);
        }
    }

    public void Pause()
    {
        AdVideo.Pause();
    }

    public void Resume()
    {
        AdVideo.Play();
    }

    public void Stop()
    {
        AdVideo.Stop();
        _skipTimer?.Stop();
    }
}

public class LikeResponse
{
    public int likes { get; set; }
    public bool liked { get; set; }
}

public class ShareResponse
{
    public int shares { get; set; }
    public string shareUrl { get; set; } = string.Empty;
}