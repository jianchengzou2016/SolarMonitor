namespace SolarMonitor.Core.Models;

public sealed record BatterySnapshot(
    decimal? StateOfCharge,
    decimal? StateOfHealth,
    decimal? Voltage,
    decimal? Current,
    decimal? Power,
    decimal? ChargePower,
    decimal? DischargePower,
    decimal? ResidualEnergy);
