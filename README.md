# SolarMonitor

`SolarMonitor` is a small FoxESS monitoring app built around:
- a reusable FoxESS client library
- a console exploration tool
- a WPF desktop app
- Windows installer packaging with Inno Setup

## Solution layout

- `src/SolarMonitor.Core`
- `src/SolarMonitor.FoxEss`
- `src/SolarMonitor.Console`
- `src/SolarMonitor.App`
- `tests/SolarMonitor.FoxEss.Tests`
- `tests/SolarMonitor.App.Tests`
- `packaging/windows`

## Implemented endpoints

- `POST /op/v0/device/list`
- `GET /op/v1/device/detail?sn=...`
- `POST /op/v1/device/real/query`

## Running locally

### Console usage

Set environment variables:

```powershell
$env:FOXESS_API_KEY = "your-private-api-key"
$env:FOXESS_INVERTER_SN = "your-inverter-serial"
```

List devices:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- devices
```

Get device detail:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- detail
```

Get realtime values:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- realtime
```

Get selected realtime variables:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- realtime --variables SoC,invBatPower,pvPower
```

Launch the WPF app:

```powershell
dotnet run --project .\src\SolarMonitor.App\
```

Or pass options directly:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- detail --api-key "your-private-api-key" --sn "your-inverter-serial"
```

## FoxESS notes

- The signature that worked in practice here uses the literal text `\\r\\n` between path, token, and timestamp.
- For query-string requests like `device/detail`, the request URL includes the query string but the signature is generated from the base path only.
- Query endpoints are rate-limited to roughly one call per second per interface, and the client enforces a small minimum interval by default.
- The WPF app is intentionally thin and reuses the same `SolarMonitor.FoxEss` client as the console.

## Local app settings

- The WPF app now loads and saves the inverter serial number in `%LocalAppData%\SolarMonitor\appsettings.json`.
- The FoxESS API key is stored separately in `%LocalAppData%\SolarMonitor\secrets.dat` using Windows DPAPI for the current user.
- Existing `FOXESS_API_KEY` and `FOXESS_INVERTER_SN` environment variables are still used as fallback values when no saved settings exist yet.

## Build the installer

Build the Windows installer:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\windows\Build-SolarMonitorInstaller.ps1
```

Expected output:

- published app files under `artifacts\publish\win-x64`
- installer under `artifacts\installer`

## Release versioning

Installer and app packaging versioning are driven by `MinVer`, which reads git tags.

Rules:

- use annotated or lightweight git tags with the `v` prefix
- example release tags:
  - `v1.0.0`
  - `v1.0.1`
  - `v1.1.0`
- if no matching tag exists, MinVer falls back to an auto-generated development version

Typical release flow:

```powershell
git tag v1.0.0
git push origin v1.0.0
powershell -ExecutionPolicy Bypass -File .\packaging\windows\Build-SolarMonitorInstaller.ps1
```

If you want to build a specific version manually without creating a tag first:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\windows\Build-SolarMonitorInstaller.ps1 -Version 1.0.0
```

The generated installer filename follows this pattern:

- `SolarMonitor-Setup-<version>-win-x64.exe`

## Installer notes

- Installer packaging uses Inno Setup under `packaging\windows`.
- The installer checks for the .NET 8 Windows Desktop Runtime and can attempt installation with `winget` before continuing.
- After installation, the wizard offers to launch `SolarMonitor`.
