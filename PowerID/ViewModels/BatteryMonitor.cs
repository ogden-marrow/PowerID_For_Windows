using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using PowerID.Core;
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

    private string _statusText = string.Empty;
    public string StatusText { get => _statusText; private set => SetField(ref _statusText, value); }

    private string _lastUpdate = string.Empty;
    public string LastUpdate { get => _lastUpdate; private set => SetField(ref _lastUpdate, value); }

    /// <summary>Starts (or restarts) periodic battery polling at the given interval.</summary>
    public void StartMonitoring(TimeSpan interval)
    {
        _timer?.Dispose();
        UpdateBatteryInfo();
        _timer = new Timer(_ => UpdateBatteryInfo(), null, interval, interval);
    }

    /// <summary>Reads the battery service, computes a snapshot, and publishes it to the UI thread.</summary>
    public void UpdateBatteryInfo()
    {
        var reading = _batteryService.GetReading();
        var snapshot = BatterySnapshotCalculator.Compute(reading, DateTimeOffset.Now);

        _dispatcherQueue.TryEnqueue(() =>
        {
            BatteryLevel = snapshot.Level;
            IsCharging = snapshot.IsCharging;
            BatteryHealth = snapshot.Health;
            CycleCount = snapshot.CycleCount;
            CurrentCapacity = snapshot.CurrentCapacity;
            MaxCapacity = snapshot.MaxCapacity;
            DesignCapacity = snapshot.DesignCapacity;
            Voltage = snapshot.Voltage;
            Amperage = snapshot.Amperage;
            Temperature = snapshot.Temperature;
            ChargingWattage = snapshot.Wattage;
            TimeToFullCharge = snapshot.TimeToFullCharge;
            PowerSourceType = snapshot.PowerSourceType;
            StatusText = BatterySnapshotCalculator.StatusText(snapshot);
            LastUpdate = snapshot.Timestamp.ToString("T");
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
