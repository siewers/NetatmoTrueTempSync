using System.Diagnostics;

namespace NetatmoTrueTempSync.Services;

internal sealed class SystemdServiceManager : IServiceManager
{
    private const string UnitName = "com.siewers.NetatmoTrueTempSync";

    private static readonly string UnitDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "systemd", "user");

    private static readonly string ServicePath = Path.Combine(UnitDir, $"{UnitName}.service");
    private static readonly string TimerPath = Path.Combine(UnitDir, $"{UnitName}.timer");

    public string ServiceFilePath => TimerPath;

    public bool IsInstalled => File.Exists(TimerPath);

    public bool IsRunning
    {
        get
        {
            var (exitCode, output) = RunSystemctl("is-active", $"{UnitName}.timer");
            return exitCode == 0 && output.Trim() == "active";
        }
    }

    public void Install(string executablePath)
    {
        Directory.CreateDirectory(UnitDir);

        File.WriteAllText(ServicePath, GenerateServiceUnit(executablePath));
        File.WriteAllText(TimerPath, GenerateTimerUnit());

        RunSystemctl("daemon-reload");
        RunSystemctl("enable", "--now", $"{UnitName}.timer");
    }

    public void Uninstall()
    {
        if (IsInstalled)
        {
            RunSystemctl("disable", "--now", $"{UnitName}.timer");
            RunSystemctl("stop", $"{UnitName}.service");
            RunSystemctl("daemon-reload");

            if (File.Exists(ServicePath))
            {
                File.Delete(ServicePath);
            }

            if (File.Exists(TimerPath))
            {
                File.Delete(TimerPath);
            }
        }
    }

    private static string GenerateServiceUnit(string executablePath)
    {
        return $"""
                [Unit]
                Description=NetatmoTrueTempSync — sync temperatures

                [Service]
                Type=oneshot
                ExecStart={executablePath} sync
                """;
    }

    private static string GenerateTimerUnit()
    {
        return $"""
                [Unit]
                Description=NetatmoTrueTempSync — sync timer

                [Timer]
                OnBootSec=1min
                OnUnitActiveSec=10min
                Unit={UnitName}.service

                [Install]
                WantedBy=timers.target
                """;
    }

    private static (int ExitCode, string Output) RunSystemctl(params string[] args)
    {
        var psi = new ProcessStartInfo("systemctl")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        psi.ArgumentList.Add("--user");
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
