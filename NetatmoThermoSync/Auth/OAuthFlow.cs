using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Web;
using NetatmoThermoSync.Models;
using Spectre.Console;

namespace NetatmoThermoSync.Auth;

public static class OAuthFlow
{
    private const string AuthorizeUrl = "https://api.netatmo.com/oauth2/authorize";
    private const string TokenUrl = "https://api.netatmo.com/oauth2/token";
    private const string Scopes = "read_thermostat write_thermostat read_station";
    private const int CallbackPort = 11842;
    private static readonly string RedirectUri = $"http://localhost:{CallbackPort}/callback";
    private static readonly HttpClient Http = new();

    public static async Task<TokenData> AuthorizeAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        var state = Guid.NewGuid().ToString("N");

        var authUrl = $"{AuthorizeUrl}?client_id={Uri.EscapeDataString(config.ClientId)}" + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" + $"&scope={Uri.EscapeDataString(Scopes)}" + $"&state={state}" + "&response_type=code";

        AnsiConsole.MarkupLine("[bold]Open this URL in your browser to authorize:[/]");
        AnsiConsole.MarkupLine($"[link={authUrl}]{authUrl}[/]");
        AnsiConsole.WriteLine();

        // Try to open the browser automatically
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            // Ignore — user can open manually
        }

        var code = await WaitForCallbackAsync(state, cancellationToken);
        return await ExchangeCodeAsync(config, code, cancellationToken);
    }

    private static async Task<string> WaitForCallbackAsync(string expectedState, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{CallbackPort}/");
        listener.Start();

        AnsiConsole.MarkupLine("[dim]Waiting for authorization callback...[/]");

        var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
        var query = context.Request.Url?.Query ?? "";
        var parameters = HttpUtility.ParseQueryString(query);

        var code = parameters["code"];
        var state = parameters["state"];
        var error = parameters["error"];

        var response = context.Response;

        if (!string.IsNullOrEmpty(error))
        {
            var errorHtml = "<html><body><h1>Authorization Failed</h1><p>You can close this window.</p></body></html>"u8.ToArray();
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(errorHtml, cancellationToken);
            response.Close();
            throw new NetatmoException($"Authorization failed: {error}");
        }

        if (state != expectedState)
        {
            var stateHtml = "<html><body><h1>State Mismatch</h1><p>Security check failed.</p></body></html>"u8.ToArray();
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(stateHtml, cancellationToken);
            response.Close();
            throw new NetatmoException("OAuth state mismatch — possible CSRF attack.");
        }

        var successHtml = "<html><body><h1>Authorization Successful</h1><p>You can close this window and return to the terminal.</p></body></html>"u8.ToArray();
        response.StatusCode = 200;
        await response.OutputStream.WriteAsync(successHtml, cancellationToken);
        response.Close();
        listener.Stop();

        return code ?? throw new NetatmoException("No authorization code received.");
    }

    private static async Task<TokenData> ExchangeCodeAsync(AppConfig config, string code, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = Scopes,
        });

        var response = await Http.PostAsync(TokenUrl, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"Token exchange failed ({response.StatusCode}): {json}");
        }

        var tokens = JsonSerializer.Deserialize(json, AppJsonContext.Default.TokenData) ?? throw new NetatmoException("Failed to parse token response.");

        // Compute absolute expiry time
        tokens = tokens with
        {
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokens.ExpiresIn,
        };

        return tokens;
    }

    public static async Task<TokenData> RefreshAsync(AppConfig config, TokenData tokens, CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["refresh_token"] = tokens.RefreshToken,
        });

        var response = await Http.PostAsync(TokenUrl, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"Token refresh failed ({response.StatusCode}): {json}");
        }

        var newTokens = JsonSerializer.Deserialize(json, AppJsonContext.Default.TokenData) ?? throw new NetatmoException("Failed to parse refresh token response.");

        newTokens = newTokens with
        {
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + newTokens.ExpiresIn,
        };

        return newTokens;
    }
}
