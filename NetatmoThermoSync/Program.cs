using NetatmoThermoSync.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("netatmo-thermosync");

    config.AddCommand<AuthCommand>("auth")
          .WithDescription("Authenticate with Netatmo (OAuth2 setup).");

    config.AddCommand<StatusCommand>("status")
          .WithDescription("Show current temperatures and device status for all rooms.");

    config.AddCommand<SyncCommand>("sync")
          .WithDescription("Sync thermostat readings to valve true_temperature corrections.");

    config.AddCommand<SetTempCommand>("set")
          .WithDescription("Set a room's target temperature manually.");

    config.AddCommand<DumpCommand>("dump")
          .WithDescription("Dump raw API data for debugging device/room associations.");
});

return await app.RunAsync(args);
