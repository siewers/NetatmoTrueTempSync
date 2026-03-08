using System.Diagnostics;
using System.Text.Json;
using NetatmoTrueTempSync.Models;

namespace NetatmoTrueTempSync.Auth;

internal sealed class SecretToolSecretStore : ISecretStore
{
    private const string ServiceName = "netatmo-truetempsync";

    public (string Account, string Secret)? Load(string key)
    {
        var psi = CreateStartInfo("lookup", "service", ServiceName, "key", key);
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return null;
        }

        var dict = JsonSerializer.Deserialize(output, AppJsonContext.Default.DictionaryStringString);
        if (dict is null || !dict.TryGetValue("account", out var account) || !dict.TryGetValue("secret", out var secret))
        {
            return null;
        }

        return (account, secret);
    }

    public void Save(string key, string account, string secret)
    {
        var dict = new Dictionary<string, string> { ["account"] = account, ["secret"] = secret };
        var json = JsonSerializer.Serialize(dict, AppJsonContext.Default.DictionaryStringString);

        var psi = CreateStartInfo("store", "--label", $"NetatmoTrueTempSync: {key}", "service", ServiceName, "key", key);
        psi.RedirectStandardInput = true;
        using var process = Process.Start(psi)!;
        process.StandardInput.Write(json);
        process.StandardInput.Close();
        process.WaitForExit();
    }

    public void Delete(string key)
    {
        var psi = CreateStartInfo("clear", "service", ServiceName, "key", key);
        using var process = Process.Start(psi)!;
        process.WaitForExit();
    }

    internal static bool IsAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("secret-tool")
            {
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi)!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static ProcessStartInfo CreateStartInfo(params string[] args)
    {
        var psi = new ProcessStartInfo("secret-tool")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return psi;
    }
}
