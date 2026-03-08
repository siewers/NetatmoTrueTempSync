using System.CommandLine;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;

namespace NetatmoThermoSync.Commands;

public static class AuthCommand
{
    public static Command Create()
    {
        var command = new Command("auth", "Authenticate with Netatmo (web session login).");
        command.SetAction(ExecuteAsync);
        return command;
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold yellow]Netatmo Setup[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Netatmo account credentials[/]");

        TokenStore.TryLoadCredentials(out var existingCredentials);

        var emailPrompt = new TextPrompt<string>("Email:");
        if (!string.IsNullOrEmpty(existingCredentials?.Email))
        {
            emailPrompt.DefaultValue(existingCredentials.Email);
        }

        var email = AnsiConsole.Prompt(emailPrompt);
        var password = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());

        TokenStore.SaveCredentials(email, password);

        AnsiConsole.WriteLine();
        try
        {
            using var webAuth = new WebSessionAuth(new NetatmoCredentials(email, password));
            await webAuth.LoginAsync(cancellationToken);
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
