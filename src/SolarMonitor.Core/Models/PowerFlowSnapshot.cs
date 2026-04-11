namespace SolarMonitor.Core.Models;

public sealed record PowerFlowSnapshot(
    decimal? PvPower,
    decimal? LoadPower,
    decimal? FeedInPower,
    decimal? GridConsumptionPower);
