using System.CommandLine;
using NetatmoTrueTempSync.Auth;
using Spectre.Console;

namespace NetatmoTrueTempSync.Commands;

public static class LogoutCommand
{
    internal static int Execute(ParseResult parseResult)
    {
        var clearedCredentials = TokenStore.DeleteCredentials();
        var clearedSession = TokenStore.DeleteWebSession();

        if (!clearedCredentials && !clearedSession)
        {
            AnsiConsole.MarkupLine("[dim]No credentials or session data found.[/]");
            return 0;
        }

        if (clearedCredentials)
        {
            AnsiConsole.MarkupLine("[green]Cleared stored credentials.[/]");
        }

        if (clearedSession)
        {
            AnsiConsole.MarkupLine("[green]Cleared cached session.[/]");
        }

        return 0;
    }
}
