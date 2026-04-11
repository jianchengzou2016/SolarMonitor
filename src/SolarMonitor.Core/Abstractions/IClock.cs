namespace SolarMonitor.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
