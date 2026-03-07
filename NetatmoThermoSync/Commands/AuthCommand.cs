using System.CommandLine;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;

namespace NetatmoThermoSync.Commands;

public static class AuthCommand
{
    public static Command Create()
    {
        var command = new Command("auth", "Authenticate with Netatmo (OAuth2 setup).");
        command.SetAction(async (_, cancellationToken) => await ExecuteAsync(cancellationToken));
        return command;
    }

    private static async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold yellow]Netatmo Setup[/]");
        AnsiConsole.WriteLine();

        var existingConfig = TokenStore.LoadConfig();

        // OAuth2 credentials (for reading data)
        AnsiConsole.MarkupLine("[bold]OAuth2 API credentials[/] [dim](from dev.netatmo.com)[/]");
        var clientId = AnsiConsole.Prompt(
            new TextPrompt<string>("Client ID:")
               .DefaultValue(existingConfig?.ClientId ?? ""));

        var clientSecret = AnsiConsole.Prompt(
            new TextPrompt<string>("Client Secret:")
               .Secret()
               .DefaultValue(existingConfig?.ClientSecret ?? ""));

        // Netatmo account credentials (for truetemperature)
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Netatmo account credentials[/] [dim](for true temperature calibration)[/]");
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>("Email:")
               .DefaultValue(existingConfig?.NetatmoEmail ?? ""));

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
               .Secret()
               .DefaultValue(existingConfig?.NetatmoPassword ?? ""));

        var config = new AppConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            NetatmoEmail = email,
            NetatmoPassword = password,
        };

        TokenStore.SaveConfig(config);

        // Test web session login
        AnsiConsole.WriteLine();
        try
        {
            var webAuth = new WebSessionAuth();
            await webAuth.LoginAsync(email, password, cancellationToken);
            AnsiConsole.MarkupLine("[bold green]Web session login successful![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Web session login failed: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]True temperature sync won't work, but other features will.[/]");
        }

        // OAuth2 flow
        try
        {
            var tokens = await OAuthFlow.AuthorizeAsync(config, cancellationToken);
            TokenStore.SaveTokens(tokens);
            AnsiConsole.MarkupLine("[bold green]OAuth2 authorization successful! Tokens saved.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]OAuth2 authorization failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
