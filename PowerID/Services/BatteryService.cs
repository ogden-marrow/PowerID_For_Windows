using System.Management;

namespace PowerID.Services;

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

/// <summary>
/// Reads battery information from the ACPI "MSBatteryClass" WMI classes exposed under root\WMI.
/// This is the closest Windows equivalent of macOS's AppleSmartBattery IOKit registry: it is the
/// lowest-level source for voltage, charge/discharge rate, cycle count, and (when the firmware
/// exposes it) temperature.
/// </summary>
public sealed class BatteryService
{
    private const string Scope = @"root\WMI";

    /// <summary>Retrieves a snapshot of the first available battery's raw ACPI data.</summary>
    public AcpiBatteryReading GetReading()
    {
        try
        {
            uint designCapacity = 0, designVoltage = 0;
            using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM BatteryStaticData"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    designCapacity = Convert.ToUInt32(obj["DesignedCapacity"]);
                    designVoltage = Convert.ToUInt32(obj["DesignedVoltage"]);
                    break;
                }
            }

            uint fullChargeCapacity = 0;
            using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM BatteryFullChargedCapacity"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    fullChargeCapacity = Convert.ToUInt32(obj["FullChargedCapacity"]);
                    break;
                }
            }

            uint cycleCount = 0;
            using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM BatteryCycleCount"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    cycleCount = Convert.ToUInt32(obj["CycleCount"]);
                    break;
                }
            }

            uint remainingCapacity = 0, voltage = 0, chargeRate = 0, dischargeRate = 0;
            bool charging = false, discharging = false, powerOnline = false;
            using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM BatteryStatus"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    remainingCapacity = Convert.ToUInt32(obj["RemainingCapacity"]);
                    voltage = Convert.ToUInt32(obj["Voltage"]);
                    chargeRate = ClampRate(obj["ChargeRate"]);
                    dischargeRate = ClampRate(obj["DischargeRate"]);
                    charging = Convert.ToBoolean(obj["Charging"]);
                    discharging = Convert.ToBoolean(obj["Discharging"]);
                    powerOnline = Convert.ToBoolean(obj["PowerOnline"]);
                    break;
                }
            }

            // Not every OEM firmware exposes battery temperature; treat its absence as "unknown".
            double? temperatureCelsius = null;
            try
            {
                using var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM BatteryTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    // Reported in tenths of a degree Kelvin.
                    var deciKelvin = Convert.ToUInt32(obj["Temperature"]);
                    temperatureCelsius = (deciKelvin / 10.0) - 273.15;
                    break;
                }
            }
            catch (ManagementException)
            {
                temperatureCelsius = null;
            }

            return new AcpiBatteryReading(
                designCapacity,
                designVoltage,
                fullChargeCapacity,
                remainingCapacity,
                voltage,
                chargeRate,
                dischargeRate,
                charging,
                discharging,
                powerOnline,
                cycleCount,
                temperatureCelsius);
        }
        catch (ManagementException)
        {
            return AcpiBatteryReading.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return AcpiBatteryReading.Empty;
        }
    }

    /// <summary>
    /// ACPI reports an all-ones value (0xFFFFFFFF, "unknown rate") on some firmware; treat that as zero
    /// rather than a multi-gigawatt charge rate.
    /// </summary>
    private static uint ClampRate(object? value)
    {
        var rate = Convert.ToUInt32(value);
        return rate == uint.MaxValue ? 0u : rate;
    }
}
