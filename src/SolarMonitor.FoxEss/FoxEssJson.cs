using System.Globalization;
using System.Text.Json;
using SolarMonitor.Core.Models;
using SolarMonitor.FoxEss.Dtos;

namespace SolarMonitor.FoxEss;

internal static class FoxEssJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static DeviceSummary ToDomain(this DeviceListItemDto dto)
    {
        return new DeviceSummary(
            dto.DeviceSn,
            dto.ModuleSn,
            dto.StationId,
            dto.StationName,
            dto.DeviceType,
            dto.ProductType,
            ToStatus(dto.Status),
            dto.HasPv,
            dto.HasBattery);
    }

    public static DeviceDetail ToDomain(this DeviceDetailResultDto dto, string deviceSn)
    {
        return new DeviceDetail(
            deviceSn,
            dto.Status,
            dto.Capacity,
            new DeviceFunctions(dto.Function.Scheduler),
            dto.BatteryList.Select(ToDomain).ToArray());
    }

    public static BatteryModule ToDomain(this BatteryItemDto dto)
    {
        return new BatteryModule(
            dto.BatterySn,
            dto.Type,
            dto.Model,
            dto.Capacity);
    }

    public static RealtimeSnapshot ToDomain(this RealtimeDeviceDto dto)
    {
        var metrics = dto.Datas
            .Select(ToDomain)
            .ToDictionary(metric => metric.Variable, StringComparer.OrdinalIgnoreCase);

        return new RealtimeSnapshot(
            dto.DeviceSn,
            metrics.Values
                .Where(metric => metric.ObservedAt.HasValue)
                .OrderByDescending(metric => metric.ObservedAt)
                .Select(metric => metric.ObservedAt)
                .FirstOrDefault(),
            new BatterySnapshot(
                TryGetNumeric(metrics, "SoC"),
                TryGetNumeric(metrics, "SOH"),
                TryGetNumeric(metrics, "invBatVolt") ?? TryGetNumeric(metrics, "batVolt"),
                TryGetNumeric(metrics, "invBatCurrent") ?? TryGetNumeric(metrics, "batCurrent"),
                TryGetNumeric(metrics, "invBatPower"),
                TryGetNumeric(metrics, "batChargePower"),
                TryGetNumeric(metrics, "batDischargePower"),
                TryGetNumeric(metrics, "ResidualEnergy")),
            new PowerFlowSnapshot(
                TryGetNumeric(metrics, "pvPower"),
                TryGetNumeric(metrics, "loadsPower"),
                TryGetNumeric(metrics, "feedinPower"),
                TryGetNumeric(metrics, "gridConsumptionPower")),
            metrics);
    }

    public static MetricReading ToDomain(this RealtimeMetricDto dto)
    {
        return new MetricReading(
            dto.Variable,
            dto.Name,
            GetValueText(dto.Value),
            TryGetNumericValue(dto.Value),
            dto.Unit,
            TryParseObservedAt(dto.Time));
    }

    private static DeviceOperationalStatus ToStatus(int rawStatus)
    {
        return Enum.IsDefined(typeof(DeviceOperationalStatus), rawStatus)
            ? (DeviceOperationalStatus)rawStatus
            : DeviceOperationalStatus.Unknown;
    }

    private static string GetValueText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => value.GetRawText()
        };
    }

    private static decimal? TryGetNumeric(IReadOnlyDictionary<string, MetricReading> metrics, string variable)
    {
        return metrics.TryGetValue(variable, out var reading)
            ? reading.NumericValue
            : null;
    }

    private static decimal? TryGetNumericValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
        {
            return numeric;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out numeric))
        {
            return numeric;
        }

        return null;
    }

    private static DateTimeOffset? TryParseObservedAt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
