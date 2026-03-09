using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using NetatmoTrueTempSync.Models;

namespace NetatmoTrueTempSync.Auth;

public static class TokenStore
{
    private const string CredentialsSecretKey = "credentials";
    private const string WebSessionSecretKey = "websession";
    private static readonly ISecretStore Secrets = CreateSecretStore();

    public static NetatmoCredentials LoadCredentials()
    {
        return TryLoadCredentials(out var credentials)
            ? credentials
            : throw new MissingCredentialsException();
    }

    public static bool TryLoadCredentials([NotNullWhen(true)] out NetatmoCredentials? credentials)
    {
        var entry = Secrets.Load(CredentialsSecretKey);
        if (entry is { Account.Length: > 0, Secret.Length: > 0 })
        {
            credentials = new NetatmoCredentials(entry.Account, entry.Secret);
            return true;
        }

        credentials = null;
        return false;
    }

    public static void SaveCredentials(string email, string password)
    {
        Secrets.Save(CredentialsSecretKey, new SecretEntry(email, password));
    }

    public static WebSessionData? LoadWebSession()
    {
        var entry = Secrets.Load(WebSessionSecretKey);
        if (entry is null)
        {
            return null;
        }

        var session = JsonSerializer.Deserialize(entry.Secret, AppJsonContext.Default.WebSessionData);
        return session is { AccessToken.Length: > 0 } ? session : null;
    }

    public static void SaveWebSession(WebSessionData session)
    {
        if (!TryLoadCredentials(out var credentials))
        {
            return;
        }

        var json = JsonSerializer.Serialize(session, AppJsonContext.Default.WebSessionData);
        Secrets.Save(WebSessionSecretKey, new SecretEntry(credentials.Email, json));
    }

    public static bool DeleteCredentials()
    {
        if (Secrets.Load(CredentialsSecretKey) is null)
        {
            return false;
        }

        Secrets.Delete(CredentialsSecretKey);
        return true;
    }

    public static bool DeleteWebSession()
    {
        if (Secrets.Load(WebSessionSecretKey) is null)
        {
            return false;
        }

        Secrets.Delete(WebSessionSecretKey);
        return true;
    }

    private static ISecretStore CreateSecretStore()
    {
        if (OperatingSystem.IsMacOS())
        {
            return new KeychainSecretStore();
        }

        if (OperatingSystem.IsLinux() && SecretToolSecretStore.IsAvailable())
        {
            return new SecretToolSecretStore();
        }

        return new FileSecretStore();
    }
}
