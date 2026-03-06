using System.ComponentModel;
using NetatmoThermoSync.Api;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetatmoThermoSync.Commands;

public class SetTempSettings : CommandSettings
{
    [CommandArgument(0, "<ROOM>")]
    [Description("Room name (case-insensitive partial match)")]
    public string RoomName { get; set; } = "";

    [CommandArgument(1, "<TEMP>")]
    [Description("Target temperature in °C")]
    public double Temperature { get; set; }

    [CommandOption("-d|--duration")]
    [Description("Duration in minutes (default: until next schedule change)")]
    public int? DurationMinutes { get; set; }

    [CommandOption("--home")]
    [Description("Home name or ID (defaults to first home)")]
    public string? HomeName { get; set; }
}

public class SetTempCommand : AsyncCommand<SetTempSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetTempSettings settings)
    {
        var (config, tokens) = StatusCommand.LoadConfigOrFail();
        using var client = new NetatmoClient(config, tokens);

        var homesData = await client.GetHomesDataAsync();
        var homes = homesData.Body?.Homes ?? [];

        var home = !string.IsNullOrEmpty(settings.HomeName)
            ? homes.FirstOrDefault(h =>
                h.Name.Equals(settings.HomeName, StringComparison.OrdinalIgnoreCase) ||
                h.Id == settings.HomeName)
              ?? throw new Exception($"Home '{settings.HomeName}' not found.")
            : homes.FirstOrDefault()
              ?? throw new Exception("No homes found.");

        var room = home.Rooms.FirstOrDefault(r =>
            r.Name.Contains(settings.RoomName, StringComparison.OrdinalIgnoreCase))
            ?? throw new Exception($"Room matching '{settings.RoomName}' not found.");

        int? endTime = settings.DurationMinutes.HasValue
            ? (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + settings.DurationMinutes.Value * 60)
            : null;

        await client.SetRoomThermPointAsync(home.Id, room.Id, settings.Temperature, endTime);

        var durationLabel = settings.DurationMinutes.HasValue
            ? $" for {settings.DurationMinutes} minutes"
            : " until next schedule change";

        AnsiConsole.MarkupLine(
            $"[green]Set [bold]{room.Name}[/] to [bold]{settings.Temperature:F1}°C[/]{durationLabel}.[/]");

        return 0;
    }
}
