using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RunApp.Desktop.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public class ReadToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? new SolidColorBrush(Color.FromRgb(36, 37, 38)) 
                           : new SolidColorBrush(Color.FromRgb(24, 25, 26));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(string), typeof(Brush))]
public class TypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = (value as string) switch
        {
            "Like" => Color.FromRgb(242, 82, 104),    // Red
            "Comment" => Color.FromRgb(24, 119, 242),  // Blue
            "Follow" => Color.FromRgb(66, 183, 42),  // Green
            "Verified" => Color.FromRgb(24, 119, 242), // Blue
            _ => Color.FromRgb(101, 103, 107)         // Gray
        };
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class UnreadToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}