using System.Text.Json.Serialization;

namespace SolarMonitor.FoxEss.Dtos;

internal sealed class DeviceDetailResponseDto
{
    public int Errno { get; init; }
    public string? Msg { get; init; }
    public DeviceDetailResultDto? Result { get; init; }
}

internal sealed class DeviceDetailResultDto
{
    public int Status { get; init; }
    public decimal Capacity { get; init; }
    public DeviceFunctionsDto Function { get; init; } = new();
    public List<BatteryItemDto> BatteryList { get; init; } = [];
}

internal sealed class DeviceFunctionsDto
{
    public bool Scheduler { get; init; }
}

internal sealed class BatteryItemDto
{
    [JsonPropertyName("batterySN")]
    public string BatterySn { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("capicty")]
    public int Capacity { get; init; }
}
