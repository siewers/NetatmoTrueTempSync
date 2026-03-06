using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetatmoThermoSync.Commands;

public class AuthCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
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
            await webAuth.LoginAsync(email, password);
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
            var tokens = await OAuthFlow.AuthorizeAsync(config);
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
