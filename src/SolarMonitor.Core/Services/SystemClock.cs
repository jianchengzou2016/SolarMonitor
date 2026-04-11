using SolarMonitor.Core.Abstractions;

namespace SolarMonitor.Core.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
