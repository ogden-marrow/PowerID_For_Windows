using Microsoft.UI.Xaml.Media;
using PowerID.Core;
using Windows.UI;

namespace PowerID.Utilities;

/// <summary>
/// WinUI-specific adapter over <see cref="BatteryColorLogic"/>: converts its platform-agnostic
/// <see cref="System.Drawing.Color"/> results into WinUI <see cref="Color"/>/<see cref="Brush"/>
/// values for XAML binding. All the actual formatting/color rules live in PowerID.Core so they
/// can be unit tested without a Windows host.
/// </summary>
public static class BatteryFormatter
{
    /// <summary>Formats time in minutes to a human-readable string ("1h 30m" / "45m").</summary>
    public static string FormatTime(int minutes) => BatteryColorLogic.FormatTime(minutes);

    /// <summary>Returns the two colors used for the battery level gradient based on level and charging state.</summary>
    public static (Color Start, Color End) BatteryGradientColors(int level, bool isCharging)
    {
        var (start, end) = BatteryColorLogic.BatteryGradientColors(level, isCharging);
        return (ToWindowsColor(start), ToWindowsColor(end));
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

    /// <summary>Returns the appropriate color based on battery health percentage.</summary>
    public static Color HealthColor(int health) => ToWindowsColor(BatteryColorLogic.HealthColor(health));

    public static SolidColorBrush HealthColorBrush(int health) => new(HealthColor(health));

    private static Color ToWindowsColor(System.Drawing.Color color) =>
        Color.FromArgb(color.A, color.R, color.G, color.B);
}
