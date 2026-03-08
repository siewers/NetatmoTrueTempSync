namespace NetatmoThermoSync.Auth;

public interface ISecretStore
{
    (string Account, string Secret)? Load(string key);

    void Save(string key, string account, string secret);

    void Delete(string key);
}
