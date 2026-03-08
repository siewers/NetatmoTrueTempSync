using System.Diagnostics;
using System.Text;

namespace NetatmoThermoSync.Auth;

internal sealed class KeychainSecretStore : ISecretStore
{
    private const string ServicePrefix = "netatmo-thermosync/";

    public (string Account, string Secret)? Load(string key)
    {
        var service = ServicePrefix + key;

        // Get attributes output to parse account name
        var (exitCode, output) = RunSecurity("find-generic-password", "-s", service);
        if (exitCode != 0)
        {
            return null;
        }

        var account = ParseAccount(output);
        if (account is null)
        {
            return null;
        }

        // Get password separately
        var (pwExitCode, password) = RunSecurity("find-generic-password", "-s", service, "-w");
        if (pwExitCode != 0)
        {
            return null;
        }

        return (account, DecodePassword(password.TrimEnd('\n')));
    }

    public void Save(string key, string account, string secret)
    {
        var service = ServicePrefix + key;
        // Delete existing entry first to handle account name changes
        RunSecurity("delete-generic-password", "-s", service);
        RunSecurity("add-generic-password", "-s", service, "-a", account, "-w", secret);
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

    private static string DecodePassword(string value)
    {
        // macOS Keychain hex-encodes the password when it contains non-ASCII characters.
        // Detect hex output (even length, all hex chars) and decode it.
        if (value.Length % 2 == 0 && value.Length > 0 && IsHex(value))
        {
            var bytes = Convert.FromHexString(value);
            return Encoding.UTF8.GetString(bytes);
        }

        return value;
    }

    private static bool IsHex(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static (int ExitCode, string Output) RunSecurity(params string[] args)
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
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
