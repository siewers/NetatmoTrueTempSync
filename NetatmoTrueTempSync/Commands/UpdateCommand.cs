using System.CommandLine;
using NetatmoTrueTempSync.Services;
using Spectre.Console;

namespace NetatmoTrueTempSync.Commands;

public static class UpdateCommand
{
    internal static readonly Option<bool> CheckOption = new("--check") { Description = "Only check for updates without installing" };

    internal static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var checkOnly = parseResult.GetValue(CheckOption);

        using var client = new HttpClient();

        var result = await AnsiConsole.Status()
            .StartAsync("Checking for updates...", async _ =>
                await UpdateService.CheckForUpdateAsync(client, cancellationToken));

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not check for updates.");
            return 1;
        }

        var (tagName, assetUrl) = result.Value;

        if (assetUrl is null)
        {
            var assetName = UpdateService.GetExpectedAssetName();
            AnsiConsole.MarkupLine($"[yellow]No binary found for [bold]{Markup.Escape(assetName)}[/] in release {Markup.Escape(tagName ?? "unknown")}.[/]");
            return 1;
        }

        var currentVersion = UpdateService.GetCurrentVersion();
        AnsiConsole.MarkupLine($"  Current version: [bold]{Markup.Escape(currentVersion ?? "unknown")}[/]");
        AnsiConsole.MarkupLine($"  Latest release:  [bold]{Markup.Escape(tagName ?? "unknown")}[/]");

        if (checkOnly)
        {
            return 0;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not determine the executable path.");
            return 1;
        }

        // Stop service before replacing binary
        IServiceManager? manager = null;
        var serviceWasRunning = false;
        try
        {
            manager = ServiceManagerFactory.Create();
            if (manager.IsInstalled)
            {
                serviceWasRunning = true;
                AnsiConsole.MarkupLine("Stopping service...");
                manager.Uninstall();
            }
        }
        catch (PlatformNotSupportedException)
        {
            // Not on a supported service platform, skip
        }

        await AnsiConsole.Status()
            .StartAsync("Downloading update...", async _ =>
                await UpdateService.DownloadAndReplaceAsync(client, assetUrl, executablePath, cancellationToken));

        AnsiConsole.MarkupLine($"[green]Updated to {Markup.Escape(tagName ?? "latest")}.[/]");

        // Reinstall service if it was running before
        if (serviceWasRunning && manager is not null)
        {
            AnsiConsole.MarkupLine("Reinstalling service...");
            manager.Install(executablePath);
            AnsiConsole.MarkupLine("[green]Service restarted.[/]");
        }

        return 0;
    }
}
