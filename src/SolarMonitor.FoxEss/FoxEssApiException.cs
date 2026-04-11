namespace SolarMonitor.FoxEss;

public sealed class FoxEssApiException : Exception
{
    public FoxEssApiException(string message)
        : base(message)
    {
    }
}
