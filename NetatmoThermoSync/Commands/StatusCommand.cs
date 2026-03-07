using System.CommandLine;
using NetatmoThermoSync.Api;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;

namespace NetatmoThermoSync.Commands;

public static class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show current temperatures and device status for all rooms.");
        command.SetAction(ExecuteAsync);
        return command;
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var config = await LoadConfigOrFail(cancellationToken);
        using var webAuth = new WebSessionAuth(config.GetNetatmoCredentials());
        await webAuth.LoginAsync(cancellationToken);
        using var client = new NetatmoClient(webAuth);

        var homesData = await client.GetHomesDataAsync(cancellationToken);
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
            var stationsData = await client.GetStationsDataAsync(cancellationToken);
            foreach (var station in stationsData.Body?.Devices ?? [])
            {
                if (station is { Type: "NAMain", DashboardData.Temperature: not null })
                {
                    indoorReadings.Add((station.ModuleName, station.DashboardData.Temperature.Value));
                }

                foreach (var module in station.Modules.Where(m => m is { Type: "NAModule4", Reachable: true, DashboardData.Temperature: not null }))
                {
                    indoorReadings.Add((module.ModuleName, module.DashboardData!.Temperature!.Value));
                }
            }
        }
        catch
        {
            /* weather station data may not be available */
        }

        foreach (var home in homes)
        {
            AnsiConsole.MarkupLine($"[bold underline]{Markup.Escape(home.Name)}[/] [dim]({home.Id})[/]");
            AnsiConsole.WriteLine();

            var status = await client.GetHomeStatusAsync(home.Id, cancellationToken);
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
                if (rs is null)
                {
                    continue;
                }

                var roomModules = home.Modules
                                      .Where(m => room.ModuleIds?.Contains(m.Id) == true)
                                      .ToList();

                var moduleInfo = roomModules.Select(m =>
                {
                    var ms = moduleStatuses.FirstOrDefault(s => s.Id == m.Id);
                    var typeLabel = m.Type switch
                    {
                        "NATherm1" => "[blue]Thermostat[/]",
                        "NRV" => "[green]Valve[/]",
                        "NAPlug" => "[dim]Relay[/]",
                        _ => m.Type,
                    };

                    var battery = ms?.BatteryState switch
                    {
                        "full" => "[green]████[/]",
                        "high" => "[green]███░[/]",
                        "medium" => "[yellow]██░░[/]",
                        "low" => "[red]█░░░[/]",
                        "very low" => "[red]░░░░[/]",
                        _ => "[dim]n/a[/]",
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

                var measuredColor = rs is { MeasuredTemperature: not null, SetpointTemperature: not null }
                    ? rs.MeasuredTemperature < rs.SetpointTemperature - 0.5 ? "red"
                    : rs.MeasuredTemperature > rs.SetpointTemperature + 0.5 ? "yellow"
                    : "green"
                    : "white";

                var heatingLabel = rs.HeatingPowerRequest switch
                {
                    null => "[dim]n/a[/]",
                    0 => "[dim]off[/]",
                    100 => "[red bold]100%[/]",
                    var p => $"[yellow]{p}%[/]",
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

    internal static async Task<AppConfig> LoadConfigOrFail(CancellationToken cancellationToken)
    {
        return await TokenStore.LoadConfig(cancellationToken) ?? throw new NetatmoException("Not configured. Run 'auth' first.");
    }
}
