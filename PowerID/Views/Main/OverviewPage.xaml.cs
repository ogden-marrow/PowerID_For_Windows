using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PowerID.Utilities;
using PowerID.ViewModels;
using Windows.UI;

namespace PowerID.Views.Main;

public sealed partial class OverviewPage : Page
{
    private BatteryMonitor? _batteryMonitor;

    public BatteryMonitor? BatteryMonitor
    {
        get => _batteryMonitor;
        set
        {
            _batteryMonitor = value;
            Bindings.Update();
        }
    }

    public OverviewPage()
    {
        InitializeComponent();
    }

    private string ChargingGlyph(bool isCharging) => isCharging ? "⚡" : "\U0001F50B";

    private Color StatusColor(bool isCharging) => isCharging ? Colors.Green : Colors.DodgerBlue;

    private string FormatWattage(double wattage) => $"{wattage:0.0} W";

    private string FormatPercent(int value) => $"{value}%";

    private Visibility ShowTimeToFull(bool isCharging, int timeToFullCharge) =>
        isCharging && timeToFullCharge > 0 ? Visibility.Visible : Visibility.Collapsed;

    private string FormatLastUpdated(string lastUpdate) => $"Last updated: {lastUpdate}";
}
