namespace PowerID.Models;

/// <summary>Electrical readings from the battery (voltage, amperage, temperature).</summary>
public readonly record struct BatteryElectricalInfo(double Voltage, double Amperage, double Temperature)
{
    public static readonly BatteryElectricalInfo Empty = new(0.0, 0.0, 0.0);
}

/// <summary>Complete battery snapshot at a point in time.</summary>
public sealed record BatterySnapshot(
    int Level,
    bool IsCharging,
    int Health,
    int CycleCount,
    int CurrentCapacity,
    int MaxCapacity,
    int DesignCapacity,
    double Voltage,
    double Amperage,
    double Temperature,
    double Wattage,
    int TimeToFullCharge,
    string PowerSourceType,
    DateTimeOffset Timestamp);

/// <summary>Raw battery data as reported by the ACPI-backed WMI classes in the root\WMI namespace.</summary>
public sealed record AcpiBatteryReading(
    uint DesignCapacityMilliwattHours,
    uint DesignVoltageMillivolts,
    uint FullChargeCapacityMilliwattHours,
    uint RemainingCapacityMilliwattHours,
    uint VoltageMillivolts,
    uint ChargeRateMilliwatts,
    uint DischargeRateMilliwatts,
    bool Charging,
    bool Discharging,
    bool PowerOnline,
    uint CycleCount,
    double? TemperatureCelsius)
{
    public static readonly AcpiBatteryReading Empty = new(0, 0, 0, 0, 0, 0, 0, false, false, false, 0, null);
}
