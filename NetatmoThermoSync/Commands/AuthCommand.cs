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
        var clientIdPrompt = new TextPrompt<string>("Client ID:");
        if (!string.IsNullOrEmpty(existingConfig?.ClientId))
        {
            clientIdPrompt.DefaultValue(existingConfig.ClientId);
        }

        var clientId = AnsiConsole.Prompt(clientIdPrompt);

        var clientSecretPrompt = new TextPrompt<string>("Client Secret:").Secret();
        if (!string.IsNullOrEmpty(existingConfig?.ClientSecret))
        {
            clientSecretPrompt.DefaultValue(existingConfig.ClientSecret);
        }

        var clientSecret = AnsiConsole.Prompt(clientSecretPrompt);

        // Netatmo account credentials (for truetemperature)
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Netatmo account credentials[/] [dim](for true temperature calibration)[/]");

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
            var webAuth = new WebSessionAuth(new NetatmoCredentials(email, password));
            await webAuth.LoginAsync(cancellationToken);
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
