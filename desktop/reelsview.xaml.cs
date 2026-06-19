using RunApp.Desktop.Controls;

public partial class ReelsView : UserControl
{
    private readonly HttpClient _http;
    private List<ReelItem> _reels = new();
    private int _currentIndex = 0;
    private const int AD_INTERVAL = 10; // Show ad every 10 reels

    public ReelsView()
    {
        InitializeComponent();
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };
    }

    public async void LoadReels()
    {
        // Load user reels from API
        var response = await _http.GetFromJsonAsync<List<ReelItem>>("reels");
        _reels = response ?? new List<ReelItem>();

        // Insert ads at positions 10, 20, 30...
        var reelsWithAds = new List<object>();
        for (int i = 0; i < _reels.Count; i++)
        {
            reelsWithAds.Add(_reels[i]);
            if ((i + 1) % AD_INTERVAL == 0)
            {
                reelsWithAds.Add(new AdPlaceholder { Position = i + 1 });
            }
        }

        // Render
        RenderReels(reelsWithAds);
    }

    private void RenderReels(List<object> items)
    {
        ReelsViewport.Children.Clear();

        foreach (var item in items)
        {
            if (item is ReelItem reel)
            {
                var reelView = new ReelItemView(reel);
                ReelsViewport.Children.Add(reelView);
            }
            else if (item is AdPlaceholder)
            {
                var adView = new AdReelView();
                adView.SkipRequested += OnAdSkip;
                adView.WebsiteOpened += OnAdWebsite;
                ReelsViewport.Children.Add(adView);
                
                // Load ad content
                _ = adView.LoadAd();
            }
        }

        SetupScrollHandler();
    }

    private void OnAdSkip(object? sender, EventArgs e)
    {
        // Scroll to next reel
        ScrollToNext();
    }

    private void OnAdWebsite(object? sender, string url)
    {
        // Optional: Show toast that website opened
    }

    private void ScrollToNext()
    {
        if (_currentIndex < ReelsViewport.Children.Count - 1)
        {
            _currentIndex++;
            var next = ReelsViewport.Children[_currentIndex];
            next.BringIntoView();
        }
    }

    private void SetupScrollHandler()
    {
        // Track which reel/ad is visible and play/pause accordingly
        // ... existing scroll logic ...
    }
}

public class AdPlaceholder
{
    public int Position { get; set; }
}