using System.CommandLine;
using NetatmoTrueTempSync.Auth;
using NetatmoTrueTempSync.Models;
using Spectre.Console;

namespace NetatmoTrueTempSync.Commands;

public static class LoginCommand
{
    internal static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold yellow]Netatmo Setup[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Netatmo account credentials[/]");

        TokenStore.TryLoadCredentials(out var existingCredentials);

        var email = AnsiConsole.Prompt(
            new TextPrompt<string>("Email:")
               .DefaultValue(existingCredentials?.Email ?? string.Empty)
        );

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
               .Secret()
               .DefaultValue(existingCredentials?.Password ?? string.Empty)
        );

        TokenStore.SaveCredentials(email, password);

        AnsiConsole.WriteLine();
        try
        {
            using var webAuth = new WebSessionAuth(new NetatmoCredentials(email, password));
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Signing in...", async _ => await webAuth.LoginAsync(cancellationToken));
            AnsiConsole.MarkupLine("[bold green]Login successful! Session saved.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Login failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
