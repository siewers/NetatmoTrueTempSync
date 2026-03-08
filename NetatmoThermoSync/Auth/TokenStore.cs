using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync.Auth;

public static class TokenStore
{
    private const string CredentialsSecretKey = "credentials";
    private const string WebSessionSecretKey = "websession";
    private static readonly ISecretStore Secrets = CreateSecretStore();

    public static bool TryLoadCredentials([NotNullWhen(true)] out NetatmoCredentials? credentials)
    {
        if (Secrets.Load(CredentialsSecretKey) is ({ Length: > 0 } email, { Length: > 0 } password))
        {
            credentials = new NetatmoCredentials(email, password);
            return true;
        }

        credentials = null;
        return false;
    }

    public static void SaveCredentials(string email, string password)
    {
        Secrets.Save(CredentialsSecretKey, email, password);
    }

    public static WebSessionData? LoadWebSession()
    {
        var result = Secrets.Load(WebSessionSecretKey);
        if (result is null)
        {
            return null;
        }

        var session = JsonSerializer.Deserialize(result.Value.Secret, AppJsonContext.Default.WebSessionData);
        return session is { AccessToken.Length: > 0 } ? session : null;
    }

    public static void SaveWebSession(WebSessionData session)
    {
        if (!TryLoadCredentials(out var credentials))
        {
            return;
        }

        var json = JsonSerializer.Serialize(session, AppJsonContext.Default.WebSessionData);
        Secrets.Save(WebSessionSecretKey, credentials.Email, json);
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
