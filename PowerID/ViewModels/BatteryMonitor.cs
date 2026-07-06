using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using PowerID.Services;

namespace PowerID.ViewModels;

/// <summary>
/// View model responsible for polling <see cref="BatteryService"/> and publishing battery
/// information to the UI. All published properties are updated on the dispatcher queue that
/// was supplied at construction time, mirroring the @MainActor guarantee of the macOS app.
/// </summary>
public sealed class BatteryMonitor : INotifyPropertyChanged, IDisposable
{
    private readonly BatteryService _batteryService = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private Timer? _timer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BatteryMonitor(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    private int _batteryLevel;
    public int BatteryLevel { get => _batteryLevel; private set => SetField(ref _batteryLevel, value); }

    private bool _isCharging;
    public bool IsCharging { get => _isCharging; private set => SetField(ref _isCharging, value); }

    private int _batteryHealth;
    public int BatteryHealth { get => _batteryHealth; private set => SetField(ref _batteryHealth, value); }

    private int _cycleCount;
    public int CycleCount { get => _cycleCount; private set => SetField(ref _cycleCount, value); }

    private int _currentCapacity;
    public int CurrentCapacity { get => _currentCapacity; private set => SetField(ref _currentCapacity, value); }

    private int _maxCapacity;
    public int MaxCapacity { get => _maxCapacity; private set => SetField(ref _maxCapacity, value); }

    private int _designCapacity;
    public int DesignCapacity { get => _designCapacity; private set => SetField(ref _designCapacity, value); }

    private double _voltage;
    public double Voltage { get => _voltage; private set => SetField(ref _voltage, value); }

    private double _amperage;
    public double Amperage { get => _amperage; private set => SetField(ref _amperage, value); }

    private double _temperature;
    public double Temperature { get => _temperature; private set => SetField(ref _temperature, value); }

    private double _chargingWattage;
    public double ChargingWattage { get => _chargingWattage; private set => SetField(ref _chargingWattage, value); }

    private int _timeToFullCharge;
    public int TimeToFullCharge { get => _timeToFullCharge; private set => SetField(ref _timeToFullCharge, value); }

    private string _powerSourceType = "Unknown";
    public string PowerSourceType { get => _powerSourceType; private set => SetField(ref _powerSourceType, value); }

    private string _lastUpdate = string.Empty;
    public string LastUpdate { get => _lastUpdate; private set => SetField(ref _lastUpdate, value); }

    /// <summary>Starts (or restarts) periodic battery polling at the given interval.</summary>
    public void StartMonitoring(TimeSpan interval)
    {
        _timer?.Dispose();
        UpdateBatteryInfo();
        _timer = new Timer(_ => UpdateBatteryInfo(), null, interval, interval);
    }

    /// <summary>Reads the battery service and publishes the results to the UI thread.</summary>
    public void UpdateBatteryInfo()
    {
        var reading = _batteryService.GetReading();

        var designVoltageVolts = reading.DesignVoltageMillivolts / 1000.0;
        var designCapacityMah = designVoltageVolts > 0
            ? (int)Math.Round(reading.DesignCapacityMilliwattHours / designVoltageVolts)
            : 0;
        var maxCapacityMah = designVoltageVolts > 0
            ? (int)Math.Round(reading.FullChargeCapacityMilliwattHours / designVoltageVolts)
            : 0;
        var currentCapacityMah = designVoltageVolts > 0
            ? (int)Math.Round(reading.RemainingCapacityMilliwattHours / designVoltageVolts)
            : 0;

        var level = maxCapacityMah > 0 ? (currentCapacityMah * 100) / maxCapacityMah : 0;
        var health = designCapacityMah > 0 ? (maxCapacityMah * 100) / designCapacityMah : 0;

        var voltage = reading.VoltageMillivolts / 1000.0;

        // Windows/ACPI exposes power (mW), not current (mA) like macOS's SMC does, so amperage is
        // derived from wattage and voltage rather than read directly.
        double wattage;
        double amperage;
        if (reading.Charging)
        {
            wattage = reading.ChargeRateMilliwatts / 1000.0;
            amperage = voltage > 0 ? (reading.ChargeRateMilliwatts / voltage) : 0.0;
        }
        else if (reading.Discharging)
        {
            wattage = reading.DischargeRateMilliwatts / 1000.0;
            amperage = voltage > 0 ? -(reading.DischargeRateMilliwatts / voltage) : 0.0;
        }
        else
        {
            wattage = 0.0;
            amperage = 0.0;
        }

        var timeToFullCharge = 0;
        if (reading.Charging && reading.ChargeRateMilliwatts > 0)
        {
            var remainingMilliwattHours = reading.FullChargeCapacityMilliwattHours - reading.RemainingCapacityMilliwattHours;
            if (remainingMilliwattHours > 0)
            {
                timeToFullCharge = (int)Math.Round(remainingMilliwattHours / (double)reading.ChargeRateMilliwatts * 60.0);
            }
        }

        var powerSourceType = reading.PowerOnline ? "AC Power" : "Battery";
        var lastUpdate = DateTimeOffset.Now.ToString("T");

        _dispatcherQueue.TryEnqueue(() =>
        {
            BatteryLevel = level;
            IsCharging = reading.Charging;
            BatteryHealth = health;
            CycleCount = (int)reading.CycleCount;
            CurrentCapacity = currentCapacityMah;
            MaxCapacity = maxCapacityMah;
            DesignCapacity = designCapacityMah;
            Voltage = voltage;
            Amperage = amperage;
            Temperature = reading.TemperatureCelsius ?? 0.0;
            ChargingWattage = wattage;
            TimeToFullCharge = timeToFullCharge;
            PowerSourceType = powerSourceType;
            LastUpdate = lastUpdate;
        });
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
