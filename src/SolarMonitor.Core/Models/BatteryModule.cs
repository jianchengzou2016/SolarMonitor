namespace SolarMonitor.Core.Models;

public sealed record BatteryModule(
    string BatterySerialNumber,
    string Type,
    string Model,
    int Capacity);
