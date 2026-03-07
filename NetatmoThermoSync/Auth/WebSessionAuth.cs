using System.Net;
using System.Text;
using System.Text.Json;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync.Auth;

/// <summary>
///     Authenticates via Netatmo's web login flow (cookie-based session).
///     Required for the /api/truetemperature endpoint which doesn't accept third-party OAuth tokens.
/// </summary>
public sealed class WebSessionAuth : IDisposable
{
    private const string AuthBase = "https://auth.netatmo.com";
    private const string UserAgent = "netatmo-home";

    private readonly CookieContainer _cookies = new();
    private readonly NetatmoCredentials _credentials;
    private readonly HttpClient _http;

    public WebSessionAuth(NetatmoCredentials credentials)
    {
        _credentials = credentials;

        var handler = new HttpClientHandler { CookieContainer = _cookies, AllowAutoRedirect = true };
        _http = new HttpClient(handler);
        _http.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private string? AccessToken { get; set; }

    public void Dispose() => _http.Dispose();

    /// <summary>
    ///     Tries the cached web session token first. Falls back to a full login only if no
    ///     cached token exists. Re-auth on API rejection is handled by <see cref="SetTrueTemperatureAsync" />.
    /// </summary>
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        var cached = TokenStore.LoadWebSession();
        if (cached is not null)
        {
            AccessToken = cached.AccessToken;
            return;
        }

        await FullLoginAsync(cancellationToken);
    }

    public async Task SetTrueTemperatureAsync(string homeId, string roomId, double currentTemp, double correctedTemp, CancellationToken cancellationToken = default)
    {
        if (AccessToken is null)
        {
            throw new NetatmoException("Not authenticated. Call LoginAsync first.");
        }

        var response = await SendTrueTemperatureAsync(homeId, roomId, currentTemp, correctedTemp, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            // Try refreshing the session before doing a full login
            if (await TryRefreshSessionAsync(cancellationToken) || await TryFullLoginAsync(cancellationToken))
            {
                response = await SendTrueTemperatureAsync(homeId, roomId, currentTemp, correctedTemp, cancellationToken);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NetatmoException($"truetemperature failed ({response.StatusCode}): {responseJson}");
        }
    }

    /// <summary>
    ///     Attempts to refresh the web session using the cached refresh token cookie.
    ///     Injects the refresh token into the cookie container and hits /access/keychain.
    /// </summary>
    private async Task<bool> TryRefreshSessionAsync(CancellationToken cancellationToken)
    {
        var cached = TokenStore.LoadWebSession();
        if (cached?.RefreshToken is null)
        {
            return false;
        }

        _cookies.Add(new Uri("https://auth.netatmo.com"),
            new Cookie("authnetatmocomrefresh_token", cached.RefreshToken, "/", ".auth.netatmo.com"));

        await _http.GetAsync($"{AuthBase}/access/keychain?next_url=https://my.netatmo.com", cancellationToken);

        var accessToken = ExtractAccessToken();
        if (accessToken is null)
        {
            return false;
        }

        AccessToken = accessToken;

        // Grab the potentially refreshed refresh token
        var refreshCookie = _cookies.GetCookies(new Uri("https://auth.netatmo.com"))["authnetatmocomrefresh_token"];
        TokenStore.SaveWebSession(new WebSessionData
        {
            AccessToken = AccessToken,
            RefreshToken = refreshCookie?.Value ?? cached.RefreshToken,
        });

        return true;
    }

    private async Task<bool> TryFullLoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            await FullLoginAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task FullLoginAsync(CancellationToken cancellationToken)
    {
        // Step 1: Get initial session cookie
        var loginPage = await _http.GetAsync($"{AuthBase}/en-us/access/login", cancellationToken);
        if (!loginPage.IsSuccessStatusCode)
        {
            throw new NetatmoException($"Failed to load login page: {loginPage.StatusCode}");
        }

        // Step 2: Set required cookie
        _cookies.Add(new Uri("https://netatmo.com"), new Cookie("netatmocomlast_app_used", "app_thermostat", "/", ".netatmo.com"));

        // Step 3: Get CSRF token
        var csrfResponse = await _http.GetAsync($"{AuthBase}/access/csrf", cancellationToken);
        if (!csrfResponse.IsSuccessStatusCode)
        {
            throw new NetatmoException($"Failed to get CSRF token: {csrfResponse.StatusCode}");
        }

        var csrfJson = await csrfResponse.Content.ReadAsStringAsync(cancellationToken);
        var csrfDoc = JsonDocument.Parse(csrfJson);
        var csrfToken = csrfDoc.RootElement.GetProperty("token").GetString() ?? throw new NetatmoException("CSRF token not found in response");

        // Step 4: Submit login credentials
        var loginPayload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = _credentials.Email,
            ["password"] = _credentials.Password,
            ["stay_logged"] = "on",
            ["_token"] = csrfToken,
        });

        await _http.PostAsync($"{AuthBase}/access/postlogin", loginPayload, cancellationToken);

        // Step 5: Complete authentication flow
        await _http.GetAsync($"{AuthBase}/access/keychain?next_url=https://my.netatmo.com", cancellationToken);

        // Step 6: Extract access token from cookies
        AccessToken = ExtractAccessToken()
            ?? throw new NetatmoException("Login succeeded but access token cookie not found. Check your credentials.");

        // Save both access token and refresh token for session reuse
        var refreshCookie = _cookies.GetCookies(new Uri("https://auth.netatmo.com"))["authnetatmocomrefresh_token"];

        TokenStore.SaveWebSession(new WebSessionData
        {
            AccessToken = AccessToken,
            RefreshToken = refreshCookie?.Value,
        });
    }

    private string? ExtractAccessToken()
    {
        foreach (var domain in new[] { "https://netatmo.com", "https://auth.netatmo.com", "https://my.netatmo.com" })
        {
            var cookie = _cookies.GetCookies(new Uri(domain))["netatmocomaccess_token"];
            if (cookie is not null)
            {
                return cookie.Value.Replace("%7C", "|");
            }
        }

        return null;
    }

    private async Task<HttpResponseMessage> SendTrueTemperatureAsync(string homeId, string roomId, double currentTemp, double correctedTemp, CancellationToken cancellationToken)
    {
        var payload = new TrueTemperatureRequest
        {
            HomeId = homeId,
            RoomId = roomId,
            CurrentTemperature = currentTemp,
            CorrectedTemperature = correctedTemp,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.netatmo.com/api/truetemperature");
        request.Headers.Add("Authorization", $"Bearer {AccessToken}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, AppJsonContext.Default.TrueTemperatureRequest),
            Encoding.UTF8,
            "application/json");

        return await _http.SendAsync(request, cancellationToken);
    }
}
