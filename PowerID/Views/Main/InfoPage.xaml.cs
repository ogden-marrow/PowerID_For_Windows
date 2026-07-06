using Microsoft.UI.Xaml.Controls;
using PowerID.ViewModels;

namespace PowerID.Views.Main;

public sealed partial class InfoPage : Page
{
    public BatteryMonitor? BatteryMonitor { get; set; }

    public InfoPage()
    {
        InitializeComponent();
    }
}
