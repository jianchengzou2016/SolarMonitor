# SolarMonitor

Small FoxESS experiments live here.

## ApiSmokeTest

Console app for testing FoxESS Open API connectivity with the private API key header flow.

### Usage

Set environment variables:

```powershell
$env:FOXESS_API_KEY = "your-private-api-key"
$env:FOXESS_INVERTER_SN = "optional-inverter-serial"
dotnet run --project .\SolarMonitor\ApiSmokeTest\
```

Or pass arguments:

```powershell
dotnet run --project .\SolarMonitor\ApiSmokeTest\ -- --api-key "your-private-api-key" --sn "optional-inverter-serial"
```

The sample always calls `device/list` first. If an inverter serial number is provided, it also calls `GET /op/v1/device/detail?sn=...`.

### Debug mode

To print the path, timestamp, masked token, and generated signature:

```powershell
dotnet run --project .\SolarMonitor\ApiSmokeTest\ -- --debug
```

FoxESS note:

- The signature that worked in practice here uses the literal text `\\r\\n` between path, token, and timestamp.
- Query endpoints are rate-limited to roughly one call per second per interface.
