namespace SolarMonitor.Core.Models;

public sealed record DeviceDetail(
    string DeviceSerialNumber,
    int Status,
    decimal CapacityKilowatts,
    DeviceFunctions Functions,
    IReadOnlyList<BatteryModule> Batteries);
