using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarMonitor.FoxEss.Dtos;

internal sealed class RealtimeResponseDto
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
    public List<RealtimeDeviceDto> Result { get; init; } = [];
}

internal sealed class RealtimeDeviceDto
{
    [JsonPropertyName("deviceSN")]
    public string DeviceSn { get; init; } = string.Empty;

    public List<RealtimeMetricDto> Datas { get; init; } = [];
}

internal sealed class RealtimeMetricDto
{
    public string Variable { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public JsonElement Value { get; init; }
    public string Time { get; init; } = string.Empty;
}
