using SolarMonitor.App.Configuration;

namespace SolarMonitor.App.Tests;

public sealed class ConnectionSettingsStoreTests : IDisposable
{
    private readonly string _testDirectory;

    public ConnectionSettingsStoreTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SolarMonitor.App.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsSerialNumberAndApiKey()
    {
        var store = CreateStore();

        store.Save(new AppConnectionSettings("test-api-key", " inverter-001 "));

        var loaded = store.Load();

        Assert.Equal("test-api-key", loaded.ApiKey);
        Assert.Equal("inverter-001", loaded.InverterSerialNumber);
    }

    [Fact]
    public void Save_WithBlankApiKey_RemovesSecretFile()
    {
        var settingsPath = Path.Combine(_testDirectory, "appsettings.json");
        var secretPath = Path.Combine(_testDirectory, "secrets.dat");
        var store = new ConnectionSettingsStore(settingsPath, secretPath);

        store.Save(new AppConnectionSettings("test-api-key", "inverter-001"));
        store.Save(new AppConnectionSettings(string.Empty, "inverter-001"));

        Assert.False(File.Exists(secretPath));
        Assert.Equal("inverter-001", store.Load().InverterSerialNumber);
    }

    [Fact]
    public void Load_WhenFilesDoNotExist_ReturnsEmptySettings()
    {
        var loaded = CreateStore().Load();

        Assert.Equal(string.Empty, loaded.ApiKey);
        Assert.Equal(string.Empty, loaded.InverterSerialNumber);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private ConnectionSettingsStore CreateStore()
    {
        return new ConnectionSettingsStore(
            Path.Combine(_testDirectory, "appsettings.json"),
            Path.Combine(_testDirectory, "secrets.dat"));
    }
}
