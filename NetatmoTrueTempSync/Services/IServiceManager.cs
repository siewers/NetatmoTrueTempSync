namespace NetatmoTrueTempSync.Services;

internal interface IServiceManager
{
    string ServiceFilePath { get; }

    bool IsInstalled { get; }

    bool IsRunning { get; }

    void Install(string executablePath);

    void Uninstall();
}
