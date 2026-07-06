using Microsoft.UI.Xaml;
using PowerID.Utilities;

namespace PowerID.Views.Settings;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsStore settings)
    {
        InitializeComponent();
        Title = "PowerID Settings";
        GeneralView.Initialize(settings);
        NotificationsView.Initialize(settings);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(450, 350));
    }
}
