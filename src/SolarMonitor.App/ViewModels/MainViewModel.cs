using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Text.Json;
using SolarMonitor.Core.Models;
using SolarMonitor.Core.Services;
using SolarMonitor.FoxEss;
using SolarMonitor.App.Configuration;
using SolarMonitor.App.Localization;

namespace SolarMonitor.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const double DailyChartWidth = 1440;
    private const double MinimumChartHeight = 320;
    private const double ChartTickStepKilowatts = 0.5;
    private const double HeightPerTick = 44;
    private const double ChartHorizontalPaddingPixels = 24;
    private static readonly TimeSpan MaximumConnectedSampleGap = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ConnectedSampleGapTolerance = TimeSpan.FromSeconds(30);
    private const int MaxTrendSamples = 180;
    private static readonly string TrendHistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SolarMonitor",
        "trend-history.json");

    private readonly ConnectionSettingsStore _settingsStore;
    private string _apiKey = string.Empty;
    private string _inverterSerialNumber = string.Empty;
    private string _languageCode = AppText.DefaultLanguageCode;
    private IReadOnlyDictionary<string, string> _texts = AppText.GetStrings(AppText.DefaultLanguageCode);
    private string _statusMessage = "Ready";
    private string _detailSummaryText = "No device detail loaded yet.";
    private string _detailFunctionsText = string.Empty;
    private string _batterySocText = "(n/a)";
    private string _batteryTemperatureText = "(n/a)";
    private string _batterySohText = "(n/a)";
    private string _batteryDischargeText = "(n/a)";
    private string _homeUsageText = "(n/a)";
    private string _pvGeneratedText = "(n/a)";
    private string _gridImportText = "(n/a)";
    private string _gridExportText = "(n/a)";
    private string _gridTotalExportText = "(n/a)";
    private string _inverterTemperatureText = "(n/a)";
    private string _footerText = "Enter your FoxESS API key and inverter serial number, then load data.";
    private string _lastUpdatedText = "Not yet";
    private string _realtimeObservedText = "Realtime not loaded yet.";
    private bool _isAutoRefreshEnabled = true;
    private RefreshIntervalOption _selectedRefreshInterval;
    private ObservableCollection<PointCollection> _homeUsageTrendSegments = [];
    private ObservableCollection<PointCollection> _derivedPvTrendSegments = [];
    private ObservableCollection<PointCollection> _gridImportTrendSegments = [];
    private ObservableCollection<PointCollection> _gridExportTrendSegments = [];
    private double _chartCanvasWidth = DailyChartWidth;
    private double _chartCanvasHeight = MinimumChartHeight;
    private string _timeAxisStartLabel = "--:--";
    private string _timeAxisMiddleLabel = "--:--";
    private string _timeAxisEndLabel = "--:--";
    private IReadOnlyList<RealtimeTrendSample> _chartSamples = Array.Empty<RealtimeTrendSample>();
    private DateTime _chartRangeStart = DateTime.Today;
    private DateTime _chartRangeEnd = DateTime.Today.AddDays(1);

    public MainViewModel()
        : this(ConnectionSettingsStore.CreateDefault())
    {
    }

    public MainViewModel(ConnectionSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;

        RefreshIntervals =
        [
            new RefreshIntervalOption("30 seconds", TimeSpan.FromSeconds(30)),
            new RefreshIntervalOption("1 minute", TimeSpan.FromMinutes(1)),
            new RefreshIntervalOption("2 minutes", TimeSpan.FromMinutes(2)),
            new RefreshIntervalOption("5 minutes", TimeSpan.FromMinutes(5))
        ];

        _selectedRefreshInterval = RefreshIntervals[3];
        LoadConnectionSettings();
        LoadPersistedTrendHistory();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceSummary> Devices { get; } = [];
    public ObservableCollection<BatteryModule> Batteries { get; } = [];
    public ObservableCollection<MetricReading> RealtimeMetrics { get; } = [];
    public ObservableCollection<RealtimeTrendSample> TrendSamples { get; } = [];
    public ObservableCollection<ChartAxisTick> YAxisTicks { get; } = [];
    public IReadOnlyList<RefreshIntervalOption> RefreshIntervals { get; }
    public IReadOnlyList<LanguageOption> LanguageOptions => AppText.SupportedLanguages;

    public IReadOnlyDictionary<string, string> Texts
    {
        get => _texts;
        private set => SetProperty(ref _texts, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (SetProperty(ref _apiKey, value))
            {
                PersistConnectionSettings();
            }
        }
    }

    public string InverterSerialNumber
    {
        get => _inverterSerialNumber;
        set
        {
            if (SetProperty(ref _inverterSerialNumber, value))
            {
                PersistConnectionSettings();
            }
        }
    }

    public string LanguageCode
    {
        get => _languageCode;
        set
        {
            if (SetProperty(ref _languageCode, string.IsNullOrWhiteSpace(value) ? AppText.DefaultLanguageCode : value))
            {
                Texts = AppText.GetStrings(_languageCode);
                PersistConnectionSettings();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string DetailSummaryText
    {
        get => _detailSummaryText;
        private set => SetProperty(ref _detailSummaryText, value);
    }

    public string DetailFunctionsText
    {
        get => _detailFunctionsText;
        private set => SetProperty(ref _detailFunctionsText, value);
    }

    public string BatterySocText
    {
        get => _batterySocText;
        private set => SetProperty(ref _batterySocText, value);
    }

    public string BatteryTemperatureText
    {
        get => _batteryTemperatureText;
        private set => SetProperty(ref _batteryTemperatureText, value);
    }

    public string BatterySohText
    {
        get => _batterySohText;
        private set => SetProperty(ref _batterySohText, value);
    }

    public string BatteryDischargeText
    {
        get => _batteryDischargeText;
        private set => SetProperty(ref _batteryDischargeText, value);
    }

    public string HomeUsageText
    {
        get => _homeUsageText;
        private set => SetProperty(ref _homeUsageText, value);
    }

    public string PvGeneratedText
    {
        get => _pvGeneratedText;
        private set => SetProperty(ref _pvGeneratedText, value);
    }

    public string GridImportText
    {
        get => _gridImportText;
        private set => SetProperty(ref _gridImportText, value);
    }

    public string GridExportText
    {
        get => _gridExportText;
        private set => SetProperty(ref _gridExportText, value);
    }

    public string GridTotalExportText
    {
        get => _gridTotalExportText;
        private set => SetProperty(ref _gridTotalExportText, value);
    }

    //_inveterTemperatureText
    public string InverterTemperatureText
    {
        get => _inverterTemperatureText;
        private set => SetProperty(ref _inverterTemperatureText, value);
    }

    public string FooterText
    {
        get => _footerText;
        private set => SetProperty(ref _footerText, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetProperty(ref _lastUpdatedText, value);
    }

    public string RealtimeObservedText
    {
        get => _realtimeObservedText;
        private set => SetProperty(ref _realtimeObservedText, value);
    }

    public bool IsAutoRefreshEnabled
    {
        get => _isAutoRefreshEnabled;
        set => SetProperty(ref _isAutoRefreshEnabled, value);
    }

    public RefreshIntervalOption SelectedRefreshInterval
    {
        get => _selectedRefreshInterval;
        set => SetProperty(ref _selectedRefreshInterval, value);
    }

    public ObservableCollection<PointCollection> HomeUsageTrendSegments
    {
        get => _homeUsageTrendSegments;
        private set => SetProperty(ref _homeUsageTrendSegments, value);
    }

    public ObservableCollection<PointCollection> DerivedPvTrendSegments
    {
        get => _derivedPvTrendSegments;
        private set => SetProperty(ref _derivedPvTrendSegments, value);
    }

    public ObservableCollection<PointCollection> GridImportTrendSegments
    {
        get => _gridImportTrendSegments;
        private set => SetProperty(ref _gridImportTrendSegments, value);
    }

    public ObservableCollection<PointCollection> GridExportTrendSegments
    {
        get => _gridExportTrendSegments;
        private set => SetProperty(ref _gridExportTrendSegments, value);
    }

    public double ChartCanvasWidth
    {
        get => _chartCanvasWidth;
        private set => SetProperty(ref _chartCanvasWidth, value);
    }

    public double ChartCanvasHeight
    {
        get => _chartCanvasHeight;
        private set => SetProperty(ref _chartCanvasHeight, value);
    }

    public IReadOnlyList<RealtimeTrendSample> ChartSamples => _chartSamples;

    public DateTime ChartRangeStart => _chartRangeStart;

    public DateTime ChartRangeEnd => _chartRangeEnd;

    public double ChartHorizontalPadding => ChartHorizontalPaddingPixels;

    public TimeSpan RefreshInterval => SelectedRefreshInterval.Interval;

    public bool HasConnectionDetails =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(InverterSerialNumber);

    public string TimeAxisStartLabel
    {
        get => _timeAxisStartLabel;
        private set => SetProperty(ref _timeAxisStartLabel, value);
    }

    public string TimeAxisMiddleLabel
    {
        get => _timeAxisMiddleLabel;
        private set => SetProperty(ref _timeAxisMiddleLabel, value);
    }

    public string TimeAxisEndLabel
    {
        get => _timeAxisEndLabel;
        private set => SetProperty(ref _timeAxisEndLabel, value);
    }

    public async Task LoadDevicesAsync()
    {
        using var client = CreateClient();
        StatusMessage = "Loading devices...";
        FooterText = "Fetching the device list from FoxESS Cloud.";
        var devices = await client.ListDevicesAsync();

        Devices.Clear();
        foreach (var device in devices)
        {
            Devices.Add(device);
        }

        StatusMessage = $"Loaded {devices.Count} device(s)";
        FooterText = devices.Count > 0
            ? "Use the slim sidebar for device context; the main area is focused on live monitoring."
            : "No devices were returned for this API key.";
        StampUpdated();
    }

    public async Task LoadDetailAsync()
    {
        using var client = CreateClient();
        var serialNumber = RequireSerialNumber();
        StatusMessage = "Loading detail...";
        FooterText = $"Fetching detail for inverter {serialNumber}.";

        var detail = await client.GetDeviceDetailAsync(serialNumber);

        Batteries.Clear();
        foreach (var battery in detail.Batteries)
        {
            Batteries.Add(battery);
        }

        DetailSummaryText = $"SN {detail.DeviceSerialNumber} | Status {detail.Status} | Capacity {detail.CapacityKilowatts} kW";
        DetailFunctionsText = $"Scheduler supported: {detail.Functions.SchedulerSupported} | Battery modules: {detail.Batteries.Count}";
        StatusMessage = "Detail loaded";
        FooterText = $"Loaded {detail.Batteries.Count} battery record(s) for {serialNumber}.";
        StampUpdated();
    }

    public async Task LoadRealtimeAsync()
    {
        using var client = CreateClient();
        var serialNumber = RequireSerialNumber();
        StatusMessage = "Loading realtime...";
        FooterText = $"Fetching realtime values for inverter {serialNumber}.";

        var realtime = await client.GetRealtimeSnapshotAsync(serialNumber);

        RealtimeMetrics.Clear();
        foreach (var metric in realtime.Metrics.Values.OrderBy(metric => metric.Variable, StringComparer.OrdinalIgnoreCase))
        {
            RealtimeMetrics.Add(metric);
        }

        BatterySocText = FormatValue(realtime.Battery.StateOfCharge, "%");
        BatteryTemperatureText = FormatMetric(realtime.Metrics, "batTemperature");
        BatterySohText = FormatValue(realtime.Battery.StateOfHealth, "%");
        BatteryDischargeText = FormatMetric(realtime.Metrics, "batDischargePower");
        HomeUsageText = FormatMetric(realtime.Metrics, "loadsPower");
        PvGeneratedText = FormatDerivedPv(realtime);
        GridImportText = FormatMetric(realtime.Metrics, "gridConsumptionPower");
        GridExportText = FormatMetric(realtime.Metrics, "feedinPower");
        GridTotalExportText = FormatMetric(realtime.Metrics, "feedin2");
        InverterTemperatureText = FormatMetric(realtime.Metrics, "invTemperation");
        RealtimeObservedText = realtime.ObservedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "FoxESS did not supply a timestamp";

        AddTrendSample(realtime);

        StatusMessage = IsAutoRefreshEnabled
            ? $"Realtime loaded | Auto refresh every {SelectedRefreshInterval.Label.ToLowerInvariant()}"
            : "Realtime loaded";
        FooterText = $"Loaded {RealtimeMetrics.Count} realtime metric(s) for {serialNumber}.";
        StampUpdated();
    }

    private void AddTrendSample(RealtimeSnapshot realtime)
    {
        TrendSamples.Add(
            new RealtimeTrendSample(
                DateTime.Now,
                realtime.PowerFlow.LoadPower ?? GetMetricValue(realtime.Metrics, "loadsPower") ?? 0,
                CalculateDerivedPvValue(realtime),
                GetMetricValue(realtime.Metrics, "gridConsumptionPower") ?? 0,
                GetMetricValue(realtime.Metrics, "feedinPower") ?? 0));

        while (TrendSamples.Count > MaxTrendSamples)
        {
            TrendSamples.RemoveAt(0);
        }

        UpdateTrendPoints();
        SaveTrendHistory();
    }

    private void UpdateTrendPoints()
    {
        if (TrendSamples.Count == 0)
        {
            HomeUsageTrendSegments = [];
            DerivedPvTrendSegments = [];
            GridImportTrendSegments = [];
            GridExportTrendSegments = [];
            _chartSamples = Array.Empty<RealtimeTrendSample>();
            _chartRangeStart = DateTime.Today;
            _chartRangeEnd = DateTime.Today.AddDays(1);
            ChartCanvasWidth = DailyChartWidth;
            ChartCanvasHeight = MinimumChartHeight;
            RebuildAxisTicks(-0.5m, 1.5m);
            TimeAxisStartLabel = DateTime.Today.ToString("dd MMM 00:00");
            TimeAxisMiddleLabel = DateTime.Today.ToString("dd MMM 12:00");
            TimeAxisEndLabel = DateTime.Today.ToString("dd MMM") + " 24:00";
            return;
        }

        _chartSamples = TrendSamples
            .OrderBy(sample => sample.Timestamp)
            .ToArray();

        _chartRangeStart = _chartSamples.First().Timestamp.Date;
        _chartRangeEnd = _chartSamples.Last().Timestamp.Date.AddDays(1);

        var allValues = _chartSamples
            .SelectMany(sample => new[] { sample.HomeUsagePower, sample.DerivedPvOutputPower, sample.GridImportPower, sample.GridExportPower })
            .DefaultIfEmpty(0m)
            .ToArray();

        var minValue = allValues.Min();
        var maxValue = allValues.Max();
        var axisMin = Math.Min(-0.5m, Math.Floor(minValue / (decimal)ChartTickStepKilowatts) * (decimal)ChartTickStepKilowatts);
        var axisMax = Math.Max(1.5m, Math.Ceiling(maxValue / (decimal)ChartTickStepKilowatts) * (decimal)ChartTickStepKilowatts);

        if (axisMax <= axisMin)
        {
            axisMax = axisMin + 2.0m;
        }

        var totalDays = Math.Max(1, (_chartRangeEnd - _chartRangeStart).TotalDays);
        ChartCanvasWidth = Math.Max(DailyChartWidth, totalDays * DailyChartWidth);
        var tickCount = (int)Math.Round((double)((axisMax - axisMin) / (decimal)ChartTickStepKilowatts), MidpointRounding.AwayFromZero) + 1;
        ChartCanvasHeight = Math.Max(MinimumChartHeight, 40 + ((tickCount - 1) * HeightPerTick));

        HomeUsageTrendSegments = BuildSegments(_chartSamples, sample => sample.HomeUsagePower, axisMin, axisMax);
        DerivedPvTrendSegments = BuildSegments(_chartSamples, sample => sample.DerivedPvOutputPower, axisMin, axisMax);
        GridImportTrendSegments = BuildSegments(_chartSamples, sample => sample.GridImportPower, axisMin, axisMax);
        GridExportTrendSegments = BuildSegments(_chartSamples, sample => sample.GridExportPower, axisMin, axisMax);

        RebuildAxisTicks(axisMin, axisMax);

        var midpoint = _chartRangeStart + TimeSpan.FromTicks((_chartRangeEnd - _chartRangeStart).Ticks / 2);
        TimeAxisStartLabel = _chartRangeStart.ToString("dd MMM HH:mm");
        TimeAxisMiddleLabel = midpoint.ToString("dd MMM HH:mm");
        TimeAxisEndLabel = _chartRangeEnd.ToString("dd MMM HH:mm");
    }

    private ObservableCollection<PointCollection> BuildSegments(
        IReadOnlyList<RealtimeTrendSample> samples,
        Func<RealtimeTrendSample, decimal> valueSelector,
        decimal minValue,
        decimal maxValue)
    {
        var range = maxValue - minValue;
        if (range <= 0)
        {
            range = 1;
        }

        if (samples.Count == 0)
        {
            return [];
        }

        var segments = new ObservableCollection<PointCollection>();

        if (samples.Count == 1)
        {
            var value = valueSelector(samples[0]);
            var y = MapValueToY(value, minValue, range, ChartCanvasHeight);
            var x = MapTimestampToX(samples[0].Timestamp);
            segments.Add([new Point(x - 1, y), new Point(x + 1, y)]);
            return segments;
        }

        var currentSegment = new PointCollection();
        RealtimeTrendSample? previousSample = null;

        foreach (var sample in samples)
        {
            if (previousSample is not null && sample.Timestamp - previousSample.Timestamp > MaximumConnectedSampleGap + ConnectedSampleGapTolerance)
            {
                if (currentSegment.Count > 0)
                {
                    segments.Add(currentSegment);
                }

                currentSegment = new PointCollection();
            }

            var x = MapTimestampToX(sample.Timestamp);
            var y = MapValueToY(valueSelector(sample), minValue, range, ChartCanvasHeight);
            currentSegment.Add(new Point(x, y));
            previousSample = sample;
        }

        if (currentSegment.Count > 0)
        {
            if (currentSegment.Count == 1)
            {
                var point = currentSegment[0];
                currentSegment = [new Point(point.X - 1, point.Y), new Point(point.X + 1, point.Y)];
            }

            segments.Add(currentSegment);
        }

        return segments;
    }

    private double MapTimestampToX(DateTime timestamp)
    {
        var totalRangeSeconds = Math.Max(1, (_chartRangeEnd - _chartRangeStart).TotalSeconds);
        var clampedTimestamp = timestamp < _chartRangeStart
            ? _chartRangeStart
            : timestamp > _chartRangeEnd
                ? _chartRangeEnd
                : timestamp;

        var offsetSeconds = (clampedTimestamp - _chartRangeStart).TotalSeconds;
        var usableWidth = Math.Max(1, ChartCanvasWidth - (2 * ChartHorizontalPaddingPixels));
        return ChartHorizontalPaddingPixels + ((offsetSeconds / totalRangeSeconds) * usableWidth);
    }

    private static double MapValueToY(decimal value, decimal minValue, decimal range, double chartHeight)
    {
        var normalized = (double)((value - minValue) / range);
        var y = chartHeight - (normalized * chartHeight);
        return Math.Clamp(y, 0, chartHeight);
    }

    private FoxEssClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("FoxESS API key is required.");
        }

        return new FoxEssClient(
            new FoxEssOptions
            {
                ApiKey = ApiKey.Trim()
            },
            new SystemClock());
    }

    private string RequireSerialNumber()
    {
        if (string.IsNullOrWhiteSpace(InverterSerialNumber))
        {
            throw new InvalidOperationException("Inverter serial number is required.");
        }

        return InverterSerialNumber.Trim();
    }

    private static decimal? GetMetricValue(IReadOnlyDictionary<string, MetricReading> metrics, string variable)
    {
        return metrics.TryGetValue(variable, out var metric)
            ? metric.NumericValue
            : null;
    }

    private static string FormatMetric(IReadOnlyDictionary<string, MetricReading> metrics, string variable)
    {
        if (!metrics.TryGetValue(variable, out var metric))
        {
            return "(n/a)";
        }

        var suffix = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : $" {metric.Unit}";
        return $"{metric.ValueText}{suffix}";
    }

    private static string FormatDerivedPv(RealtimeSnapshot realtime)
    {
        return $"{CalculateDerivedPvValue(realtime):0.###} kW (derived)";
    }

    //TODO: logic error to fix
    private static decimal CalculateDerivedPvValue(RealtimeSnapshot realtime)
    {
        var rawPv = realtime.PowerFlow.PvPower ?? GetMetricValue(realtime.Metrics, "pvPower");
        if (rawPv.HasValue && rawPv.Value > 0)
        {
            return rawPv.Value;
        }

        var load = realtime.PowerFlow.LoadPower ?? GetMetricValue(realtime.Metrics, "loadsPower");
        var charge = realtime.Battery.ChargePower ?? GetMetricValue(realtime.Metrics, "batChargePower");
        var export = GetMetricValue(realtime.Metrics, "feedinPower");
        var discharge = realtime.Battery.DischargePower ?? GetMetricValue(realtime.Metrics, "batDischargePower");
        var import = GetMetricValue(realtime.Metrics, "gridConsumptionPower");

        if (!load.HasValue && !charge.HasValue && !export.HasValue && !discharge.HasValue && !import.HasValue)
        {
            return 0m;
        }

        var derivedPv =
            Math.Abs(load ?? 0m) +
            (charge ?? 0m) +
            (export ?? 0m) -
            (discharge ?? 0m) -
            (import ?? 0m);

        if (derivedPv < 0 && derivedPv > -0.02m)
        {
            derivedPv = 0;
        }

        return derivedPv;
    }

    private static string FormatValue(decimal? value, string unit)
    {
        return value.HasValue ? $"{value.Value:0.###} {unit}" : "(n/a)";
    }

    private void StampUpdated()
    {
        LastUpdatedText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void LoadPersistedTrendHistory()
    {
        try
        {
            if (!File.Exists(TrendHistoryPath))
            {
                UpdateTrendPoints();
                return;
            }

            var json = File.ReadAllText(TrendHistoryPath);
            var history = JsonSerializer.Deserialize<List<RealtimeTrendSample>>(json);
            if (history is null)
            {
                UpdateTrendPoints();
                return;
            }

            foreach (var sample in history
                         .OrderBy(sample => sample.Timestamp)
                         .TakeLast(MaxTrendSamples))
            {
                TrendSamples.Add(sample);
            }

            UpdateTrendPoints();
        }
        catch
        {
            UpdateTrendPoints();
        }
    }

    private void RebuildAxisTicks(decimal axisMin, decimal axisMax)
    {
        YAxisTicks.Clear();

        var range = axisMax - axisMin;
        if (range <= 0)
        {
            range = 1;
        }

        for (var tickValue = axisMax; tickValue >= axisMin; tickValue -= (decimal)ChartTickStepKilowatts)
        {
            var y = MapValueToY(tickValue, axisMin, range, ChartCanvasHeight);
            YAxisTicks.Add(new ChartAxisTick($"{tickValue:0.##} kW", y));
        }
    }

    private void SaveTrendHistory()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TrendHistoryPath)!);
            var json = JsonSerializer.Serialize(TrendSamples.ToList());
            File.WriteAllText(TrendHistoryPath, json);
        }
        catch
        {
            // Ignore persistence failures; live monitoring should continue working.
        }
    }

    private void LoadConnectionSettings()
    {
        var savedSettings = _settingsStore.Load();
        _languageCode = savedSettings.LanguageCode;
        _texts = AppText.GetStrings(_languageCode);

        _apiKey = !string.IsNullOrWhiteSpace(savedSettings.ApiKey)
            ? savedSettings.ApiKey
            : Environment.GetEnvironmentVariable("FOXESS_API_KEY") ?? string.Empty;

        _inverterSerialNumber = !string.IsNullOrWhiteSpace(savedSettings.InverterSerialNumber)
            ? savedSettings.InverterSerialNumber
            : Environment.GetEnvironmentVariable("FOXESS_INVERTER_SN") ?? string.Empty;
    }

    private void PersistConnectionSettings()
    {
        try
        {
            _settingsStore.Save(new AppConnectionSettings(ApiKey, InverterSerialNumber, LanguageCode));
        }
        catch
        {
            // Ignore settings persistence failures; the monitoring experience should keep working.
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed record RefreshIntervalOption(string Label, TimeSpan Interval);

public sealed record ChartAxisTick(string Label, double Y);

public sealed record RealtimeTrendSample(
    DateTime Timestamp,
    decimal HomeUsagePower,
    decimal DerivedPvOutputPower,
    decimal GridImportPower,
    decimal GridExportPower)
{
    public string TimestampText => Timestamp.ToString("dd MMM HH:mm");
}
