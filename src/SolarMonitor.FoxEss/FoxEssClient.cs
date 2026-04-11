using System.Net.Http.Json;
using System.Text.Json;
using SolarMonitor.Core.Abstractions;
using SolarMonitor.Core.Models;
using SolarMonitor.FoxEss.Dtos;

namespace SolarMonitor.FoxEss;

public sealed class FoxEssClient : IFoxEssGateway, IDisposable
{
    private const string DeviceListPath = "/op/v0/device/list";
    private const string DeviceDetailPath = "/op/v1/device/detail";
    private const string DeviceRealtimePath = "/op/v1/device/real/query";

    private readonly HttpClient _httpClient;
    private readonly FoxEssOptions _options;
    private readonly FoxEssRateLimiter _rateLimiter;
    private readonly bool _disposeHttpClient;

    public FoxEssClient(FoxEssOptions options, IClock clock, HttpClient? httpClient = null)
    {
        _options = options;
        _rateLimiter = new FoxEssRateLimiter(clock, options.MinimumInterval);
        _disposeHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
    }

    public async Task<IReadOnlyList<DeviceSummary>> ListDevicesAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<DeviceListResponseDto>(
            HttpMethod.Post,
            DeviceListPath,
            DeviceListPath,
            new { currentPage = 1, pageSize = 100 },
            cancellationToken);

        return response.Result?.Data.Select(dto => dto.ToDomain()).ToArray()
            ?? [];
    }

    public async Task<DeviceDetail> GetDeviceDetailAsync(
        string deviceSn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSn))
        {
            throw new ArgumentException("Device serial number is required.", nameof(deviceSn));
        }

        var requestPath = $"{DeviceDetailPath}?sn={Uri.EscapeDataString(deviceSn)}";
        var response = await SendAsync<DeviceDetailResponseDto>(
            HttpMethod.Get,
            requestPath,
            DeviceDetailPath,
            body: null,
            cancellationToken);

        return response.Result?.ToDomain(deviceSn)
            ?? throw new FoxEssApiException("FoxESS returned success without device detail data.");
    }

    public async Task<RealtimeSnapshot> GetRealtimeSnapshotAsync(
        string deviceSn,
        IReadOnlyList<string>? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSn))
        {
            throw new ArgumentException("Device serial number is required.", nameof(deviceSn));
        }

        var body = new Dictionary<string, object?>
        {
            ["sn"] = deviceSn
        };

        if (variables is { Count: > 0 })
        {
            body["variables"] = variables;
        }

        var response = await SendAsync<RealtimeResponseDto>(
            HttpMethod.Post,
            DeviceRealtimePath,
            DeviceRealtimePath,
            body,
            cancellationToken);

        var snapshot = response.Result
            .FirstOrDefault(result => string.Equals(result.DeviceSn, deviceSn, StringComparison.OrdinalIgnoreCase));

        return snapshot?.ToDomain()
            ?? throw new FoxEssApiException($"FoxESS returned success without realtime data for inverter {deviceSn}.");
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string requestPath,
        string signaturePath,
        object? body,
        CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(signaturePath, cancellationToken);

        using var request = new HttpRequestMessage(method, requestPath);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signature = FoxEssSignatureBuilder.CreateSignature(signaturePath, _options.ApiKey, timestamp);

        request.Headers.TryAddWithoutValidation("token", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("lang", _options.Language);
        request.Headers.TryAddWithoutValidation("signature", signature);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new FoxEssApiException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Body: {rawJson}");
        }

        var errorEnvelope = JsonSerializer.Deserialize<FoxEssErrorEnvelope>(rawJson, FoxEssJson.SerializerOptions);
        if (errorEnvelope is null)
        {
            throw new FoxEssApiException("FoxESS returned an empty response body.");
        }

        if (errorEnvelope.Errno != 0)
        {
            throw new FoxEssApiException(
                $"errno={errorEnvelope.Errno}, msg={errorEnvelope.Msg ?? "(no message)"}");
        }

        var result = JsonSerializer.Deserialize<T>(rawJson, FoxEssJson.SerializerOptions);
        if (result is null)
        {
            throw new FoxEssApiException("FoxESS returned an empty success payload.");
        }

        return result;
    }

    private sealed class FoxEssErrorEnvelope
    {
        public int Errno { get; init; }
        public string? Msg { get; init; }
    }
}
