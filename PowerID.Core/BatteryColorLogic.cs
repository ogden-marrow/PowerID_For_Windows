using System.Drawing;

namespace PowerID.Core;

/// <summary>
/// Pure formatting/color rules for battery display - the platform-agnostic core of
/// BatteryFormatter. Uses <see cref="Color"/> only as a plain RGBA value (no GDI+ calls), so it
/// has no Windows dependency and is safe to unit test on any OS.
/// </summary>
public static class BatteryColorLogic
{
    /// <summary>Formats time in minutes to a human-readable string ("1h 30m" / "45m").</summary>
    public static string FormatTime(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";
    }

    /// <summary>Returns the two colors used for the battery level gradient based on level and charging state.</summary>
    public static (Color Start, Color End) BatteryGradientColors(int level, bool isCharging)
    {
        if (isCharging)
        {
            return (Color.Green, Color.MediumSpringGreen);
        }
        if (level < 20)
        {
            return (Color.Red, Color.Orange);
        }
        if (level < 50)
        {
            return (Color.Orange, Color.Gold);
        }
        return (Color.DodgerBlue, Color.Cyan);
    }

    /// <summary>Returns the appropriate color based on battery health percentage.</summary>
    public static Color HealthColor(int health)
    {
        if (health >= 80) return Color.Green;
        if (health >= 60) return Color.Orange;
        return Color.Red;
    }
}
