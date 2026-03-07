using System.CommandLine;
using NetatmoThermoSync.Commands;

var rootCommand = new RootCommand("Netatmo ThermoSync — sync weather station temperatures to smart radiator valves")
{
    AuthCommand.Create(),
    StatusCommand.Create(),
    SyncCommand.Create(),
    SetTempCommand.Create(),
    DumpCommand.Create(),
};

return await rootCommand.Parse(args).InvokeAsync();
