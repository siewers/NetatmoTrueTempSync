using System.CommandLine;
using System.Globalization;
using NetatmoThermoSync.Api;
using NetatmoThermoSync.Auth;
using Spectre.Console;

namespace NetatmoThermoSync.Commands;

public static class DumpCommand
{
    public static Command Create()
    {
        var command = new Command("dump", "Dump raw API data for debugging device/room associations.");
        command.SetAction(ExecuteAsync);
        return command;
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var config = await StatusCommand.LoadConfigOrFail(cancellationToken);
        using var webAuth = new WebSessionAuth(config.GetNetatmoCredentials());
        await webAuth.LoginAsync(cancellationToken);
        using var client = new NetatmoClient(webAuth);

        var homesData = await client.GetHomesDataAsync(cancellationToken);
        var homes = homesData.Body?.Homes ?? [];

        foreach (var home in homes)
        {
            AnsiConsole.MarkupLine($"[bold underline]{home.Name}[/]");
            AnsiConsole.WriteLine();

            // Show all modules with their types and room assignments
            AnsiConsole.MarkupLine("[bold]Modules from /homesdata:[/]");
            foreach (var m in home.Modules)
            {
                var roomName = home.Rooms.FirstOrDefault(r => r.Id == m.RoomId)?.Name ?? "(no room)";
                AnsiConsole.WriteLine($"  {m.Type,-12} {Markup.Escape(m.Name),-20} room={Markup.Escape(roomName),-20} id={m.Id} bridge={m.Bridge ?? "none"}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Rooms from /homesdata:[/]");
            foreach (var r in home.Rooms)
            {
                var moduleNames = home.Modules
                                      .Where(m => r.ModuleIds?.Contains(m.Id) == true)
                                      .Select(m => $"{m.Type}:{Markup.Escape(m.Name)}");

                AnsiConsole.WriteLine($"  {Markup.Escape(r.Name),-20} id={r.Id} modules=[{string.Join(", ", moduleNames)}]");
            }

            AnsiConsole.WriteLine();

            var status = await client.GetHomeStatusAsync(home.Id, cancellationToken);
            AnsiConsole.MarkupLine("[bold]Modules from /homestatus:[/]");
            foreach (var ms in status.Body?.Home?.Modules ?? [])
            {
                AnsiConsole.MarkupLine($"  {ms.Type,-12} id={ms.Id} reachable={ms.Reachable} battery={ms.BatteryState ?? "n/a"} boiler={ms.BoilerStatus?.ToString() ?? "n/a"}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Rooms from /homestatus:[/]");
            foreach (var rs in status.Body?.Home?.Rooms ?? [])
            {
                AnsiConsole.MarkupLine(
                    $"  id={rs.Id,-10} measured={rs.MeasuredTemperature?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a"}°C setpoint={rs.SetpointTemperature?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a"}°C mode={rs.SetpointMode ?? "n/a"} heating={rs.HeatingPowerRequest?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}%");
            }

            AnsiConsole.WriteLine();
        }

        // Weather station data
        try
        {
            var stationsData = await client.GetStationsDataAsync(cancellationToken);
            var devices = stationsData.Body?.Devices ?? [];

            if (devices.Count > 0)
            {
                AnsiConsole.MarkupLine("[bold underline]Weather Stations (/getstationsdata)[/]");
                AnsiConsole.WriteLine();

                foreach (var station in devices)
                {
                    AnsiConsole.WriteLine($"  Station: {station.StationName} (type={station.Type}, id={station.Id})");
                    AnsiConsole.WriteLine(
                        $"    Base module: {station.ModuleName} temp={station.DashboardData?.Temperature?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a"}°C humidity={station.DashboardData?.Humidity?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}% CO2={station.DashboardData?.Co2?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}ppm");

                    foreach (var module in station.Modules)
                    {
                        AnsiConsole.WriteLine($"    Module: {module.ModuleName} (type={module.Type}, id={module.Id}, reachable={module.Reachable})");
                        if (module.DashboardData is not null)
                        {
                            AnsiConsole.WriteLine(
                                $"      temp={module.DashboardData.Temperature?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a"}°C humidity={module.DashboardData.Humidity?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}% CO2={module.DashboardData.Co2?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}ppm");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not read weather station data: {Markup.Escape(ex.Message)}[/]");
        }

        return 0;
    }
}
