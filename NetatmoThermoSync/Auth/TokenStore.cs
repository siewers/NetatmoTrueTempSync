using System.Text.Json;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync.Auth;

public static class TokenStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "netatmo-thermosync");

    private static string TokenPath => Path.Combine(ConfigDir, "tokens.json");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    private static void EnsureConfigDir()
    {
        Directory.CreateDirectory(ConfigDir);
    }

    public static TokenData? LoadTokens()
    {
        if (!File.Exists(TokenPath))
        {
            return null;
        }

        var json = File.ReadAllText(TokenPath);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.TokenData);
    }

    public static void SaveTokens(TokenData tokens)
    {
        EnsureConfigDir();
        var json = JsonSerializer.Serialize(tokens, AppJsonContext.Default.TokenData);
        File.WriteAllText(TokenPath, json);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(TokenPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static AppConfig? LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
    }

    public static void SaveConfig(AppConfig config)
    {
        EnsureConfigDir();
        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig);
        File.WriteAllText(ConfigPath, json);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static WebSessionData? LoadWebSession()
    {
        var path = Path.Combine(ConfigDir, "websession.json");
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.WebSessionData);
    }

    public static void SaveWebSession(WebSessionData session)
    {
        EnsureConfigDir();
        var path = Path.Combine(ConfigDir, "websession.json");
        var json = JsonSerializer.Serialize(session, AppJsonContext.Default.WebSessionData);
        File.WriteAllText(path, json);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static bool IsTokenExpired(TokenData tokens)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= tokens.ExpiresAt - 60;
    }
}
