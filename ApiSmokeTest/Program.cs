using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string baseUrl = "https://www.foxesscloud.com";
const string deviceListPath = "/op/v0/device/list";
const string deviceDetailPath = "/op/v1/device/detail";

var apiKey = GetSetting(
    "FOXESS_API_KEY",
    args,
    "--api-key")?.Trim();

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Missing API key.");
    Console.Error.WriteLine("Set FOXESS_API_KEY or pass --api-key <value>.");
    return 1;
}

var inverterSn = GetSetting(
    "FOXESS_INVERTER_SN",
    args,
    "--sn")?.Trim();

var debug = args.Any(arg => string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase));

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
    "User-Agent",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");

try
{
    Console.WriteLine("Calling FoxESS device list...");
    var deviceList = await PostAsync<DeviceListResponse>(
        httpClient,
        deviceListPath,
        apiKey,
        debug,
        new
        {
            currentPage = 1,
            pageSize = 10
        });
    var deviceListResult = deviceList.Result
        ?? throw new FoxEssApiException("FoxESS API returned success without device list data.");

    Console.WriteLine(
        $"Success. Returned {deviceListResult.Data.Count} device(s) " +
        $"out of {deviceListResult.Total}.");

    foreach (var device in deviceListResult.Data)
    {
        Console.WriteLine(
            $"- DeviceSN={device.DeviceSn}, Station={device.StationName}, " +
            $"Model={device.DeviceType}, HasBattery={device.HasBattery}");
    }

    if (string.IsNullOrWhiteSpace(inverterSn))
    {
        Console.WriteLine();
        Console.WriteLine(
            "Set FOXESS_INVERTER_SN or pass --sn <serial> to also call device/detail.");
        return 0;
    }

    Console.WriteLine("~~~~~~~~~~");


    Console.WriteLine($"Calling FoxESS device detail for inverter {inverterSn}...");

    var detailPathWithQuery = $"{deviceDetailPath}?sn={Uri.EscapeDataString(inverterSn)}";

    var detail = await GetAsync<DeviceDetailResponse>(
        httpClient,
        detailPathWithQuery,
        deviceDetailPath,
        apiKey,
        debug);
    var detailResult = detail.Result
        ?? throw new FoxEssApiException("FoxESS API returned success without device detail data.");

    Console.WriteLine(
        $"Status={detailResult.Status}, Capacity={detailResult.Capacity}kW, " +
        $"SchedulerSupported={detailResult.Function.Scheduler}");

    if (detailResult.BatteryList.Count == 0)
    {
        Console.WriteLine("No batteries were returned by device detail.");
    }
    else
    {
        Console.WriteLine("Batteries:");
        foreach (var battery in detailResult.BatteryList)
        {
            Console.WriteLine(
                $"- BatterySN={battery.BatterySn}, Type={battery.Type}, " +
                $"Model={battery.Model}, Capacity={battery.Capacity}");
        }
    }

    return 0;
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

static async Task<T> PostAsync<T>(
    HttpClient httpClient,
    string path,
    string apiKey,
    bool debug,
    object body)
{
    using var request = new HttpRequestMessage(HttpMethod.Post, path);
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var signature = CreateSignature(path, apiKey, timestamp);

    request.Headers.TryAddWithoutValidation("token", apiKey);
    request.Headers.TryAddWithoutValidation("timestamp", timestamp);
    request.Headers.TryAddWithoutValidation("lang", "en");
    request.Headers.TryAddWithoutValidation("signature", signature);
    request.Content = JsonContent.Create(body);

    if (debug)
    {
        Console.WriteLine($"Debug path: {path}");
        Console.WriteLine($"Debug timestamp: {timestamp}");
        Console.WriteLine($"Debug token length: {apiKey.Length}");
        Console.WriteLine($"Debug token preview: {Mask(apiKey)}");
        Console.WriteLine($"Debug signature: {signature}");
    }

    using var response = await httpClient.SendAsync(request);
    var rawJson = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new FoxEssApiException(
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Body: {rawJson}");
    }

    var errorEnvelope = JsonSerializer.Deserialize<FoxEssErrorEnvelope>(rawJson, JsonOptions.Default);
    if (errorEnvelope is null)
    {
        throw new FoxEssApiException("FoxESS API returned an empty response body.");
    }

    if (errorEnvelope.Errno != 0)
    {
        throw new FoxEssApiException(
            $"errno={errorEnvelope.Errno}, msg={errorEnvelope.Msg ?? "(no message)"}");
    }

    var result = JsonSerializer.Deserialize<T>(rawJson, JsonOptions.Default);
    if (result is null)
    {
        throw new FoxEssApiException($"FoxESS API returned unexpected JSON: {rawJson}");
    }

    return result;
}

static async Task<T> GetAsync<T>(
    HttpClient httpClient,
    string path,
    string sigPath,
    string apiKey,
    bool debug)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, path);
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var signature = CreateSignature(sigPath, apiKey, timestamp);

    request.Headers.TryAddWithoutValidation("token", apiKey);
    request.Headers.TryAddWithoutValidation("timestamp", timestamp);
    request.Headers.TryAddWithoutValidation("lang", "en");
    request.Headers.TryAddWithoutValidation("signature", signature);

    if (debug)
    {
        Console.WriteLine($"Debug path: {path}");
        Console.WriteLine($"Debug timestamp: {timestamp}");
        Console.WriteLine($"Debug token length: {apiKey.Length}");
        Console.WriteLine($"Debug token preview: {Mask(apiKey)}");
        Console.WriteLine($"Debug signature: {signature}");
    }

    using var response = await httpClient.SendAsync(request);
    var rawJson = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new FoxEssApiException(
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Body: {rawJson}");
    }

    var errorEnvelope = JsonSerializer.Deserialize<FoxEssErrorEnvelope>(rawJson, JsonOptions.Default);
    if (errorEnvelope is null)
    {
        throw new FoxEssApiException("FoxESS API returned an empty response body.");
    }

    if (errorEnvelope.Errno != 0)
    {
        throw new FoxEssApiException(
            $"errno={errorEnvelope.Errno}, msg={errorEnvelope.Msg ?? "(no message)"}");
    }

    var result = JsonSerializer.Deserialize<T>(rawJson, JsonOptions.Default);
    if (result is null)
    {
        throw new FoxEssApiException($"FoxESS API returned unexpected JSON: {rawJson}");
    }

    return result;
}

