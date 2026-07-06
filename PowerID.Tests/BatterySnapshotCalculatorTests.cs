using PowerID.Core;
using PowerID.Models;
using Xunit;

namespace PowerID.Tests;

public class BatterySnapshotCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    private static AcpiBatteryReading Reading(
        uint designCapacityMWh = 60000,
        uint designVoltageMv = 12000,
        uint fullChargeCapacityMWh = 55000,
        uint remainingCapacityMWh = 27500,
        uint voltageMv = 12000,
        uint chargeRateMw = 0,
        uint dischargeRateMw = 0,
        bool charging = false,
        bool discharging = true,
        bool powerOnline = false,
        uint cycleCount = 42,
        double? temperatureCelsius = 32.5) =>
        new(designCapacityMWh, designVoltageMv, fullChargeCapacityMWh, remainingCapacityMWh,
            voltageMv, chargeRateMw, dischargeRateMw, charging, discharging, powerOnline,
            cycleCount, temperatureCelsius);

    [Fact]
    public void Compute_WhenDischarging_ReturnsNegativeAmperageFromDischargeRate()
    {
        // 24,000 mW at 12V discharging is -2,000 mA (negative sign = discharging, matching the
        // Mac app's IOKit Amperage sign convention) and 24 W.
        var reading = Reading(voltageMv: 12000, dischargeRateMw: 24000, discharging: true, charging: false);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(-2000.0, snapshot.Amperage, precision: 6);
        Assert.Equal(24.0, snapshot.Wattage, precision: 6);
    }

    [Fact]
    public void Compute_WhenCharging_ReturnsPositiveAmperageFromChargeRate()
    {
        // 36,000 mW at 12V charging is +3,000 mA and 36 W.
        var reading = Reading(voltageMv: 12000, chargeRateMw: 36000, charging: true, discharging: false);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(3000.0, snapshot.Amperage, precision: 6);
        Assert.Equal(36.0, snapshot.Wattage, precision: 6);
    }

    [Fact]
    public void Compute_WhenIdle_WattageAndAmperageAreZero()
    {
        var reading = Reading(charging: false, discharging: false, chargeRateMw: 5000, dischargeRateMw: 5000);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0.0, snapshot.Amperage);
        Assert.Equal(0.0, snapshot.Wattage);
    }

    [Fact]
    public void Compute_WhenVoltageIsZero_AmperageDoesNotDivideByZero()
    {
        var reading = Reading(voltageMv: 0, chargeRateMw: 10000, charging: true, discharging: false);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0.0, snapshot.Amperage);
        Assert.Equal(10.0, snapshot.Wattage);
    }

    [Theory]
    [InlineData(60000u, 30000u, 50)]   // half full
    [InlineData(60000u, 60000u, 100)]  // full
    [InlineData(60000u, 0u, 0)]        // empty
    public void Compute_LevelIsPercentOfMaxCapacity(uint fullChargeCapacityMWh, uint remainingCapacityMWh, int expectedLevel)
    {
        var reading = Reading(
            designVoltageMv: 12000,
            fullChargeCapacityMWh: fullChargeCapacityMWh,
            remainingCapacityMWh: remainingCapacityMWh);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(expectedLevel, snapshot.Level);
    }

    [Fact]
    public void Compute_WhenMaxCapacityIsZero_LevelIsZeroNotDivideByZero()
    {
        var reading = Reading(fullChargeCapacityMWh: 0, remainingCapacityMWh: 0);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.Level);
    }

    [Theory]
    [InlineData(60000u, 60000u, 100)]  // brand new
    [InlineData(60000u, 30000u, 50)]   // half worn out
    [InlineData(60000u, 0u, 0)]        // fully worn out
    public void Compute_HealthIsPercentOfDesignCapacity(uint designCapacityMWh, uint fullChargeCapacityMWh, int expectedHealth)
    {
        var reading = Reading(
            designVoltageMv: 12000,
            designCapacityMWh: designCapacityMWh,
            fullChargeCapacityMWh: fullChargeCapacityMWh);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(expectedHealth, snapshot.Health);
    }

    [Fact]
    public void Compute_WhenDesignCapacityIsZero_HealthIsZeroNotDivideByZero()
    {
        var reading = Reading(designCapacityMWh: 0);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.Health);
    }

    [Fact]
    public void Compute_WhenDesignVoltageIsZero_AllCapacitiesAreZeroNotDivideByZero()
    {
        var reading = Reading(designVoltageMv: 0);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.DesignCapacity);
        Assert.Equal(0, snapshot.MaxCapacity);
        Assert.Equal(0, snapshot.CurrentCapacity);
    }

    [Fact]
    public void Compute_TimeToFullCharge_WhenCharging_EstimatesMinutesFromRemainingEnergyAndRate()
    {
        // 10,000 mWh remaining to full at a 30,000 mW rate should take 20 minutes.
        var reading = Reading(
            charging: true,
            discharging: false,
            fullChargeCapacityMWh: 60000,
            remainingCapacityMWh: 50000,
            chargeRateMw: 30000);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(20, snapshot.TimeToFullCharge);
    }

    [Fact]
    public void Compute_TimeToFullCharge_WhenNotCharging_IsZero()
    {
        var reading = Reading(charging: false, discharging: true, dischargeRateMw: 15000);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.TimeToFullCharge);
    }

    [Fact]
    public void Compute_TimeToFullCharge_WhenChargeRateIsZero_IsZeroNotDivideByZero()
    {
        var reading = Reading(charging: true, discharging: false, chargeRateMw: 0);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.TimeToFullCharge);
    }

    [Fact]
    public void Compute_TimeToFullCharge_WhenAlreadyAtFullCapacity_IsZero()
    {
        var reading = Reading(
            charging: true,
            discharging: false,
            fullChargeCapacityMWh: 60000,
            remainingCapacityMWh: 60000,
            chargeRateMw: 15000);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.TimeToFullCharge);
    }

    [Fact]
    public void Compute_TimeToFullCharge_WhenRemainingExceedsFullCapacity_DoesNotUnderflowToHugeValue()
    {
        // Some firmware transiently reports RemainingCapacity slightly above FullChargeCapacity;
        // since both are uint, a naive subtraction would wrap around to a huge number.
        var reading = Reading(
            charging: true,
            discharging: false,
            fullChargeCapacityMWh: 50000,
            remainingCapacityMWh: 50100,
            chargeRateMw: 10000);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0, snapshot.TimeToFullCharge);
    }

    [Theory]
    [InlineData(true, "AC Power")]
    [InlineData(false, "Battery")]
    public void Compute_PowerSourceType_ReflectsPowerOnlineFlag(bool powerOnline, string expected)
    {
        var reading = Reading(powerOnline: powerOnline);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(expected, snapshot.PowerSourceType);
    }

    [Fact]
    public void Compute_PassesThroughCycleCountAndTemperatureAndTimestamp()
    {
        var reading = Reading(cycleCount: 314, temperatureCelsius: 27.3);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(314, snapshot.CycleCount);
        Assert.Equal(27.3, snapshot.Temperature);
        Assert.Equal(Now, snapshot.Timestamp);
    }

    [Fact]
    public void Compute_WhenTemperatureIsUnknown_DefaultsToZero()
    {
        var reading = Reading(temperatureCelsius: null);

        var snapshot = BatterySnapshotCalculator.Compute(reading, Now);

        Assert.Equal(0.0, snapshot.Temperature);
    }

    [Fact]
    public void Compute_IsChargingReflectsReadingChargingFlag()
    {
        var charging = Reading(charging: true, discharging: false);
        var discharging = Reading(charging: false, discharging: true);

        Assert.True(BatterySnapshotCalculator.Compute(charging, Now).IsCharging);
        Assert.False(BatterySnapshotCalculator.Compute(discharging, Now).IsCharging);
    }
}
