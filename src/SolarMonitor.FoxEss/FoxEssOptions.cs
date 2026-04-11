namespace SolarMonitor.FoxEss;

public sealed class FoxEssOptions
{
    public const string DefaultBaseUrl = "https://www.foxesscloud.com";
    public const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";

    public required string ApiKey { get; init; }
    public string BaseUrl { get; init; } = DefaultBaseUrl;
    public string UserAgent { get; init; } = DefaultUserAgent;
    public string Language { get; init; } = "en";
    public TimeSpan MinimumInterval { get; init; } = TimeSpan.FromMilliseconds(1100);
}
