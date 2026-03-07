using System.CommandLine;
using NetatmoThermoSync.Api;
using Spectre.Console;

namespace NetatmoThermoSync.Commands;

public static class SetTempCommand
{
    public static Command Create()
    {
        var roomArgument = new Argument<string>("room") { Description = "Room name (case-insensitive partial match)" };
        var tempArgument = new Argument<double>("temp") { Description = "Target temperature in °C" };
        var durationOption = new Option<int?>("--duration", "-d") { Description = "Duration in minutes (default: until next schedule change)" };
        var homeOption = new Option<string?>("--home") { Description = "Home name or ID (defaults to first home)" };

        var command = new Command("set", "Set a room's target temperature manually.")
        {
            roomArgument,
            tempArgument,
            durationOption,
            homeOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var roomName = parseResult.GetValue(roomArgument);
            var temperature = parseResult.GetValue(tempArgument);
            var durationMinutes = parseResult.GetValue(durationOption);
            var homeName = parseResult.GetValue(homeOption);
            return await ExecuteAsync(roomName!, temperature, durationMinutes, homeName, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string roomName, double temperature, int? durationMinutes, string? homeName, CancellationToken cancellationToken)
    {
        var (config, tokens) = StatusCommand.LoadConfigOrFail();
        using var client = new NetatmoClient(config, tokens);

        var homesData = await client.GetHomesDataAsync(cancellationToken);
        var homes = homesData.Body?.Homes ?? [];

        var home = !string.IsNullOrEmpty(homeName)
            ? homes.FirstOrDefault(h =>
                  h.Name.Equals(homeName, StringComparison.OrdinalIgnoreCase) ||
                  h.Id == homeName) ??
              throw new NetatmoException($"Home '{homeName}' not found.")
            : homes.FirstOrDefault() ?? throw new NetatmoException("No homes found.");

        var room = home.Rooms.FirstOrDefault(r =>
                       r.Name.Contains(roomName, StringComparison.OrdinalIgnoreCase)) ??
                   throw new NetatmoException($"Room matching '{roomName}' not found.");

        int? endTime = durationMinutes.HasValue
            ? (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + durationMinutes.Value * 60)
            : null;

        await client.SetRoomThermPointAsync(home.Id, room.Id, temperature, endTime, cancellationToken);

        var durationLabel = durationMinutes.HasValue
            ? $" for {durationMinutes} minutes"
            : " until next schedule change";

        AnsiConsole.MarkupLine(
            $"[green]Set [bold]{room.Name}[/] to [bold]{temperature:F1}°C[/]{durationLabel}.[/]");

        return 0;
    }
}
