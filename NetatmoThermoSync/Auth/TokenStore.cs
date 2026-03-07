using System.Text.Json;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync.Auth;

public static class TokenStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "netatmo-thermosync");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    private static void EnsureConfigDir()
    {
        Directory.CreateDirectory(ConfigDir);
    }

    public static async Task<AppConfig?> LoadConfig(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
    }

    public static async Task SaveConfig(AppConfig config, CancellationToken cancellationToken)
    {
        EnsureConfigDir();
        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig);
        await File.WriteAllTextAsync(ConfigPath, json, cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static async Task<WebSessionData?> LoadWebSession(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ConfigDir, "websession.json");
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.WebSessionData);
    }

    public static async Task SaveWebSession(WebSessionData session, CancellationToken cancellationToken)
    {
        EnsureConfigDir();
        var path = Path.Combine(ConfigDir, "websession.json");
        var json = JsonSerializer.Serialize(session, AppJsonContext.Default.WebSessionData);
        await File.WriteAllTextAsync(path, json, cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
