using SolarMonitor.Core.Abstractions;
using System.Collections.Concurrent;

namespace SolarMonitor.FoxEss;

public sealed class FoxEssRateLimiter
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequestTimes = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IClock _clock;
    private readonly TimeSpan _minimumInterval;

    public FoxEssRateLimiter(IClock clock, TimeSpan minimumInterval)
    {
        _clock = clock;
        _minimumInterval = minimumInterval;
    }

    public async Task WaitAsync(string key, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_lastRequestTimes.TryGetValue(key, out var lastRequestTime))
            {
                var dueAt = lastRequestTime + _minimumInterval;
                var delay = dueAt - _clock.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            _lastRequestTimes[key] = _clock.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }
}
