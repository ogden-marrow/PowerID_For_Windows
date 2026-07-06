using PowerID.Models;

namespace PowerID.Core;

/// <summary>
/// Pure transform from a raw <see cref="AcpiBatteryReading"/> into a <see cref="BatterySnapshot"/>.
/// Kept free of any I/O, threading, or Windows-specific dependency so it can be unit tested in
/// isolation from the WMI battery service and the WinUI dispatcher.
/// </summary>
public static class BatterySnapshotCalculator
{
    public static BatterySnapshot Compute(AcpiBatteryReading reading, DateTimeOffset now)
    {
        var voltage = reading.VoltageMillivolts / 1000.0;

        // Some OEM firmware doesn't expose BatteryStaticData over WMI at all (no DesignVoltage),
        // even though BatteryStatus/BatteryFullChargedCapacity (a different WMI class) report real
        // capacities. Design voltage and current voltage are close enough for Li-ion batteries near
        // nominal charge that falling back to current voltage beats collapsing every capacity to 0.
        var designVoltageVolts = reading.DesignVoltageMillivolts > 0
            ? reading.DesignVoltageMillivolts / 1000.0
            : voltage;

        var designCapacityMah = ToMilliampHours(reading.DesignCapacityMilliwattHours, designVoltageVolts);
        var maxCapacityMah = ToMilliampHours(reading.FullChargeCapacityMilliwattHours, designVoltageVolts);
        var currentCapacityMah = ToMilliampHours(reading.RemainingCapacityMilliwattHours, designVoltageVolts);

        var level = maxCapacityMah > 0 ? (currentCapacityMah * 100) / maxCapacityMah : 0;
        var health = designCapacityMah > 0 ? (maxCapacityMah * 100) / designCapacityMah : 0;

        // Windows/ACPI exposes power (mW), not current (mA) like macOS's SMC does, so amperage is
        // derived from wattage and voltage rather than read directly.
        var (wattage, amperage) = ComputeWattageAndAmperage(reading, voltage);

        var timeToFullCharge = ComputeTimeToFullChargeMinutes(reading);
        var powerSourceType = reading.PowerOnline ? "AC Power" : "Battery";

        return new BatterySnapshot(
            Level: level,
            IsCharging: reading.Charging,
            Health: health,
            CycleCount: (int)reading.CycleCount,
            CurrentCapacity: currentCapacityMah,
            MaxCapacity: maxCapacityMah,
            DesignCapacity: designCapacityMah,
            Voltage: voltage,
            Amperage: amperage,
            Temperature: reading.TemperatureCelsius ?? 0.0,
            Wattage: wattage,
            TimeToFullCharge: timeToFullCharge,
            PowerSourceType: powerSourceType,
            Timestamp: now);
    }

    private static int ToMilliampHours(uint milliwattHours, double voltageVolts) =>
        voltageVolts > 0 ? (int)Math.Round(milliwattHours / voltageVolts) : 0;

    private static (double Wattage, double Amperage) ComputeWattageAndAmperage(AcpiBatteryReading reading, double voltage)
    {
        if (reading.Charging)
        {
            var wattage = reading.ChargeRateMilliwatts / 1000.0;
            var amperage = voltage > 0 ? reading.ChargeRateMilliwatts / voltage : 0.0;
            return (wattage, amperage);
        }

        if (reading.Discharging)
        {
            var wattage = reading.DischargeRateMilliwatts / 1000.0;
            var amperage = voltage > 0 ? -(reading.DischargeRateMilliwatts / voltage) : 0.0;
            return (wattage, amperage);
        }

        return (0.0, 0.0);
    }

    /// <summary>
    /// Human-readable charging status. Distinguishes "on battery" from "plugged in but not
    /// actively charging" (which is either "fully charged" or, more rarely, mid-calibration) -
    /// collapsing these into a single "on battery"/"charging" binary is misleading when the
    /// device is actually connected to AC power.
    /// </summary>
    public static string StatusText(BatterySnapshot snapshot)
    {
        if (snapshot.IsCharging) return "Charging";
        if (snapshot.PowerSourceType != "AC Power") return "On Battery";
        return snapshot.Level >= 100 ? "Fully Charged" : "Plugged In";
    }

    private static int ComputeTimeToFullChargeMinutes(AcpiBatteryReading reading)
    {
        if (!reading.Charging || reading.ChargeRateMilliwatts == 0) return 0;

        // Cast to long first: these are uint, and RemainingCapacity can exceed FullChargeCapacity
        // on some firmware's transient readings, which would otherwise wrap around to a huge value.
        var remainingMilliwattHours = (long)reading.FullChargeCapacityMilliwattHours - reading.RemainingCapacityMilliwattHours;
        if (remainingMilliwattHours <= 0) return 0;

        return (int)Math.Round(remainingMilliwattHours / (double)reading.ChargeRateMilliwatts * 60.0);
    }
}
