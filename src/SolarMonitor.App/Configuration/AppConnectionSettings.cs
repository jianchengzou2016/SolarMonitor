namespace SolarMonitor.App.Configuration;

public sealed record AppConnectionSettings(
    string ApiKey,
    string InverterSerialNumber);
