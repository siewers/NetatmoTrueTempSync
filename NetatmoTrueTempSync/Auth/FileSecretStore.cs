using System.Text.Json;
using NetatmoTrueTempSync.Models;

namespace NetatmoTrueTempSync.Auth;

internal sealed class FileSecretStore : ISecretStore
{
    private static readonly string SecretsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "netatmo-truetempsync", "secrets");

    public (string Account, string Secret)? Load(string key)
    {
        var path = Path.Combine(SecretsDir, key);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        var dict = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString);
        if (dict is null || !dict.TryGetValue("account", out var account) || !dict.TryGetValue("secret", out var secret))
        {
            return null;
        }

        return (account, secret);
    }

    public void Save(string key, string account, string secret)
    {
        Directory.CreateDirectory(SecretsDir);
        var dict = new Dictionary<string, string> { ["account"] = account, ["secret"] = secret };
        var json = JsonSerializer.Serialize(dict, AppJsonContext.Default.DictionaryStringString);
        var path = Path.Combine(SecretsDir, key);
        File.WriteAllText(path, json);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public void Delete(string key)
    {
        var path = Path.Combine(SecretsDir, key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
