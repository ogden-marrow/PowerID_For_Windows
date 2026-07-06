using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PowerID.Utilities;

/// <summary>Utility functions for formatting battery information and UI elements.</summary>
public static class BatteryFormatter
{
    // MARK: - Time Formatting

    /// <summary>Formats time in minutes to a human-readable string ("1h 30m" / "45m").</summary>
    public static string FormatTime(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";
    }

    // MARK: - Battery Gradient

    /// <summary>Returns the two colors used for the battery level gradient based on level and charging state.</summary>
    public static (Color Start, Color End) BatteryGradientColors(int level, bool isCharging)
    {
        if (isCharging)
        {
            return (Colors.Green, Colors.MediumSpringGreen);
        }
        if (level < 20)
        {
            return (Colors.Red, Colors.Orange);
        }
        if (level < 50)
        {
            return (Colors.Orange, Colors.Gold);
        }
        return (Colors.DodgerBlue, Colors.Cyan);
    }

    /// <summary>Returns a diagonal LinearGradientBrush for the given battery level and charging state.</summary>
    public static LinearGradientBrush BatteryGradientBrush(int level, bool isCharging)
    {
        var (start, end) = BatteryGradientColors(level, isCharging);
        return new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            GradientStops =
            {
                new GradientStop { Color = start, Offset = 0.0 },
                new GradientStop { Color = end, Offset = 1.0 },
            },
        };
    }

    // MARK: - Health Color

    /// <summary>Returns the appropriate color based on battery health percentage.</summary>
    public static Color HealthColor(int health)
    {
        if (health >= 80) return Colors.Green;
        if (health >= 60) return Colors.Orange;
        return Colors.Red;
    }

    public static SolidColorBrush HealthColorBrush(int health) => new(HealthColor(health));
}
