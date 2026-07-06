using System.Management;
using PowerID.Models;

namespace PowerID.Services;

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
            QueryFirstInstance("BatteryStaticData", obj =>
            {
                // Not every OEM driver populates every optional field on this class (e.g. some
                // report DesignedCapacity but omit DesignedVoltage entirely) - read each property
                // independently so one missing field doesn't discard the others.
                designCapacity = GetUInt32OrDefault(obj, "DesignedCapacity");
                designVoltage = GetUInt32OrDefault(obj, "DesignedVoltage");
            });

            uint fullChargeCapacity = 0;
            QueryFirstInstance("BatteryFullChargedCapacity", obj =>
            {
                fullChargeCapacity = GetUInt32OrDefault(obj, "FullChargedCapacity");
            });

            uint cycleCount = 0;
            QueryFirstInstance("BatteryCycleCount", obj =>
            {
                cycleCount = GetUInt32OrDefault(obj, "CycleCount");
            });

            uint remainingCapacity = 0, voltage = 0, chargeRate = 0, dischargeRate = 0;
            bool charging = false, discharging = false, powerOnline = false;
            QueryFirstInstance("BatteryStatus", obj =>
            {
                remainingCapacity = GetUInt32OrDefault(obj, "RemainingCapacity");
                voltage = GetUInt32OrDefault(obj, "Voltage");
                chargeRate = ClampRate(GetUInt32OrDefault(obj, "ChargeRate"));
                dischargeRate = ClampRate(GetUInt32OrDefault(obj, "DischargeRate"));
                charging = GetBooleanOrDefault(obj, "Charging");
                discharging = GetBooleanOrDefault(obj, "Discharging");
                powerOnline = GetBooleanOrDefault(obj, "PowerOnline");
            });

            // Not every OEM firmware exposes battery temperature; treat its absence as "unknown".
            double? temperatureCelsius = null;
            QueryFirstInstance("BatteryTemperature", obj =>
            {
                var deciKelvin = GetUInt32OrDefault(obj, "Temperature");
                if (deciKelvin > 0)
                {
                    temperatureCelsius = (deciKelvin / 10.0) - 273.15;
                }
            });

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
    /// Runs a WMI class query and invokes <paramref name="onFound"/> for the first instance, if
    /// any. A missing/erroring WMI class is treated as "no data for this class" rather than
    /// failing the entire reading - different classes come from different (sometimes absent)
    /// OEM driver components.
    /// </summary>
    private static void QueryFirstInstance(string className, Action<ManagementBaseObject> onFound)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(Scope, $"SELECT * FROM {className}");
            foreach (ManagementObject obj in searcher.Get())
            {
                onFound(obj);
                break;
            }
        }
        catch (ManagementException)
        {
            // This specific class isn't available on this hardware/driver - leave defaults.
        }
    }

    private static uint GetUInt32OrDefault(ManagementBaseObject obj, string propertyName)
    {
        foreach (PropertyData prop in obj.Properties)
        {
            if (prop.Name == propertyName)
            {
                return prop.Value is null ? 0u : Convert.ToUInt32(prop.Value);
            }
        }

        return 0u;
    }

    private static bool GetBooleanOrDefault(ManagementBaseObject obj, string propertyName)
    {
        foreach (PropertyData prop in obj.Properties)
        {
            if (prop.Name == propertyName)
            {
                return prop.Value is not null && Convert.ToBoolean(prop.Value);
            }
        }

        return false;
    }

    /// <summary>
    /// ACPI reports an all-ones value (0xFFFFFFFF, "unknown rate") on some firmware; treat that as zero
    /// rather than a multi-gigawatt charge rate.
    /// </summary>
    private static uint ClampRate(uint rate) => rate == uint.MaxValue ? 0u : rate;
}
