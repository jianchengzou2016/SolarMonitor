using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SolarMonitor.App.Configuration;

public sealed class ConnectionSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly string _secretPath;

    public ConnectionSettingsStore(string settingsPath, string secretPath)
    {
        _settingsPath = settingsPath;
        _secretPath = secretPath;
    }

    public static ConnectionSettingsStore CreateDefault()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SolarMonitor");

        return new ConnectionSettingsStore(
            Path.Combine(appDataDirectory, "appsettings.json"),
            Path.Combine(appDataDirectory, "secrets.dat"));
    }

    public AppConnectionSettings Load()
    {
        string inverterSerialNumber = string.Empty;
        string apiKey = string.Empty;

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var payload = JsonSerializer.Deserialize<AppSettingsPayload>(json, JsonOptions);
                inverterSerialNumber = payload?.InverterSerialNumber?.Trim() ?? string.Empty;
            }
        }
        catch
        {
            inverterSerialNumber = string.Empty;
        }

        try
        {
            if (File.Exists(_secretPath))
            {
                var protectedBytes = File.ReadAllBytes(_secretPath);
                var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
                apiKey = Encoding.UTF8.GetString(bytes).Trim();
            }
        }
        catch
        {
            apiKey = string.Empty;
        }

        return new AppConnectionSettings(apiKey, inverterSerialNumber);
    }

    public void Save(AppConnectionSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

        var payload = new AppSettingsPayload
        {
            InverterSerialNumber = settings.InverterSerialNumber.Trim()
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(_settingsPath, json);

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            if (File.Exists(_secretPath))
            {
                File.Delete(_secretPath);
            }

            return;
        }

        var bytes = Encoding.UTF8.GetBytes(settings.ApiKey.Trim());
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_secretPath, protectedBytes);
    }

    private sealed class AppSettingsPayload
    {
        public string? InverterSerialNumber { get; init; }
    }
}
