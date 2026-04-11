using System.Text.Json.Serialization;

namespace SolarMonitor.FoxEss.Dtos;

internal sealed class DeviceListResponseDto
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
    public DeviceListResultDto? Result { get; init; }
}

internal sealed class DeviceListResultDto
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public List<DeviceListItemDto> Data { get; init; } = [];
}

internal sealed class DeviceListItemDto
{
    [JsonPropertyName("deviceSN")]
    public string DeviceSn { get; init; } = string.Empty;

    [JsonPropertyName("moduleSN")]
    public string ModuleSn { get; init; } = string.Empty;

    [JsonPropertyName("stationID")]
    public string StationId { get; init; } = string.Empty;

    public string StationName { get; init; } = string.Empty;
    public int Status { get; init; }
    public bool HasPv { get; init; }
    public bool HasBattery { get; init; }
    public string DeviceType { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
}
