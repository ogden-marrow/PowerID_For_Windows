using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PowerID.Utilities;

/// <summary>Converts a bool to Visibility (true -> Visible).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>Converts an int to Visibility (value > 0 -> Visible). Used for "time to full charge" rows.</summary>
public sealed class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (value is int i && i > 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
