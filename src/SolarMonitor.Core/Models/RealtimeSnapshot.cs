namespace SolarMonitor.Core.Models;

public sealed record RealtimeSnapshot(
    string DeviceSerialNumber,
    DateTimeOffset? ObservedAt,
    BatterySnapshot Battery,
    PowerFlowSnapshot PowerFlow,
    IReadOnlyDictionary<string, MetricReading> Metrics);
