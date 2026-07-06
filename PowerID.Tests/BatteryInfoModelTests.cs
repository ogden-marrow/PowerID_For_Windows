using PowerID.Models;
using Xunit;

namespace PowerID.Tests;

public class BatteryInfoModelTests
{
    [Fact]
    public void BatteryElectricalInfo_Empty_HasAllZeroValues()
    {
        var empty = BatteryElectricalInfo.Empty;

        Assert.Equal(0.0, empty.Voltage);
        Assert.Equal(0.0, empty.Amperage);
        Assert.Equal(0.0, empty.Temperature);
    }

    [Fact]
    public void AcpiBatteryReading_Empty_HasAllZeroValuesAndUnknownTemperature()
    {
        var empty = AcpiBatteryReading.Empty;

        Assert.Equal(0u, empty.DesignCapacityMilliwattHours);
        Assert.Equal(0u, empty.CycleCount);
        Assert.False(empty.Charging);
        Assert.False(empty.Discharging);
        Assert.False(empty.PowerOnline);
        Assert.Null(empty.TemperatureCelsius);
    }

    [Fact]
    public void BatterySnapshot_TwoInstancesWithSameValues_AreEqual()
    {
        var timestamp = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
        var a = new BatterySnapshot(50, true, 90, 100, 3000, 6000, 6600, 12.0, 2.5, 30.0, 30.0, 20, "AC Power", timestamp);
        var b = new BatterySnapshot(50, true, 90, 100, 3000, 6000, 6600, 12.0, 2.5, 30.0, 30.0, 20, "AC Power", timestamp);

        Assert.Equal(a, b);
    }
}
