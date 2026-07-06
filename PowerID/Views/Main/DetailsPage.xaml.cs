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

    private string FormatMah(int value) => $"{value} mAh";

    private string FormatPercent(int value) => $"{value}%";

    private string FormatVoltage(double value) => $"{value:0.00} V";

    private string FormatAmperage(double value) => $"{value:0} mA";

    private string FormatWattage(double value) => $"{value:0.00} W";

    private string FormatTemperature(double value) => $"{value:0.0} °C";

    private string ChargingStatusText(bool isCharging) => isCharging ? "Charging" : "On Battery";
}
