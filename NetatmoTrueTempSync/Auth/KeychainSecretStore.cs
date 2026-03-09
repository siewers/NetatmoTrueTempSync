using System.Diagnostics;
using System.Text;

namespace NetatmoTrueTempSync.Auth;

internal sealed class KeychainSecretStore : ISecretStore
{
    private const string ServicePrefix = "com.siewers.NetatmoTrueTempSync/";

    public SecretEntry? Load(string key)
    {
        var service = ServicePrefix + key;

        // -g prints password to stderr, attributes to stdout
        var (exitCode, stdout, stderr) = RunSecurity("find-generic-password", "-g", "-s", service);
        if (exitCode != 0)
        {
            return null;
        }

        var account = ParseAccount(stdout);
        var password = ParsePassword(stderr);
        if (account is null || password is null)
        {
            return null;
        }

        return new SecretEntry(account, password);
    }

    public void Save(string key, SecretEntry entry)
    {
        var service = ServicePrefix + key;
        // Delete existing entry first to handle account name changes
        RunSecurity("delete-generic-password", "-s", service);
        RunSecurity("add-generic-password", "-s", service, "-a", entry.Account, "-w", entry.Secret);
    }

    public void Delete(string key)
    {
        RunSecurity("delete-generic-password", "-s", ServicePrefix + key);
    }

    private static string? ParseAccount(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.AsSpan().Trim();
            if (!trimmed.StartsWith("\"acct\""))
            {
                continue;
            }

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0)
            {
                continue;
            }

            var value = trimmed[(eqIndex + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                return value[1..^1].ToString();
            }
        }

        return null;
    }

    private static string? ParsePassword(string stderr)
    {
        foreach (var line in stderr.Split('\n'))
        {
            var trimmed = line.AsSpan().Trim();
            if (!trimmed.StartsWith("password:"))
            {
                continue;
            }

            var value = trimmed["password:".Length..].Trim();

            // Plain text: password: "thepassword"
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                return value[1..^1].ToString();
            }

            // Hex-encoded (non-ASCII): password: 0x<hex>  "escaped representation"
            if (value.StartsWith("0x"))
            {
                var hex = value[2..];
                var spaceIndex = hex.IndexOf(' ');
                if (spaceIndex >= 0)
                {
                    hex = hex[..spaceIndex];
                }

                return Encoding.UTF8.GetString(Convert.FromHexString(hex.ToString()));
            }
        }

        return null;
    }

    private static (int ExitCode, string Stdout, string Stderr) RunSecurity(params string[] args)
    {
        var psi = new ProcessStartInfo("security")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout, stderr);
    }
}
