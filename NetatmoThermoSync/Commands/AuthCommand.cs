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

        var existingConfig = await TokenStore.LoadConfig(cancellationToken);

        AnsiConsole.MarkupLine("[bold]Netatmo account credentials[/]");

        var emailPrompt = new TextPrompt<string>("Email:");
        if (!string.IsNullOrEmpty(existingConfig?.NetatmoEmail))
        {
            emailPrompt.DefaultValue(existingConfig.NetatmoEmail);
        }

        var email = AnsiConsole.Prompt(emailPrompt);

        var passwordPrompt = new TextPrompt<string>("Password:").Secret();
        if (!string.IsNullOrEmpty(existingConfig?.NetatmoPassword))
        {
            passwordPrompt.DefaultValue(existingConfig.NetatmoPassword);
        }

        var password = AnsiConsole.Prompt(passwordPrompt);

        var config = new AppConfig
        {
            NetatmoEmail = email,
            NetatmoPassword = password,
            SensorMap = existingConfig?.SensorMap,
        };

        await TokenStore.SaveConfig(config, cancellationToken);

        AnsiConsole.WriteLine();
        try
        {
            using var webAuth = new WebSessionAuth(config.GetNetatmoCredentials());
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
