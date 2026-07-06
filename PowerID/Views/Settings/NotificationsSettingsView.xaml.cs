using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PowerID.Utilities;

namespace PowerID.Views.Settings;

public sealed partial class NotificationsSettingsView : UserControl
{
    private SettingsStore? _settings;
    private bool _isLoadingValues;

    public NotificationsSettingsView()
    {
        InitializeComponent();
    }

    public void Initialize(SettingsStore settings)
    {
        _settings = settings;
        _isLoadingValues = true;
        NotificationsToggle.IsOn = settings.NotificationsEnabled;
        NotificationInfoPanel.Visibility = settings.NotificationsEnabled ? Visibility.Visible : Visibility.Collapsed;
        _isLoadingValues = false;
    }

    private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        NotificationInfoPanel.Visibility = NotificationsToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        if (_isLoadingValues || _settings is null) return;
        _settings.NotificationsEnabled = NotificationsToggle.IsOn;
    }
}
