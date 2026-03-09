using System.CommandLine;
using NetatmoTrueTempSync.Services;
using Spectre.Console;

namespace NetatmoTrueTempSync.Commands;

public static class ServiceCommand
{
    internal static Task<int> InstallAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not determine the executable path.");
            return Task.FromResult(1);
        }

        var fileName = Path.GetFileName(executablePath);
        if (fileName is "dotnet" or "dotnet.exe")
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Cannot install as a service when running via [bold]dotnet run[/].");
            AnsiConsole.MarkupLine("Publish the app first with [bold]dotnet publish[/], then run the published binary.");
            return Task.FromResult(1);
        }

        try
        {
            var manager = ServiceManagerFactory.Create();

            if (manager.IsInstalled)
            {
                AnsiConsole.MarkupLine("[yellow]Service is already installed.[/] Reinstalling...");
                manager.Uninstall();
            }

            manager.Install(executablePath);
            AnsiConsole.MarkupLine("[green]Service installed and started.[/]");
            AnsiConsole.MarkupLine($"  Service file: [dim]{Markup.Escape(manager.ServiceFilePath)}[/]");
            AnsiConsole.MarkupLine("  Sync will run every [bold]10 minutes[/].");
        }
        catch (PlatformNotSupportedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    internal static Task<int> UninstallAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            var manager = ServiceManagerFactory.Create();

            if (!manager.IsInstalled)
            {
                AnsiConsole.MarkupLine("[yellow]Service is not installed.[/]");
                return Task.FromResult(0);
            }

            manager.Uninstall();
            AnsiConsole.MarkupLine("[green]Service stopped and removed.[/]");
        }
        catch (PlatformNotSupportedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    internal static Task<int> StatusAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            var manager = ServiceManagerFactory.Create();
            var platform = OperatingSystem.IsMacOS() ? "macOS (launchd)" : "Linux (systemd)";

            AnsiConsole.MarkupLine($"  Platform:  [bold]{platform}[/]");
            AnsiConsole.MarkupLine($"  Service:   [dim]{Markup.Escape(manager.ServiceFilePath)}[/]");
            AnsiConsole.MarkupLine($"  Installed: {(manager.IsInstalled ? "[green]yes[/]" : "[red]no[/]")}");
            AnsiConsole.MarkupLine($"  Running:   {(manager.IsRunning ? "[green]yes[/]" : "[red]no[/]")}");
        }
        catch (PlatformNotSupportedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
