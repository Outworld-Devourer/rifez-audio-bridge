using System.Text.Json;

namespace RifeZPhoneBridge.App;

public sealed class AppSettingsService
{
    private readonly string _rootPath;
    private readonly string _settingsPath;

    public AppSettingsService()
    {
        _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RifeZPhoneBridge");
        _settingsPath = Path.Combine(_rootPath, "settings.json");
    }

    public string RootPath => _rootPath;
    public string SettingsPath => _settingsPath;
    public string LogsPath => Path.Combine(_rootPath, "Logs");

    public BridgeAppSettings Load()
    {
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(LogsPath);

        if (!File.Exists(_settingsPath))
        {
            var defaults = new BridgeAppSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<BridgeAppSettings>(json, CreateOptions()) ?? new BridgeAppSettings();
        }
        catch
        {
            return new BridgeAppSettings();
        }
    }

    public void Save(BridgeAppSettings settings)
    {
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(LogsPath);
        string json = JsonSerializer.Serialize(settings, CreateOptions());
        File.WriteAllText(_settingsPath, json);
    }

    private static JsonSerializerOptions CreateOptions() => new()
    {
        WriteIndented = true
    };
}
