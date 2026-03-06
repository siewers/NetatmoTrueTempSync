using NetatmoThermoSync.Api;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetatmoThermoSync.Commands;

public class StatusCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var (config, tokens) = LoadConfigOrFail();
        using var client = new NetatmoClient(config, tokens);

        var homesData = await client.GetHomesDataAsync();
        var homes = homesData.Body?.Homes ?? [];

        if (homes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No homes found.[/]");
            return 0;
        }

        // Get weather station indoor module readings
        var indoorReadings = new List<(string Name, double Temp)>();
        try
        {
            var stationsData = await client.GetStationsDataAsync();
            foreach (var station in stationsData.Body?.Devices ?? [])
            {
                if (station.Type == "NAMain" && station.DashboardData?.Temperature is not null)
                    indoorReadings.Add((station.ModuleName, station.DashboardData.Temperature.Value));

                foreach (var module in station.Modules.Where(m => m.Type == "NAModule4" && m.Reachable && m.DashboardData?.Temperature is not null))
                    indoorReadings.Add((module.ModuleName, module.DashboardData!.Temperature!.Value));
            }
        }
        catch { /* read_station scope may not be granted yet */ }

        foreach (var home in homes)
        {
            AnsiConsole.MarkupLine($"[bold underline]{Markup.Escape(home.Name)}[/] [dim]({home.Id})[/]");
            AnsiConsole.WriteLine();

            var status = await client.GetHomeStatusAsync(home.Id);
            var roomStatuses = status.Body?.Home?.Rooms ?? [];
            var moduleStatuses = status.Body?.Home?.Modules ?? [];

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Room")
                .AddColumn("Valve")
                .AddColumn("Sensor")
                .AddColumn("Delta")
                .AddColumn("Setpoint")
                .AddColumn("Mode")
                .AddColumn("Heating")
                .AddColumn("Devices");

            foreach (var room in home.Rooms)
            {
                var rs = roomStatuses.FirstOrDefault(r => r.Id == room.Id);
                if (rs is null) continue;

                var roomModules = home.Modules
                    .Where(m => room.ModuleIds.Contains(m.Id))
                    .ToList();

                var moduleInfo = roomModules.Select(m =>
                {
                    var ms = moduleStatuses.FirstOrDefault(s => s.Id == m.Id);
                    var typeLabel = m.Type switch
                    {
                        "NATherm1" => "[blue]Thermostat[/]",
                        "NRV" => "[green]Valve[/]",
                        "NAPlug" => "[dim]Relay[/]",
                        _ => m.Type
                    };
                    var battery = ms?.BatteryState switch
                    {
                        "full" => "[green]████[/]",
                        "high" => "[green]███░[/]",
                        "medium" => "[yellow]██░░[/]",
                        "low" => "[red]█░░░[/]",
                        "very low" => "[red]░░░░[/]",
                        _ => "[dim]n/a[/]"
                    };
                    var reachable = ms?.Reachable == true ? "" : " [red](offline)[/]";
                    return $"{typeLabel} {Markup.Escape(m.Name)}{reachable} {battery}";
                });

                // Match indoor sensor to this room by name
                var sensor = indoorReadings.FirstOrDefault(r =>
                    room.Name.Contains(r.Name, StringComparison.OrdinalIgnoreCase) ||
                    r.Name.Contains(room.Name, StringComparison.OrdinalIgnoreCase));

                var sensorLabel = sensor != default
                    ? $"[blue]{sensor.Temp:F1}°C[/] [dim]({Markup.Escape(sensor.Name)})[/]"
                    : "[dim]—[/]";

                var deltaLabel = "[dim]—[/]";
                if (sensor != default && rs.MeasuredTemperature.HasValue)
                {
                    var delta = sensor.Temp - rs.MeasuredTemperature.Value;
                    var deltaColor = Math.Abs(delta) > 1.0 ? "red" : Math.Abs(delta) > 0.5 ? "yellow" : "green";
                    deltaLabel = $"[{deltaColor}]{delta:+0.0;-0.0}°C[/]";
                }

                var measuredColor = rs.MeasuredTemperature.HasValue && rs.SetpointTemperature.HasValue
                    ? (rs.MeasuredTemperature < rs.SetpointTemperature - 0.5 ? "red"
                        : rs.MeasuredTemperature > rs.SetpointTemperature + 0.5 ? "yellow"
                        : "green")
                    : "white";

                var heatingLabel = rs.HeatingPowerRequest switch
                {
                    null => "[dim]n/a[/]",
                    0 => "[dim]off[/]",
                    100 => "[red bold]100%[/]",
                    var p => $"[yellow]{p}%[/]"
                };

                table.AddRow(
                    $"[bold]{Markup.Escape(room.Name)}[/]",
                    rs.MeasuredTemperature.HasValue
                        ? $"[{measuredColor}]{rs.MeasuredTemperature:F1}°C[/]"
                        : "[dim]n/a[/]",
                    sensorLabel,
                    deltaLabel,
                    rs.SetpointTemperature.HasValue
                        ? $"{rs.SetpointTemperature:F1}°C"
                        : "[dim]n/a[/]",
                    rs.SetpointMode ?? "[dim]n/a[/]",
                    heatingLabel,
                    string.Join("\n", moduleInfo)
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        return 0;
    }

    internal static (AppConfig config, TokenData tokens) LoadConfigOrFail()
    {
        var config = TokenStore.LoadConfig()
            ?? throw new Exception("Not configured. Run 'auth' first.");
        var tokens = TokenStore.LoadTokens()
            ?? throw new Exception("Not authenticated. Run 'auth' first.");
        return (config, tokens);
    }
}
