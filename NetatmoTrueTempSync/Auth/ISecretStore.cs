namespace NetatmoTrueTempSync.Auth;

public interface ISecretStore
{
    SecretEntry? Load(string key);

    void Save(string key, SecretEntry entry);

    void Delete(string key);
}
