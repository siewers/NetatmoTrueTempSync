using System.Diagnostics;

namespace NetatmoTrueTempSync.Services;

internal sealed class LaunchdServiceManager : IServiceManager
{
    private const string Label = "com.siewers.NetatmoTrueTempSync";

    private static readonly string PlistPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{Label}.plist");

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Logs", "NetatmoTrueTempSync.log");

    public string ServiceFilePath => PlistPath;

    public bool IsInstalled => File.Exists(PlistPath);

    public bool IsRunning => RunLaunchctl("list", Label) == 0;

    public void Install(string executablePath)
    {
        var plist = GeneratePlist(executablePath);

        var dir = Path.GetDirectoryName(PlistPath)!;
        Directory.CreateDirectory(dir);

        File.WriteAllText(PlistPath, plist);
        RunLaunchctl("load", PlistPath);
    }

    public void Uninstall()
    {
        if (IsInstalled)
        {
            RunLaunchctl("unload", PlistPath);
            File.Delete(PlistPath);
        }
    }

    private static string GeneratePlist(string executablePath)
    {
        // language=xml
        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>{Label}</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>{executablePath}</string>
                        <string>sync</string>
                    </array>
                    <key>StartInterval</key>
                    <integer>600</integer>
                    <key>RunAtLoad</key>
                    <true/>
                    <key>StandardOutPath</key>
                    <string>{LogPath}</string>
                    <key>StandardErrorPath</key>
                    <string>{LogPath}</string>
                </dict>
                </plist>
                """;
    }

    private static int RunLaunchctl(params string[] args)
    {
        var psi = new ProcessStartInfo("launchctl")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)!;
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }
}
