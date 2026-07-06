using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PowerID.ViewModels;
using PowerID.Views.Main;

namespace PowerID;

public sealed partial class MainWindow : Window
{
    public BatteryMonitor? BatteryMonitor { get; set; }

    private object? _lastNonSettingsSelection;

    public MainWindow()
    {
        InitializeComponent();
        Title = "PowerID";
        ContentFrame.Navigated += (_, _) => ApplyBatteryMonitorToCurrentPage();
    }

    private void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(OverviewPage));
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            (Application.Current as App)?.ShowSettingsWindow();
            if (_lastNonSettingsSelection is not null)
            {
                sender.SelectedItem = _lastNonSettingsSelection;
            }
            return;
        }

        if (args.SelectedItem is NavigationViewItem { Tag: string tag } item)
        {
            _lastNonSettingsSelection = item;
            var pageType = tag switch
            {
                "Overview" => typeof(OverviewPage),
                "Details" => typeof(DetailsPage),
                "Info" => typeof(InfoPage),
                _ => typeof(OverviewPage),
            };

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void ApplyBatteryMonitorToCurrentPage()
    {
        switch (ContentFrame.Content)
        {
            case OverviewPage overview:
                overview.BatteryMonitor = BatteryMonitor;
                break;
            case DetailsPage details:
                details.BatteryMonitor = BatteryMonitor;
                break;
            case InfoPage info:
                info.BatteryMonitor = BatteryMonitor;
                break;
        }
    }
}
