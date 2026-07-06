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
