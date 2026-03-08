using System.CommandLine;

namespace NetatmoTrueTempSync.Commands;

public static class CommandExtensions
{
    public static Command WithAction(this Command command, Func<ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(action);
        return command;
    }

    public static Command WithAction(this Command command, Func<ParseResult, int> action)
    {
        command.SetAction(action);
        return command;
    }
}