static string CreateSignature(string path, string apiKey, string timestamp)
{
    var payload = $"{path}\\r\\n{apiKey}\\r\\n{timestamp}";
    var bytes = Encoding.UTF8.GetBytes(payload);
    var hash = MD5.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static string? GetSetting(string envVarName, IReadOnlyList<string> args, string optionName)
{
    var envValue = Environment.GetEnvironmentVariable(envVarName);
    if (!string.IsNullOrWhiteSpace(envValue))
    {
        return envValue;
    }

    for (var index = 0; index < args.Count - 1; index++)
    {
        if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return null;
}

static string Mask(string value)
{
    if (value.Length <= 8)
    {
        return new string('*', value.Length);
    }

    return $"{value[..4]}...{value[^4..]}";
}

static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

interface IFoxEssResponse
{
    int Errno { get; }
    string? Msg { get; }
}

sealed class DeviceListResponse : IFoxEssResponse
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
    public DeviceListResult? Result { get; init; }
}

sealed class DeviceListResult
{
    public int Total { get; init; }
    public required List<DeviceListItem> Data { get; init; }
}

sealed class DeviceListItem
{
    [JsonPropertyName("deviceSN")]
    public required string DeviceSn { get; init; }

    public string? StationName { get; init; }
    public string? DeviceType { get; init; }
    public bool HasBattery { get; init; }
}

sealed class DeviceDetailResponse : IFoxEssResponse
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
    public DeviceDetailResult? Result { get; init; }
}

sealed class DeviceDetailResult
{
    public int Status { get; init; }
    public decimal Capacity { get; init; }
    public required DeviceFunction Function { get; init; }
    public required List<BatteryItem> BatteryList { get; init; }
}

sealed class DeviceFunction
{
    public bool Scheduler { get; init; }
}

sealed class BatteryItem
{
    [JsonPropertyName("batterySN")]
    public required string BatterySn { get; init; }

    public string? Type { get; init; }
    public string? Model { get; init; }

    [JsonPropertyName("capicty")]
    public int Capacity { get; init; }
}

sealed class FoxEssErrorEnvelope
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
}

sealed class FoxEssApiException(string message) : Exception(message);
