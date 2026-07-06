# PowerID ⚡ (Windows)

<div align="center">

![PowerID Icon](https://img.shields.io/badge/Windows-10%2FSlash%2011-blue?style=for-the-badge&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Advanced Battery Monitoring for Windows**

A native WinUI 3 app that provides detailed insights into your PC's battery health, performance, and power
consumption. This is the Windows counterpart to [PowerID for Mac](https://github.com/ogden-marrow/PowerID_For_Mac),
rebuilt from scratch on the Windows App SDK (no Electron, no web views).

[Features](#-features) • [Installation](#-installation) • [Building](#-building-from-source) • [Architecture](ARCHITECTURE.md)

</div>

---

## ✨ Features

### Real-Time Monitoring
- Battery information refreshes every 2 seconds (configurable in Settings)
- Reads directly from the ACPI battery WMI classes (`root\WMI`)
- Lightweight native C# implementation, no background services

### Comprehensive Battery Information

#### 📊 Overview Tab
- Battery level with a dynamic color gradient
- Charging status and charging power (W)
- Estimated time to full charge
- Battery health at a glance
- Cycle count

#### 📋 Details Tab
- **Capacity**: Current / Maximum / Design capacity (mAh), battery level (%)
- **Electrical**: Voltage (V), Amperage (mA), Power draw (W)
- **Health & Lifecycle**: Health (%), cycle count, temperature (°C, where the firmware reports it)
- **Power Source**: Charging status, power source type, time to full charge

#### ℹ️ Info Tab
Plain-language explanations of every metric plus battery care tips.

### Native Windows Experience
- Pure WinUI 3 (Fluent Design), no third-party UI frameworks
- System tray icon showing live battery % (`Services/TrayIconService.cs`), with a right-click menu for status,
  health, show/quit
- Dark/light theme aware
- Optional "close to tray" behavior, same as the menu bar toggle on macOS

---

## 🚀 Installation

### Option 1: Download a Release

1. Go to the [Releases](https://github.com/ogden-marrow/PowerID_For_Windows/releases) page
2. Download `PowerID-win-x64.zip`
3. Unzip it anywhere and run `PowerID.exe`

#### ⚠️ First Launch
Since the app is unsigned, Windows SmartScreen may show a warning the first time you run it:
1. Click **"More info"**
2. Click **"Run anyway"**

### Option 2: Build from Source

See [Building from Source](#-building-from-source) below.

---

## 🛠 Building from Source

### Prerequisites

- Windows 10 (build 17763+) or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) with the **Windows App SDK** and **.NET Desktop Development** workloads
  *(or the standalone [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/) tooling)*
- Python 3 + [Pillow](https://pypi.org/project/Pillow/) (only needed to regenerate app icons)

### Build Steps

1. **Clone the repository**
   ```powershell
   git clone https://github.com/ogden-marrow/PowerID_For_Windows.git
   cd PowerID_For_Windows
   ```

2. **Generate app icons** (optional, already committed under `PowerID/Assets`)
   ```powershell
   pip install Pillow
   python generate_icon.py
   ```

3. **Open in Visual Studio**
   ```powershell
   start PowerID.sln
   ```

4. **Build and Run**
   - Set the platform to `x64` (or `arm64`)
   - Press `F5` to build and run, or `Ctrl+Shift+B` to build only

### Command Line Build

```powershell
dotnet restore PowerID.sln
dotnet build PowerID.sln -c Release -p:Platform=x64
```

### Unpackaged, self-contained publish (no MSIX/signing required)

```powershell
dotnet publish PowerID/PowerID.csproj -c Release -p:Platform=x64 `
  -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true `
  -r win-x64 -o publish/PowerID
```

The runnable app will be at `publish/PowerID/PowerID.exe`.

---

## 🏗 Project Structure

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full breakdown, MVVM structure, and a macOS → Windows API mapping
table (IOKit ↔ WMI, `NSStatusItem` ↔ `Shell_NotifyIcon`, etc.).

---

## 🔧 Technical Details

### Battery Metrics Explained

| Metric | Description | Source |
|--------|-------------|--------|
| Battery Level | Current charge as % of max capacity | `BatteryStatus` (WMI) |
| Current Capacity | Actual charge in mAh (derived from mWh / design voltage) | `BatteryStatus` |
| Max Capacity | Current full-charge capacity in mAh | `BatteryFullChargedCapacity` |
| Design Capacity | Original factory capacity in mAh | `BatteryStaticData` |
| Battery Health | (Max / Design) × 100% | Calculated |
| Cycle Count | Total charge-discharge cycles | `BatteryCycleCount` |
| Voltage | Battery voltage in volts | `BatteryStatus` |
| Amperage | Current flow in milliamps (derived from rate / voltage) | Calculated |
| Wattage | Charge/discharge rate in watts | `BatteryStatus` |
| Temperature | Battery temperature in Celsius, if reported by firmware | `BatteryTemperature` |

### System Requirements

- **Operating System**: Windows 10 (build 17763+) or Windows 11
- **Architecture**: x64 or ARM64
- **Hardware**: A PC/laptop with a battery

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Ideas for Contributions
- [ ] Low battery / fully-charged notifications (toast notifications)
- [ ] Real "Launch at login" registration (`StartupTask` API)
- [ ] Historical battery data tracking
- [ ] MSIX signing + Microsoft Store packaging
- [ ] Localization for other languages

---

## 📊 Automated Builds

PowerID uses GitHub Actions (`.github/workflows/build-release.yml`) to build on every push/PR, and to publish a
zipped release when a `v*` tag is pushed:

```powershell
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

---

## 🐛 Troubleshooting

### App Won't Open
**Problem**: Windows SmartScreen blocks the app ("Windows protected your PC")

**Solution**: Click "More info" then "Run anyway".

### Battery Data Not Showing
**Problem**: All values show as 0 or "Unknown"

**Solution**:
- Ensure you're running on a device with a battery
- Some OEM firmware doesn't expose `BatteryTemperature` over WMI - this is expected and shows as 0°C
- Try restarting the app

### Build Errors
**Problem**: Missing Windows App SDK workload / Windows 10 SDK

**Solution**: Install the "Windows application development" workload via the Visual Studio Installer, or install
the [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) directly.

---

## 📝 License

MIT License - see [LICENSE](LICENSE) for details.

---

## 📮 Contact

- **Issues**: [GitHub Issues](https://github.com/ogden-marrow/PowerID_For_Windows/issues)
- **macOS version**: [PowerID_For_Mac](https://github.com/ogden-marrow/PowerID_For_Mac)

---

<div align="center">

**Made with ⚡ for the Windows community**

</div>
