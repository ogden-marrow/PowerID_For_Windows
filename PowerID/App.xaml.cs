using Microsoft.UI.Xaml;
using PowerID.Services;
using PowerID.Utilities;
using PowerID.ViewModels;
using PowerID.Views.Settings;

namespace PowerID;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private TrayIconService? _trayIconService;

    public SettingsStore Settings { get; } = new();
    public BatteryMonitor BatteryMonitor { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        BatteryMonitor = new BatteryMonitor(_mainWindow.DispatcherQueue);
        BatteryMonitor.StartMonitoring(TimeSpan.FromSeconds(Settings.UpdateIntervalSeconds));

        _mainWindow.BatteryMonitor = BatteryMonitor;
        _mainWindow.Closed += OnMainWindowClosed;
        _mainWindow.Activate();

        _trayIconService = new TrayIconService(BatteryMonitor, ShowMainWindow, QuitApplication);
        if (Settings.ShowInTrayIcon)
        {
            _trayIconService.Enable();
        }

        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsStore.ShowInTrayIcon))
            {
                if (Settings.ShowInTrayIcon) _trayIconService.Enable();
                else _trayIconService.Disable();
            }
            else if (e.PropertyName == nameof(SettingsStore.UpdateIntervalSeconds))
            {
                BatteryMonitor.StartMonitoring(TimeSpan.FromSeconds(Settings.UpdateIntervalSeconds));
            }
        };
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (Settings.ShowInTrayIcon)
        {
            // Keep running in the tray, same as macOS's applicationShouldTerminateAfterLastWindowClosed.
            args.Handled = true;
            _mainWindow?.AppWindow.Hide();
        }
        else
        {
            QuitApplication();
        }
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.AppWindow.Show();
        _mainWindow.Activate();
    }

    public void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(Settings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Activate();
    }

    private void QuitApplication()
    {
        _trayIconService?.Dispose();
        BatteryMonitor?.Dispose();
        Exit();
    }
}
