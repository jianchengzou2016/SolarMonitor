// See https://aka.ms/new-console-template for more information
using SolarMonitor.Core.Services;
using SolarMonitor.FoxEss;

var options = ParseArguments(args);
if (options.ShowHelp || string.IsNullOrWhiteSpace(options.Command))
{
    PrintHelp();
    return 0;
}

var apiKey = GetOptionValue(options, "api-key") ?? Environment.GetEnvironmentVariable("FOXESS_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Missing FoxESS API key. Use --api-key or set FOXESS_API_KEY.");
    return 1;
}

var inverterSn = GetOptionValue(options, "sn") ?? Environment.GetEnvironmentVariable("FOXESS_INVERTER_SN");
var baseUrl = GetOptionValue(options, "base-url") ?? Environment.GetEnvironmentVariable("FOXESS_BASE_URL") ?? FoxEssOptions.DefaultBaseUrl;

using var client = new FoxEssClient(
    new FoxEssOptions
    {
        ApiKey = apiKey.Trim(),
        BaseUrl = baseUrl.Trim()
    },
    new SystemClock());

try
{
    switch (options.Command)
    {
        case "devices":
            await PrintDevicesAsync(client);
            return 0;

        case "detail":
            if (string.IsNullOrWhiteSpace(inverterSn))
            {
                Console.Error.WriteLine("Missing inverter serial number. Use --sn or set FOXESS_INVERTER_SN.");
                return 1;
            }

            await PrintDetailAsync(client, inverterSn.Trim());
            return 0;

        case "realtime":
            if (string.IsNullOrWhiteSpace(inverterSn))
            {
                Console.Error.WriteLine("Missing inverter serial number. Use --sn or set FOXESS_INVERTER_SN.");
                return 1;
            }

            var variables = ParseCsvOption(options, "variables");
            await PrintRealtimeAsync(client, inverterSn.Trim(), variables);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown command '{options.Command}'.");
            PrintHelp();
            return 1;
    }
}
catch (FoxEssApiException ex)
{
    Console.Error.WriteLine($"FoxESS API error: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    return 3;
}

static async Task PrintDevicesAsync(FoxEssClient client)
{
    var devices = await client.ListDevicesAsync();
    Console.WriteLine($"Devices ({devices.Count}):");
    foreach (var device in devices)
    {
        Console.WriteLine(
            $"- SN={device.DeviceSerialNumber}, Station={device.StationName}, " +
            $"Model={device.DeviceModel}, Status={device.Status}, HasBattery={device.HasBattery}");
    }
}

static async Task PrintDetailAsync(FoxEssClient client, string inverterSn)
{
    var detail = await client.GetDeviceDetailAsync(inverterSn);
    Console.WriteLine($"Device: {detail.DeviceSerialNumber}");
    Console.WriteLine($"Status: {detail.Status}");
    Console.WriteLine($"Capacity: {detail.CapacityKilowatts} kW");
    Console.WriteLine($"Scheduler supported: {detail.Functions.SchedulerSupported}");
    Console.WriteLine($"Batteries ({detail.Batteries.Count}):");

    foreach (var battery in detail.Batteries)
    {
        Console.WriteLine(
            $"- BatterySN={battery.BatterySerialNumber}, Type={battery.Type}, " +
            $"Model={battery.Model}, Capacity={battery.Capacity}");
    }
}

static async Task PrintRealtimeAsync(FoxEssClient client, string inverterSn, IReadOnlyList<string> variables)
{
    var realtime = await client.GetRealtimeSnapshotAsync(
        inverterSn,
        variables.Count > 0 ? variables : null);

    Console.WriteLine($"Device: {realtime.DeviceSerialNumber}");
    Console.WriteLine($"Observed at: {realtime.ObservedAt?.ToString("u") ?? "(not supplied)"}");
    Console.WriteLine("Battery:");
    Console.WriteLine($"- SoC: {FormatValue(realtime.Battery.StateOfCharge, "%")}");
    Console.WriteLine($"- SOH: {FormatValue(realtime.Battery.StateOfHealth, "%")}");
    Console.WriteLine($"- Voltage: {FormatValue(realtime.Battery.Voltage, "V")}");
    Console.WriteLine($"- Current: {FormatValue(realtime.Battery.Current, "A")}");
    Console.WriteLine($"- Power: {FormatValue(realtime.Battery.Power, "W")}");
    Console.WriteLine($"- Charge power: {FormatValue(realtime.Battery.ChargePower, "W")}");
    Console.WriteLine($"- Discharge power: {FormatValue(realtime.Battery.DischargePower, "W")}");
    Console.WriteLine($"- Residual energy: {FormatValue(realtime.Battery.ResidualEnergy, "kWh")}");
    Console.WriteLine("Power flow:");
    Console.WriteLine($"- PV power: {FormatValue(realtime.PowerFlow.PvPower, "W")}");
    Console.WriteLine($"- Load power: {FormatValue(realtime.PowerFlow.LoadPower, "W")}");
    Console.WriteLine($"- Feed-in power: {FormatValue(realtime.PowerFlow.FeedInPower, "W")}");
    Console.WriteLine($"- Grid consumption power: {FormatValue(realtime.PowerFlow.GridConsumptionPower, "W")}");
    Console.WriteLine($"Metrics ({realtime.Metrics.Count}):");

    foreach (var metric in realtime.Metrics.Values.OrderBy(metric => metric.Variable, StringComparer.OrdinalIgnoreCase))
    {
        var suffix = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : $" {metric.Unit}";
        Console.WriteLine($"- {metric.Variable}: {metric.ValueText}{suffix}");
    }
}

static string FormatValue(decimal? value, string unit)
{
    return value.HasValue ? $"{value.Value} {unit}" : "(n/a)";
}

static IReadOnlyList<string> ParseCsvOption(CommandLineOptions options, string key)
{
    var rawValue = GetOptionValue(options, key);
    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return [];
    }

    return rawValue
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();
}

static string? GetOptionValue(CommandLineOptions options, string key)
{
    return options.Values.TryGetValue(key, out var value)
        ? value
        : null;
}

static CommandLineOptions ParseArguments(IReadOnlyList<string> args)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    string? command = null;
    var showHelp = false;

    for (var index = 0; index < args.Count; index++)
    {
        var arg = args[index];

        if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
        {
            showHelp = true;
            continue;
        }

        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            var key = arg[2..];
            if (index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[index + 1];
                index++;
            }
            else
            {
                values[key] = "true";
            }

            continue;
        }

        command ??= arg;
    }

    return new CommandLineOptions(command?.ToLowerInvariant(), showHelp, values);
}

static void PrintHelp()
{
    Console.WriteLine("SolarMonitor Phase 1 console");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  devices");
    Console.WriteLine("  detail --sn <inverter-serial>");
    Console.WriteLine("  realtime --sn <inverter-serial> [--variables SoC,invBatPower,pvPower]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --api-key <value>   FoxESS private API key");
    Console.WriteLine("  --base-url <value>  Override FoxESS base URL");
    Console.WriteLine("  --sn <value>        Inverter serial number");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  FOXESS_API_KEY");
    Console.WriteLine("  FOXESS_INVERTER_SN");
    Console.WriteLine("  FOXESS_BASE_URL");
}

internal sealed record CommandLineOptions(
    string? Command,
    bool ShowHelp,
    IReadOnlyDictionary<string, string> Values);
