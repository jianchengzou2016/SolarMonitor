using SolarMonitor.Core.Models;

namespace SolarMonitor.Core.Abstractions;

public interface IFoxEssGateway
{
    Task<IReadOnlyList<DeviceSummary>> ListDevicesAsync(CancellationToken cancellationToken = default);

    Task<DeviceDetail> GetDeviceDetailAsync(
        string deviceSn,
        CancellationToken cancellationToken = default);

    Task<RealtimeSnapshot> GetRealtimeSnapshotAsync(
        string deviceSn,
        IReadOnlyList<string>? variables = null,
        CancellationToken cancellationToken = default);
}
