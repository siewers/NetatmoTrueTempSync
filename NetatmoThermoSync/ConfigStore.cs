using System.Text.Json;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync;

public static class ConfigStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "netatmo-thermosync");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static async Task<AppConfig?> Load(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
    }

    public static async Task Save(AppConfig config, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig);
        await File.WriteAllTextAsync(ConfigPath, json, cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
