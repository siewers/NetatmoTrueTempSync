using System.CommandLine;
using NetatmoTrueTempSync;
using NetatmoTrueTempSync.Commands;
using Spectre.Console;

var rootCommand = new RootCommand("Netatmo TrueTempSync — sync weather station temperatures to smart radiator valves")
{
    new Command("auth", "Manage Netatmo authentication.")
    {
        new Command("login", "Authenticate with Netatmo (web session login).")
           .WithAction(LoginCommand.ExecuteAsync),
        new Command("logout", "Clear cached credentials and session data.")
           .WithAction(LogoutCommand.Execute),
    },
    new Command("status", "Show current temperatures and device status for all rooms.")
       .WithAction(StatusCommand.ExecuteAsync),
    new Command("sync", "Sync thermostat readings to valve true_temperature corrections.")
    {
        SyncCommand.DryRunOption,
        SyncCommand.HomeOption,
    }.WithAction(SyncCommand.ExecuteAsync),
    new Command("dump", "Dump raw API data for debugging device/room associations.")
       .WithAction(DumpCommand.ExecuteAsync),
};

var config = new InvocationConfiguration { EnableDefaultExceptionHandler = false };

try
{
    return await rootCommand.Parse(args).InvokeAsync(config);
}
catch (MissingCredentialsException ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)} Run [bold]auth login[/] first.");
    return 1;
}
