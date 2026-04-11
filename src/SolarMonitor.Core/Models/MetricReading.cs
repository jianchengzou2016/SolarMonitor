namespace SolarMonitor.Core.Models;

public sealed record MetricReading(
    string Variable,
    string DisplayName,
    string ValueText,
    decimal? NumericValue,
    string Unit,
    DateTimeOffset? ObservedAt);
