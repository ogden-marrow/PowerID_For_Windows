using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace PowerID.Utilities;

/// <summary>
/// Persisted user preferences, backed by the packaged app's local settings container - the
/// Windows equivalent of @AppStorage/UserDefaults on macOS.
/// </summary>
public sealed class SettingsStore : INotifyPropertyChanged
{
    private readonly ApplicationDataContainer _values = ApplicationData.Current.LocalSettings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double UpdateIntervalSeconds
    {
        get => GetValue("updateInterval", 2.0);
        set
        {
            SetValue("updateInterval", value);
            OnPropertyChanged();
        }
    }

    public bool ShowInTrayIcon
    {
        get => GetValue("showInMenuBar", true);
        set
        {
            SetValue("showInMenuBar", value);
            OnPropertyChanged();
        }
    }

    public bool LaunchAtLogin
    {
        get => GetValue("launchAtLogin", false);
        set
        {
            SetValue("launchAtLogin", value);
            OnPropertyChanged();
        }
    }

    public bool NotificationsEnabled
    {
        get => GetValue("notifications", true);
        set
        {
            SetValue("notifications", value);
            OnPropertyChanged();
        }
    }

    private T GetValue<T>(string key, T defaultValue)
    {
        return _values.Values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
    }

    private void SetValue<T>(string key, T value)
    {
        _values.Values[key] = value;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
