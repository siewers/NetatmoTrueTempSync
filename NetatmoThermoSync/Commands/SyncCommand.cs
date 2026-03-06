using System.ComponentModel;
using NetatmoThermoSync.Api;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetatmoThermoSync.Commands;

public class SyncSettings : CommandSettings
{
    [CommandOption("--dry-run")]
    [Description("Show what would be synced without making changes")]
    public bool DryRun { get; set; }

    [CommandOption("--home")]
    [Description("Home name or ID to sync (defaults to first home)")]
    public string? HomeName { get; set; }
}

public class SyncCommand : AsyncCommand<SyncSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SyncSettings settings)
    {
        var (config, tokens) = StatusCommand.LoadConfigOrFail();
        using var client = new NetatmoClient(config, tokens);

        if (string.IsNullOrEmpty(config.NetatmoEmail) || string.IsNullOrEmpty(config.NetatmoPassword))
        {
            AnsiConsole.MarkupLine("[red]Netatmo account credentials not configured. Run 'auth' to set them up.[/]");
            return 1;
        }

        // Authenticate via web session for truetemperature access
        var webAuth = new WebSessionAuth();
        AnsiConsole.MarkupLine("[dim]Logging in via web session...[/]");
        await webAuth.LoginAsync(config.NetatmoEmail, config.NetatmoPassword);

        if (settings.DryRun)
            AnsiConsole.MarkupLine("[yellow]Dry run mode — no changes will be made.[/]");

        try
        {
            await RunSyncCycleAsync(client, webAuth, settings, config);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Sync error: {ex}");
            return 1;
        }

        return 0;
    }

    private static async Task RunSyncCycleAsync(NetatmoClient client, WebSessionAuth webAuth, SyncSettings settings, AppConfig config)
    {
        var homesData = await client.GetHomesDataAsync();
        var homes = homesData.Body?.Homes ?? [];

        var home = !string.IsNullOrEmpty(settings.HomeName)
            ? homes.FirstOrDefault(h =>
                h.Name.Equals(settings.HomeName, StringComparison.OrdinalIgnoreCase) ||
                h.Id == settings.HomeName)
              ?? throw new Exception($"Home '{settings.HomeName}' not found.")
            : homes.FirstOrDefault()
              ?? throw new Exception("No homes found.");

        var status = await client.GetHomeStatusAsync(home.Id);
        var roomStatuses = status.Body?.Home?.Rooms ?? [];
        var moduleStatuses = status.Body?.Home?.Modules ?? [];

        // Get weather station indoor module readings
        var indoorReadings = await GetIndoorReadingsAsync(client);

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape($"[{timestamp}]")}[/] Syncing [bold]{Markup.Escape(home.Name)}[/]...");

        if (indoorReadings.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No weather station indoor modules found. Run 'auth' again to grant read_station scope.[/]");
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

            if (valves.Count == 0) continue;

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

            if (sensor is null) continue;

            var rs = roomStatuses.FirstOrDefault(r => r.Id == room.Id);
            if (rs?.MeasuredTemperature is null) continue;

            var valveTemp = rs.MeasuredTemperature.Value;
            var delta = sensor.Temperature - valveTemp;
            var valveNames = string.Join(", ", valves.Select(v => Markup.Escape(v.Name)));

            AnsiConsole.MarkupLine(
                $"  [bold]{Markup.Escape(room.Name)}[/] — sensor [blue]{Markup.Escape(sensor.Name)}[/] [cyan]{sensor.Temperature:F1}°C[/], valve [yellow]{valveTemp:F1}°C[/] (delta {delta:+0.0;-0.0}°C) → [green]{valveNames}[/]");

            if (!settings.DryRun)
            {
                await webAuth.SetTrueTemperatureAsync(home.Id, room.Id, valveTemp, sensor.Temperature);
            }

            syncCount++;
        }

        if (syncCount == 0)
            AnsiConsole.MarkupLine("[yellow]  No room/sensor matches found. Indoor module names must match room names.[/]");

        AnsiConsole.MarkupLine($"[dim]{Markup.Escape($"[{timestamp}]")}[/] Sync complete — {syncCount} room(s) updated.");
    }

    private static async Task<List<IndoorReading>> GetIndoorReadingsAsync(NetatmoClient client)
    {
        var readings = new List<IndoorReading>();

        try
        {
            var stationsData = await client.GetStationsDataAsync();
            var devices = stationsData.Body?.Devices ?? [];

            foreach (var station in devices)
            {
                // The base station (NAMain) is itself an indoor module
                if (station.Type == "NAMain" && station.DashboardData?.Temperature is not null)
                {
                    readings.Add(new IndoorReading(
                        station.ModuleName,
                        station.DashboardData.Temperature.Value,
                        station.Id));
                }

                // Additional indoor modules (NAModule4)
                foreach (var module in station.Modules.Where(m => m.Type == "NAModule4" && m.Reachable))
                {
                    if (module.DashboardData?.Temperature is not null)
                    {
                        readings.Add(new IndoorReading(
                            module.ModuleName,
                            module.DashboardData.Temperature.Value,
                            module.Id));
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

    private record IndoorReading(string Name, double Temperature, string ModuleId);
}
