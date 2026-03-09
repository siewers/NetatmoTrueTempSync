namespace NetatmoTrueTempSync.Services;

internal static class ServiceManagerFactory
{
    public static IServiceManager Create()
    {
        if (OperatingSystem.IsMacOS())
        {
            return new LaunchdServiceManager();
        }

        if (OperatingSystem.IsLinux())
        {
            return new SystemdServiceManager();
        }

        throw new PlatformNotSupportedException(
            "Background service management is only supported on macOS (launchd) and Linux (systemd).");
    }
}
