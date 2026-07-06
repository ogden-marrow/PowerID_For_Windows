# PowerID (Windows) Architecture

This is the Windows port of [PowerID for Mac](https://github.com/ogden-marrow/PowerID_For_Mac), rebuilt with WinUI 3
(.NET 8, C#) instead of SwiftUI. The information architecture, view hierarchy, and naming closely mirror the macOS
app so the two codebases stay easy to cross-reference.

## Project Structure

```
PowerID/
├── App.xaml / App.xaml.cs        # App startup, window/tray lifecycle (like AppDelegate + PowerIDApp.swift)
├── MainWindow.xaml(.cs)          # Root window with top NavigationView tab bar (like ContentView.swift)
│
├── Views/
│   ├── Main/                     # Main app pages
│   │   ├── OverviewPage          # Battery overview dashboard
│   │   ├── DetailsPage           # Detailed battery information
│   │   └── InfoPage              # Battery tips and information
│   │
│   ├── Settings/                 # Settings window views
│   │   ├── SettingsWindow        # Settings window root (Pivot host)
│   │   ├── GeneralSettingsView
│   │   ├── NotificationsSettingsView
│   │   └── AboutSettingsView
│   │
│   └── Components/               # Reusable view components
│       ├── StatusCard
│       ├── SectionHeader
│       ├── DetailRow
│       ├── InfoSection
│       ├── TipRow
│       └── BatteryProgressBar
│
├── ViewModels/
│   └── BatteryMonitor.cs         # Polls BatteryService and publishes battery state
│
├── Models/
│   └── BatteryInfo.cs            # Battery data structures
│
├── Services/
│   ├── BatteryService.cs         # Reads ACPI battery data via WMI (root\WMI)
│   └── TrayIconService.cs        # Native Win32 system tray icon + menu
│
├── Utilities/
│   ├── BatteryFormatter.cs       # Gradient/color/time formatting helpers
│   ├── SettingsStore.cs          # Persisted preferences (ApplicationData local settings)
│   └── Converters.cs             # XAML value converters
│
└── Assets/                       # App icons and package assets
```

## Architecture Pattern

MVVM, same as the macOS app:

- **Models** (`BatteryInfo.cs`): plain data records for battery state.
- **Views** (`Views/Main`, `Views/Settings`, `Views/Components`): XAML pages/controls, presentation only.
- **ViewModels** (`BatteryMonitor.cs`): `INotifyPropertyChanged` object that owns polling and published state.
- **Services** (`BatteryService.cs`, `TrayIconService.cs`): external system access (WMI, Win32 shell APIs).
- **Utilities** (`BatteryFormatter.cs`, `SettingsStore.cs`): pure helpers and persisted preferences.

## Data Flow

```
ACPI battery data (root\WMI classes)
    ↓
BatteryService (raw WMI reads: voltage, rate, cycle count, capacities)
    ↓
BatteryMonitor (computes level/health/wattage/time-to-full, publishes on the UI dispatcher)
    ↓
Pages/Controls (x:Bind, OneWay)
    ↓
BatteryFormatter (gradient/color/time formatting for display)
```

## macOS → Windows mapping

| macOS (IOKit / SwiftUI)                          | Windows (WMI / WinUI 3)                                  |
|---------------------------------------------------|-----------------------------------------------------------|
| `IOPSCopyPowerSourcesInfo` (level, charging, TTFC) | `BatteryStatus` WMI class (remaining capacity, rates, charging/discharging flags) |
| `IORegistryEntryCreateCFProperty` (voltage/amperage/temp/design capacity/cycle count) | `BatteryStaticData`, `BatteryFullChargedCapacity`, `BatteryCycleCount`, `BatteryTemperature` WMI classes |
| `NSStatusItem` menu bar icon (`MenuBarManager.swift`) | `Shell_NotifyIcon` tray icon + native popup menu (`TrayIconService.cs`) |
| `@AppStorage` / `UserDefaults`                     | `ApplicationData.Current.LocalSettings` (`SettingsStore.cs`) |
| SwiftUI `TabView`                                  | WinUI `NavigationView` (top pane mode) + `Frame` navigation |
| SwiftUI `Settings { }` scene                       | Separate `SettingsWindow` with a `Pivot` |

One notable difference: macOS's SMC exposes raw current (mA); Windows/ACPI exposes power (mW) instead. Amperage is
therefore derived from wattage and voltage (`ChargeRate / Voltage`) rather than read directly - see the comment in
`BatteryMonitor.UpdateBatteryInfo`. Battery temperature also isn't exposed by every OEM's firmware; when the
`BatteryTemperature` WMI class is absent, the value falls back to 0, matching the "Unknown" fallback behavior of the
macOS app when a key is missing.

## Adding New Features

### Adding a New Page
1. Create the `.xaml`/`.xaml.cs` pair under the appropriate `Views/` subfolder.
2. Follow the existing `BatteryMonitor` property + `Bindings.Update()` pattern used by `OverviewPage`/`DetailsPage`
   so `x:Bind Mode=OneWay` bindings refresh once the view model is attached after navigation.
3. Reuse existing `Views/Components` controls where possible.

### Adding New Battery Data
1. Add the raw field to `AcpiBatteryReading` in `BatteryService.cs` if it needs a new WMI query.
2. Add a published property to `BatteryMonitor.cs` and compute it in `UpdateBatteryInfo()`.
3. Bind to it from the relevant page via `x:Bind`.

### Adding a New Component
1. Create a `UserControl` in `Views/Components/`.
2. Expose simple `DependencyProperty` values (string/int/Brush) rather than accepting arbitrary child content,
   matching the pattern used by `StatusCard`/`DetailRow`/`InfoSection`.
