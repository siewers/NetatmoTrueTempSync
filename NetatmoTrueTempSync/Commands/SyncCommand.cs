using System.CommandLine;
using System.Globalization;
using NetatmoTrueTempSync.Api;
using NetatmoTrueTempSync.Auth;
using NetatmoTrueTempSync.Models;
using NetatmoTrueTempSync.Services;
using Spectre.Console;

namespace NetatmoTrueTempSync.Commands;

public static class SyncCommand
{
    internal static readonly Option<bool> DryRunOption = new("--dry-run") { Description = "Show what would be synced without making changes" };
    internal static readonly Option<string?> HomeOption = new("--home") { Description = "Home name or ID to sync (defaults to first home)" };

    internal static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var dryRun = parseResult.GetValue(DryRunOption);
        var homeName = parseResult.GetValue(HomeOption);
        return await RunAsync(dryRun, homeName, cancellationToken);
    }

    private static async Task<int> RunAsync(bool dryRun, string? homeName, CancellationToken cancellationToken)
    {
        var config = await ConfigStore.LoadAsync(cancellationToken);
        using var client = new NetatmoClient(TokenStore.LoadCredentials());

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run mode — no changes will be made.[/]");
        }

        try
        {
            await RunSyncCycleAsync(client, dryRun, homeName, config, cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Sync error: {ex}");
            return 1;
        }

        return 0;
    }

    private static async Task RunSyncCycleAsync(NetatmoClient client, bool dryRun, string? homeName, AppConfig config, CancellationToken cancellationToken)
    {
        var homesData = await client.GetHomesDataAsync(cancellationToken);
        var homes = homesData.Body?.Homes ?? [];

        var home = SyncService.FindHome(homes, homeName);

        var status = await client.GetHomeStatusAsync(home.Id, cancellationToken);
        var roomStatuses = status.Body?.Home?.Rooms ?? [];

        // Get weather station indoor module readings
        var indoorReadings = await GetIndoorReadingsAsync(client, cancellationToken) ?? [];

        var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape($"[{timestamp}]")}[/] Syncing [bold]{Markup.Escape(home.Name)}[/]...");

        if (indoorReadings.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No weather station indoor modules found.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"  Indoor sensors: {string.Join(", ", indoorReadings.Select(r => $"{Markup.Escape(r.Name)} ({r.Temperature:F1}°C)"))}");

        var syncCount = 0;

        foreach (var room in home.Rooms)
        {
            var moduleIds = room.ModuleIds ?? [];
            var valves = home.Modules
                             .Where(m => moduleIds.Contains(m.Id) && m.Type == "NRV")
                             .ToList();

            if (valves.Count == 0)
            {
                continue;
            }

            var sensor = SyncService.FindSensorForRoom(room.Name, indoorReadings, config);

            if (sensor is null)
            {
                continue;
            }

            var rs = roomStatuses.FirstOrDefault(r => r.Id == room.Id);
            if (rs?.MeasuredTemperature is null)
            {
                continue;
            }

            var valveTemp = rs.MeasuredTemperature.Value;
            var delta = sensor.Temperature - valveTemp;
            var valveNames = string.Join(", ", valves.Select(v => Markup.Escape(v.Name)));

            if (!SyncService.ShouldSync(sensor.Temperature, valveTemp))
            {
                AnsiConsole.MarkupLine(
                    $"  [bold]{Markup.Escape(room.Name)}[/] — sensor [blue]{Markup.Escape(sensor.Name)}[/] [cyan]{sensor.Temperature:F1}°C[/], valve [yellow]{valveTemp:F1}°C[/] [dim](no correction needed)[/]");

                continue;
            }

            if (!SyncService.IsDeltaSafe(sensor.Temperature, valveTemp))
            {
                AnsiConsole.MarkupLine(
                    $"  [bold]{Markup.Escape(room.Name)}[/] — sensor [blue]{Markup.Escape(sensor.Name)}[/] [cyan]{sensor.Temperature:F1}°C[/], valve [yellow]{valveTemp:F1}°C[/] (delta {delta:+0.0;-0.0}°C) [red]skipped — delta too large[/]");

                continue;
            }

            AnsiConsole.MarkupLine(
                $"  [bold]{Markup.Escape(room.Name)}[/] — sensor [blue]{Markup.Escape(sensor.Name)}[/] [cyan]{sensor.Temperature:F1}°C[/], valve [yellow]{valveTemp:F1}°C[/] (delta {delta:+0.0;-0.0}°C) → [green]{valveNames}[/]");

            if (!dryRun)
            {
                await client.SetTrueTemperatureAsync(home.Id, room.Id, valveTemp, sensor.Temperature, cancellationToken);
            }

            syncCount++;
        }

        if (syncCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No room/sensor matches found. Indoor module names must match room names.[/]");
        }

        AnsiConsole.MarkupLine($"[dim]{Markup.Escape($"[{timestamp}]")}[/] Sync complete — {syncCount} room(s) updated.");
    }

    private static async Task<List<SyncService.IndoorReading>?> GetIndoorReadingsAsync(NetatmoClient client, CancellationToken cancellationToken)
    {
        try
        {
            var stationsData = await client.GetStationsDataAsync(cancellationToken);
            return SyncService.ExtractIndoorReadings(stationsData.Body?.Devices ?? []);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]  Warning: Could not read weather station data: {Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }
}
