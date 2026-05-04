namespace SolarMonitor.App.Localization;

public static class AppText
{
    public const string DefaultLanguageCode = "en-AU";

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("en-AU", "English"),
        new("zh-CN", "简体中文")
    ];

    public static IReadOnlyDictionary<string, string> GetStrings(string languageCode)
    {
        return languageCode == "zh-CN" ? Chinese : English;
    }

    private static readonly Dictionary<string, string> English = new()
    {
        ["AppDescription"] = "Read-only FoxESS explorer for your inverter and battery stack.",
        ["ConnectionStatus"] = "Connection status",
        ["LastRefresh"] = "Last refresh",
        ["ApiKey"] = "FoxESS API key",
        ["InverterSerialNumber"] = "Inverter serial number",
        ["Language"] = "Language",
        ["LoadDevices"] = "Load Devices",
        ["LoadDetail"] = "Load Detail",
        ["LoadRealtime"] = "Load Realtime",
        ["Devices"] = "Devices",
        ["DeviceDetail"] = "Device Detail",
        ["RealtimeDashboard"] = "Realtime Dashboard",
        ["AutoRefresh"] = "Auto refresh",
        ["BatterySoc"] = "Battery SoC",
        ["BatteryTemp"] = "Battery Temp",
        ["BatterySoh"] = "Battery SOH",
        ["BatteryDischarge"] = "Battery Discharge",
        ["HomeUsage"] = "Home Usage",
        ["DerivedPvOutput"] = "Derived PV output",
        ["GridImport"] = "Grid Import",
        ["GridExport"] = "Grid Export",
        ["TotalGridExport"] = "Total Grid Export",
        ["InverterTemperature"] = "Inverter Temperature",
        ["RecentTrend"] = "Recent Trend (Home usage, Derived PV output, Grid import, Grid export)",
        ["RecentRefreshes"] = "Recent Refreshes",
        ["AllRealtimeMetrics"] = "All Realtime Metrics"
    };

    private static readonly Dictionary<string, string> Chinese = new()
    {
        ["AppDescription"] = "FoxESS 逆变器与电池组只读监控工具。",
        ["ConnectionStatus"] = "连接状态",
        ["LastRefresh"] = "上次刷新",
        ["ApiKey"] = "FoxESS API 密钥",
        ["InverterSerialNumber"] = "逆变器序列号",
        ["Language"] = "界面语言",
        ["LoadDevices"] = "加载设备",
        ["LoadDetail"] = "加载详情",
        ["LoadRealtime"] = "加载实时数据",
        ["Devices"] = "设备",
        ["DeviceDetail"] = "设备详情",
        ["RealtimeDashboard"] = "实时仪表盘",
        ["AutoRefresh"] = "自动刷新",
        ["BatterySoc"] = "电池电量",
        ["BatteryTemp"] = "电池温度",
        ["BatterySoh"] = "电池健康度",
        ["BatteryDischarge"] = "电池放电",
        ["HomeUsage"] = "家庭用电",
        ["DerivedPvOutput"] = "推算光伏输出",
        ["GridImport"] = "电网输入",
        ["GridExport"] = "电网输出",
        ["TotalGridExport"] = "累计电网输出",
        ["InverterTemperature"] = "逆变器温度",
        ["RecentTrend"] = "近期趋势（家庭用电、推算光伏输出、电网输入、电网输出）",
        ["RecentRefreshes"] = "近期刷新",
        ["AllRealtimeMetrics"] = "全部实时指标"
    };
}
