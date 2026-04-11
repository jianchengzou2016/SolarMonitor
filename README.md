# SolarMonitor

Phase 1 is a small read-only FoxESS exploration tool with a reusable client layer and a console entry point.

## Solution layout

- `src/SolarMonitor.Core`
- `src/SolarMonitor.FoxEss`
- `src/SolarMonitor.Console`
- `tests/SolarMonitor.FoxEss.Tests`

## Implemented endpoints

- `POST /op/v0/device/list`
- `GET /op/v1/device/detail?sn=...`
- `POST /op/v1/device/real/query`

## Console usage

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

Or pass options directly:

```powershell
dotnet run --project .\src\SolarMonitor.Console\ -- detail --api-key "your-private-api-key" --sn "your-inverter-serial"
```

## FoxESS notes

- The signature that worked in practice here uses the literal text `\\r\\n` between path, token, and timestamp.
- For query-string requests like `device/detail`, the request URL includes the query string but the signature is generated from the base path only.
- Query endpoints are rate-limited to roughly one call per second per interface, and the client enforces a small minimum interval by default.
