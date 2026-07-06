# PowerID for Windows - Project Notes

Windows port of [PowerID for Mac](https://github.com/ogden-marrow/PowerID_For_Mac) (a native battery
monitor). This port is WinUI 3 on .NET 8, built to mirror the Mac app's structure and features as
closely as the platform allows. See `README.md` for user-facing docs and `ARCHITECTURE.md` for the
full project structure, MVVM breakdown, and the macOS→Windows API mapping table.

## Solution layout

- `PowerID.Core/` - plain `net8.0` class library, **zero Windows/WinUI dependency**. Holds the
  battery math (`BatterySnapshotCalculator`) and color/formatting rules (`BatteryColorLogic`) as
  pure functions. This is deliberate: it's what makes the logic unit-testable without a Windows host.
- `PowerID.Tests/` - xUnit tests against `PowerID.Core` only. Runs on any OS, including Linux CI.
- `PowerID/` - the actual WinUI 3 packaged app (MSIX), `net8.0-windows10.0.19041.0`.

## Required local environment

This is a genuine WinUI 3 desktop app - `PowerID/` **cannot be built, run, or debugged on Linux/Mac**.
You need:
- Windows 10 (build 17763+) or Windows 11
- .NET 8 SDK
- Visual Studio 2022 (17.8+) with the **Windows App SDK** and **.NET Desktop Development** workloads
- Python 3 + Pillow (`pip install Pillow`) only if regenerating icons via `generate_icon.py`

`PowerID.Core` and `PowerID.Tests` build and run fine on any OS with just the .NET 8 SDK
(`dotnet test PowerID.Tests/PowerID.Tests.csproj`).

## Known build gotcha (already fixed once, watch for regressions)

**Do not build/publish this solution with plain `dotnet build` / `dotnet publish`.** The .NET SDK's
own MSBuild host has proven unreliable at launching `XamlCompiler.exe` (a .NET Framework 4.7.2
external tool that WinUI 3's `MarkupCompilePass1` target shells out to) - it fails instantly with a
bare exit code and zero diagnostic output. Always use the real `msbuild.exe` (from the Visual
Studio/Build Tools install) instead:

```powershell
dotnet restore PowerID.sln
msbuild PowerID.sln /p:Configuration=Release /p:Platform=x64
```

CI (`.github/workflows/build-release.yml`) does this via `microsoft/setup-msbuild@v2` in the
`build` and `publish` jobs. If you ever see MSB3073 with a bare "exited with code 1" and no
compiler output, this is almost certainly the cause - check you're invoking `msbuild`, not
`dotnet build`/`dotnet publish`, for anything touching the `PowerID` project.

Also: every `Page`/`UserControl` XAML root must declare `xmlns:d` if it declares
`mc:Ignorable="d"` (or just omit `mc:Ignorable="d"` entirely if no `d:`-prefixed attributes are
used) - WinUI 3's XAML compiler treats an undeclared "d" prefix as a hard error
(`WMC9999: 'd' prefix is not defined`), unlike WPF's more lenient XAML parser. This bit us once
already in `OverviewPage.xaml`/`DetailsPage.xaml`/`InfoPage.xaml`.

## CI pipeline

Three jobs in `.github/workflows/build-release.yml`:
1. `test` (ubuntu-latest) - `dotnet test` on `PowerID.Tests`. Fast, no Windows needed.
2. `build` (windows-2022, needs `test`) - `msbuild` build of the full solution.
3. `publish` (windows-2022, needs `build`, tags/`workflow_dispatch` only) - self-contained
   unpackaged (`WindowsPackageType=None`) publish + zip, attached to GitHub Releases on `v*` tags.

As of the last push, `test` is green in CI; `build` was failing on the `XamlCompiler.exe`
invocation issue above, which was fixed by switching to `msbuild.exe` - check the latest run at
https://github.com/ogden-marrow/PowerID_For_Windows/actions to see if it's gone fully green, and
whether the follow-up `mc:Ignorable="d"` fix (commit `565838f`) resolved the next error that
surfaced (`WMC9999: 'd' prefix is not defined`).

## Workflow / conventions

- **TDD for `PowerID.Core`**: write a failing test in `PowerID.Tests` first, make it pass with the
  smallest change to `PowerID.Core`, then wire the result into `BatteryMonitor`/`BatteryFormatter`
  and the XAML that binds to it. Keep new pure logic in `PowerID.Core`, not inline in
  `BatteryMonitor.cs` or XAML code-behind.
- No third-party UI dependencies in the `PowerID` app project - pure WinUI 3 + Win32 P/Invoke
  (`TrayIconService.cs` hand-rolls the system tray icon via `Shell_NotifyIcon` since WinUI 3 has no
  built-in notification-area API), matching the Mac app's "no Electron, no third-party frameworks" stance.
- Never commit `bin/`/`obj/` output (already in `.gitignore`).
- This repo (`ogden-marrow/PowerID_For_Windows`) is separate from the Mac repo
  (`ogden-marrow/PowerID_For_Mac`) - they're sibling projects, not a monorepo.
