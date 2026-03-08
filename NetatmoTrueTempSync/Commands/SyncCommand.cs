using System.CommandLine;
using System.Globalization;
using NetatmoTrueTempSync.Api;
using NetatmoTrueTempSync.Auth;
using NetatmoTrueTempSync.Models;
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
        var config = await StatusCommand.LoadConfigOrFail(cancellationToken);
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

        var home = !string.IsNullOrEmpty(homeName)
            ? homes.FirstOrDefault(h =>
                  h.Name.Equals(homeName, StringComparison.OrdinalIgnoreCase) ||
                  h.Id == homeName) ??
              throw new NetatmoException($"Home '{homeName}' not found.")
            : homes.FirstOrDefault() ?? throw new NetatmoException("No homes found.");

        var status = await client.GetHomeStatusAsync(home.Id, cancellationToken);
        var roomStatuses = status.Body?.Home?.Rooms ?? [];

        // Get weather station indoor module readings
        var indoorReadings = await GetIndoorReadingsAsync(client, cancellationToken);

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

            // Check sensor_map config first, then fall back to name matching
            IndoorReading? sensor = null;
            if (config.SensorMap is not null &&
                config.SensorMap.TryGetValue(room.Name, out var mappedSensor))
            {
                sensor = indoorReadings.FirstOrDefault(r =>
                    r.Name.Equals(mappedSensor, StringComparison.OrdinalIgnoreCase));
            }

            sensor ??= indoorReadings.FirstOrDefault(r =>
                room.Name.Contains(r.Name, StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains(room.Name, StringComparison.OrdinalIgnoreCase));

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

            if (Math.Abs(delta) < 0.05)
            {
                AnsiConsole.MarkupLine(
                    $"  [bold]{Markup.Escape(room.Name)}[/] — sensor [blue]{Markup.Escape(sensor.Name)}[/] [cyan]{sensor.Temperature:F1}°C[/], valve [yellow]{valveTemp:F1}°C[/] [dim](no correction needed)[/]");

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

    private static async Task<List<IndoorReading>> GetIndoorReadingsAsync(NetatmoClient client, CancellationToken cancellationToken)
    {
        var readings = new List<IndoorReading>();

        try
        {
            var stationsData = await client.GetStationsDataAsync(cancellationToken);
            var devices = stationsData.Body?.Devices ?? [];

            foreach (var station in devices)
            {
                // The base station (NAMain) is itself an indoor module
                if (station is { Type: "NAMain", DashboardData.Temperature: not null })
                {
                    readings.Add(new IndoorReading(
                        station.ModuleName,
                        station.DashboardData.Temperature.Value));
                }

                // Additional indoor modules (NAModule4)
                foreach (var module in station.Modules.Where(m => m is { Type: "NAModule4", Reachable: true }))
                {
                    if (module.DashboardData?.Temperature is not null)
                    {
                        readings.Add(new IndoorReading(
                            module.ModuleName,
                            module.DashboardData.Temperature.Value));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]  Warning: Could not read weather station data: {Markup.Escape(ex.Message)}[/]");
        }

        return readings;
    }

    private sealed record IndoorReading(string Name, double Temperature);
}
