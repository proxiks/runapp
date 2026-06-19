using System.IO;
using System.Windows;

namespace RunApp.Desktop.Services;

public enum AppTheme
{
    Dark,      // Facebook dark
    Light,     // Facebook light
    System,    // Follow Windows
    Midnight,  // Pure black OLED
    Ocean      // Blue tint
}

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private AppTheme _currentTheme = AppTheme.Dark;
    public AppTheme CurrentTheme => _currentTheme;

    public event Action<AppTheme>? ThemeChanged;

    private readonly string _settingsPath;
    private readonly Dictionary<AppTheme, ThemeColors> _themes;

    private ThemeService()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RunApp", "theme.config");

        _themes = new Dictionary<AppTheme, ThemeColors>
        {
            [AppTheme.Dark] = new ThemeColors
            {
                Background = "#18191A",
                Surface = "#242526",
                SurfaceHover = "#3A3B3C",
                TextPrimary = "#E4E6EB",
                TextSecondary = "#B0B3B8",
                TextMuted = "#65676B",
                Accent = "#1877F2",
                AccentHover = "#166fe5",
                Success = "#42B72A",
                Danger = "#F02849",
                Border = "#3A3B3C",
                Shadow = "#000000"
            },
            [AppTheme.Light] = new ThemeColors
            {
                Background = "#F0F2F5",
                Surface = "#FFFFFF",
                SurfaceHover = "#F0F2F5",
                TextPrimary = "#050505",
                TextSecondary = "#65676B",
                TextMuted = "#8C939D",
                Accent = "#1877F2",
                AccentHover = "#166fe5",
                Success = "#42B72A",
                Danger = "#F02849",
                Border = "#DADDE1",
                Shadow = "rgba(0,0,0,0.1)"
            },
            [AppTheme.Midnight] = new ThemeColors
            {
                Background = "#000000",
                Surface = "#0A0A0A",
                SurfaceHover = "#141414",
                TextPrimary = "#FFFFFF",
                TextSecondary = "#A0A0A0",
                TextMuted = "#606060",
                Accent = "#00D4AA",
                AccentHover = "#00B894",
                Success = "#00D4AA",
                Danger = "#FF4757",
                Border = "#1A1A1A",
                Shadow = "#000000"
            },
            [AppTheme.Ocean] = new ThemeColors
            {
                Background = "#0F172A",
                Surface = "#1E293B",
                SurfaceHover = "#334155",
                TextPrimary = "#F8FAFC",
                TextSecondary = "#94A3B8",
                TextMuted = "#64748B",
                Accent = "#38BDF8",
                AccentHover = "#0EA5E9",
                Success = "#34D399",
                Danger = "#FB7185",
                Border = "#334155",
                Shadow = "#000000"
            }
        };

        LoadSavedTheme();
    }

    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme == theme) return;
        
        _currentTheme = theme;
        ApplyTheme(theme);
        SaveTheme(theme);
        ThemeChanged?.Invoke(theme);
    }

    private void ApplyTheme(AppTheme theme)
    {
        var colors = _themes[theme];
        var app = Application.Current;

        // Update resource dictionary
        var dict = app.Resources;
        dict["BackgroundBrush"] = new SolidColorBrush(ParseColor(colors.Background));
        dict["SurfaceBrush"] = new SolidColorBrush(ParseColor(colors.Surface));
        dict["SurfaceHoverBrush"] = new SolidColorBrush(ParseColor(colors.SurfaceHover));
        dict["TextPrimaryBrush"] = new SolidColorBrush(ParseColor(colors.TextPrimary));
        dict["TextSecondaryBrush"] = new SolidColorBrush(ParseColor(colors.TextSecondary));
        dict["TextMutedBrush"] = new SolidColorBrush(ParseColor(colors.TextMuted));
        dict["AccentBrush"] = new SolidColorBrush(ParseColor(colors.Accent));
        dict["AccentHoverBrush"] = new SolidColorBrush(ParseColor(colors.AccentHover));
        dict["SuccessBrush"] = new SolidColorBrush(ParseColor(colors.Success));
        dict["DangerBrush"] = new SolidColorBrush(ParseColor(colors.Danger));
        dict["BorderBrush"] = new SolidColorBrush(ParseColor(colors.Border));

        // Force refresh all open windows
        foreach (Window window in app.Windows)
        {
            window.InvalidateVisual();
        }
    }

    private void LoadSavedTheme()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var saved = File.ReadAllText(_settingsPath);
                if (Enum.TryParse<AppTheme>(saved, out var theme))
                {
                    _currentTheme = theme;
                }
            }
        }
        catch { /* Default to dark */ }

        ApplyTheme(_currentTheme);
    }

    private void SaveTheme(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, theme.ToString());
        }
        catch { /* Ignore save errors */ }
    }

    private static Color ParseColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
            if (hex.Length == 6)
            {
                return Color.FromRgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            }
        }
        return Colors.Black;
    }

    public ThemeColors GetColors() => _themes[_currentTheme];
}

public class ThemeColors
{
    public string Background { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string SurfaceHover { get; set; } = string.Empty;
    public string TextPrimary { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string TextMuted { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
    public string AccentHover { get; set; } = string.Empty;
    public string Success { get; set; } = string.Empty;
    public string Danger { get; set; } = string.Empty;
    public string Border { get; set; } = string.Empty;
    public string Shadow { get; set; } = string.Empty;
}