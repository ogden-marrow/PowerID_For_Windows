using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using PowerID.Services;
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

    public async void Initialize(SettingsStore settings)
    {
        _settings = settings;
        _isLoadingValues = true;
        UpdateIntervalSlider.Value = settings.UpdateIntervalSeconds;
        UpdateIntervalLabel.Text = $"{settings.UpdateIntervalSeconds:0.0}s";
        ShowInTrayToggle.IsOn = settings.ShowInTrayIcon;
        // The startup task's own registration state is authoritative - the user can also flip
        // this from Task Manager's Startup tab, which a locally-persisted preference wouldn't see.
        LaunchAtLoginToggle.IsOn = await StartupTaskService.IsEnabledAsync();
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

    private async void LaunchAtLoginToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isLoadingValues || _settings is null) return;
        var requested = LaunchAtLoginToggle.IsOn;
        var actual = await StartupTaskService.SetEnabledAsync(requested);
        _settings.LaunchAtLogin = actual;
        if (actual != requested)
        {
            // Reflect reality if the OS consent prompt was declined or policy blocked it, without
            // re-entering this handler (avoid IsOn -> Toggled -> IsOn feedback loop).
            _isLoadingValues = true;
            LaunchAtLoginToggle.IsOn = actual;
            _isLoadingValues = false;
        }
    }
}
