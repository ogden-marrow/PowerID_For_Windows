using Microsoft.UI.Xaml.Controls;
using PowerID.Utilities;

namespace PowerID.Views.Settings;

public sealed partial class GeneralSettingsView : UserControl
{
    private SettingsStore? _settings;
    private bool _isLoadingValues;

    public GeneralSettingsView()
    {
        InitializeComponent();
    }

    public void Initialize(SettingsStore settings)
    {
        _settings = settings;
        _isLoadingValues = true;
        UpdateIntervalSlider.Value = settings.UpdateIntervalSeconds;
        UpdateIntervalLabel.Text = $"{settings.UpdateIntervalSeconds:0.0}s";
        ShowInTrayToggle.IsOn = settings.ShowInTrayIcon;
        LaunchAtLoginToggle.IsOn = settings.LaunchAtLogin;
        _isLoadingValues = false;
    }

    private void UpdateIntervalSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateIntervalLabel.Text = $"{e.NewValue:0.0}s";
        if (_isLoadingValues || _settings is null) return;
        _settings.UpdateIntervalSeconds = e.NewValue;
    }

    private void ShowInTrayToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isLoadingValues || _settings is null) return;
        _settings.ShowInTrayIcon = ShowInTrayToggle.IsOn;
    }

    private void LaunchAtLoginToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isLoadingValues || _settings is null) return;
        _settings.LaunchAtLogin = LaunchAtLoginToggle.IsOn;
    }
}
