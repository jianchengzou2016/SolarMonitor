namespace SolarMonitor.Core.Models;

public sealed record DeviceSummary(
    string DeviceSerialNumber,
    string ModuleSerialNumber,
    string StationId,
    string StationName,
    string DeviceModel,
    string ProductSeries,
    DeviceOperationalStatus Status,
    bool HasPv,
    bool HasBattery);
