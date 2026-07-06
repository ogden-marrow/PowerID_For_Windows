using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace PowerID.Views.Settings;

public sealed partial class AboutSettingsView : UserControl
{
    public AboutSettingsView()
    {
        InitializeComponent();
    }

    private async void GitHubButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/ogden-marrow/PowerID_For_Windows"));
    }

    private async void ReportIssueButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/ogden-marrow/PowerID_For_Windows/issues"));
    }
}
