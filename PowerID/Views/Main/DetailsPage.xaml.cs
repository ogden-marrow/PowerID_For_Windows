using Microsoft.UI.Xaml.Controls;
using PowerID.ViewModels;

namespace PowerID.Views.Main;

public sealed partial class DetailsPage : Page
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

    public DetailsPage()
    {
        InitializeComponent();
    }

    private static string FormatMah(int value) => $"{value} mAh";

    private static string FormatPercent(int value) => $"{value}%";

    private static string FormatVoltage(double value) => $"{value:0.00} V";

    private static string FormatAmperage(double value) => $"{value:0} mA";

    private static string FormatWattage(double value) => $"{value:0.00} W";

    private static string FormatTemperature(double value) => $"{value:0.0} °C";

    private static string ChargingStatusText(bool isCharging) => isCharging ? "Charging" : "On Battery";
}
