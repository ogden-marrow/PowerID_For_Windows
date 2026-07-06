using System.Drawing;
using PowerID.Core;
using Xunit;

namespace PowerID.Tests;

public class BatteryColorLogicTests
{
    [Theory]
    [InlineData(0, "0m")]
    [InlineData(5, "5m")]
    [InlineData(59, "59m")]
    [InlineData(60, "1h 0m")]
    [InlineData(75, "1h 15m")]
    [InlineData(125, "2h 5m")]
    public void FormatTime_FormatsMinutesAsHoursAndMinutes(int minutes, string expected)
    {
        Assert.Equal(expected, BatteryColorLogic.FormatTime(minutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)]
    [InlineData(50)]
    [InlineData(100)]
    public void BatteryGradientColors_WhenCharging_AlwaysReturnsGreenGradientRegardlessOfLevel(int level)
    {
        var (start, end) = BatteryColorLogic.BatteryGradientColors(level, isCharging: true);

        Assert.Equal(Color.Green, start);
        Assert.Equal(Color.MediumSpringGreen, end);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)]
    public void BatteryGradientColors_WhenNotChargingAndLevelBelow20_ReturnsRedGradient(int level)
    {
        var (start, end) = BatteryColorLogic.BatteryGradientColors(level, isCharging: false);

        Assert.Equal(Color.Red, start);
        Assert.Equal(Color.Orange, end);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(49)]
    public void BatteryGradientColors_WhenNotChargingAndLevelBetween20And49_ReturnsOrangeGradient(int level)
    {
        var (start, end) = BatteryColorLogic.BatteryGradientColors(level, isCharging: false);

        Assert.Equal(Color.Orange, start);
        Assert.Equal(Color.Gold, end);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void BatteryGradientColors_WhenNotChargingAndLevelAtLeast50_ReturnsBlueGradient(int level)
    {
        var (start, end) = BatteryColorLogic.BatteryGradientColors(level, isCharging: false);

        Assert.Equal(Color.DodgerBlue, start);
        Assert.Equal(Color.Cyan, end);
    }

    [Theory]
    [InlineData(100, "Green")]
    [InlineData(80, "Green")]
    [InlineData(79, "Orange")]
    [InlineData(60, "Orange")]
    [InlineData(59, "Red")]
    [InlineData(0, "Red")]
    public void HealthColor_MapsHealthPercentageToTrafficLightColor(int health, string expectedColorName)
    {
        var expected = Color.FromName(expectedColorName);

        Assert.Equal(expected, BatteryColorLogic.HealthColor(health));
    }
}
