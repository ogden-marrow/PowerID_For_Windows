# PowerID (Windows) Architecture

This is the Windows port of [PowerID for Mac](https://github.com/ogden-marrow/PowerID_For_Mac), rebuilt with WinUI 3
(.NET 8, C#) instead of SwiftUI. The information architecture, view hierarchy, and naming closely mirror the macOS
app so the two codebases stay easy to cross-reference.

## Project Structure

The solution is split into three projects specifically so the business logic can be unit tested
without a Windows/WinUI host:

```
PowerID.Core/                     # Plain net8.0 class library - zero Windows/WinUI dependency
├── Models/BatteryInfo.cs         # Battery data structures (snapshot, electrical info, raw ACPI reading)
├── BatterySnapshotCalculator.cs  # Pure transform: AcpiBatteryReading -> BatterySnapshot
└── BatteryColorLogic.cs          # Pure formatting/color rules (time, gradients, health color)

PowerID.Tests/                    # xUnit tests for PowerID.Core - runs on any OS, no Windows needed
├── BatterySnapshotCalculatorTests.cs
├── BatteryColorLogicTests.cs
└── BatteryInfoModelTests.cs

PowerID/                          # The WinUI 3 app itself
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
│   └── BatteryMonitor.cs         # Polls BatteryService, delegates to BatterySnapshotCalculator, publishes state
│
├── Services/
│   ├── BatteryService.cs         # Reads raw ACPI battery data via WMI (root\WMI)
│   └── TrayIconService.cs        # Native Win32 system tray icon + menu
│
├── Utilities/
│   ├── BatteryFormatter.cs       # Thin WinUI adapter over PowerID.Core.BatteryColorLogic
│   ├── SettingsStore.cs          # Persisted preferences (ApplicationData local settings)
│   └── Converters.cs             # XAML value converters
│
└── Assets/                       # App icons and package assets
```

## Architecture Pattern

MVVM, same as the macOS app - with the business logic pulled one layer further out than a typical
MVVM split, into a platform-agnostic core:

- **PowerID.Core**: pure, Windows-free logic. `BatterySnapshotCalculator` turns a raw WMI reading
  into a `BatterySnapshot` (level/health/wattage/time-to-full math); `BatteryColorLogic` turns
  battery state into colors and formatted strings. Neither touches WMI, WinUI, or any I/O - both
  are exercised directly by `PowerID.Tests`.
- **Models** (`PowerID.Core/Models/BatteryInfo.cs`): plain data records for battery state.
- **Views** (`Views/Main`, `Views/Settings`, `Views/Components`): XAML pages/controls, presentation only.
- **ViewModels** (`BatteryMonitor.cs`): `INotifyPropertyChanged` object that owns polling, calls into
  `BatterySnapshotCalculator`, and publishes the result on the UI dispatcher.
- **Services** (`BatteryService.cs`, `TrayIconService.cs`): external system access (WMI, Win32 shell APIs).
- **Utilities** (`BatteryFormatter.cs`, `SettingsStore.cs`): thin WinUI adapters and persisted preferences.

## Data Flow

```
ACPI battery data (root\WMI classes)
    ↓
BatteryService (raw WMI reads: voltage, rate, cycle count, capacities) -> AcpiBatteryReading
    ↓
BatterySnapshotCalculator.Compute()  [PowerID.Core - pure, unit tested]
    ↓
BatteryMonitor (publishes the resulting BatterySnapshot on the UI dispatcher)
    ↓
Pages/Controls (x:Bind, OneWay)
    ↓
BatteryFormatter -> BatteryColorLogic  [PowerID.Core - pure, unit tested]
```

## Testing / TDD

`PowerID.Core` has no dependency on Windows, WinUI, or WMI - it's plain `net8.0`, so
`PowerID.Tests` runs anywhere, including in CI on `ubuntu-latest` (see the `test` job in
`.github/workflows/build-release.yml`), well before the slower Windows/WinUI build job even starts.

When adding or changing battery math or formatting rules:
1. Write a failing test in `PowerID.Tests` against `BatterySnapshotCalculator` or `BatteryColorLogic` first.
2. Make it pass with the smallest change to `PowerID.Core`.
3. Only then wire the result into `BatteryMonitor`/`BatteryFormatter` and the XAML that binds to it.

Keep new business logic in `PowerID.Core` rather than inline in `BatteryMonitor.cs` or XAML
code-behind whenever it's pure (no I/O, no WinUI types) - that's what keeps it testable without a
Windows host.

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
therefore derived from wattage and voltage (`ChargeRate / Voltage`) rather than read directly - see
`BatterySnapshotCalculator.ComputeWattageAndAmperage` (and its unit tests) in `PowerID.Core`. Battery temperature also
isn't exposed by every OEM's firmware; when the `BatteryTemperature` WMI class is absent, the value falls back to 0,
matching the "Unknown" fallback behavior of the macOS app when a key is missing.

## Adding New Features

### Adding a New Page
1. Create the `.xaml`/`.xaml.cs` pair under the appropriate `Views/` subfolder.
2. Follow the existing `BatteryMonitor` property + `Bindings.Update()` pattern used by `OverviewPage`/`DetailsPage`
   so `x:Bind Mode=OneWay` bindings refresh once the view model is attached after navigation.
3. Reuse existing `Views/Components` controls where possible.

### Adding New Battery Data
1. Add the raw field to `AcpiBatteryReading` (`PowerID.Core/Models/BatteryInfo.cs`) and the WMI query
   that fills it in `BatteryService.cs`.
2. Write a failing test in `PowerID.Tests/BatterySnapshotCalculatorTests.cs` for the new derived value,
   then add it to `BatterySnapshot` and compute it in `BatterySnapshotCalculator.Compute()` until it passes.
3. Add a published property to `BatteryMonitor.cs` that reads it off the computed snapshot.
4. Bind to it from the relevant page via `x:Bind`.

### Adding a New Component
1. Create a `UserControl` in `Views/Components/`.
2. Expose simple `DependencyProperty` values (string/int/Brush) rather than accepting arbitrary child content,
   matching the pattern used by `StatusCard`/`DetailRow`/`InfoSection`.
