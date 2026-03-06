using System.Net;
using System.Text.Json;
using NetatmoThermoSync.Models;
using Spectre.Console;

namespace NetatmoThermoSync.Auth;

/// <summary>
/// Authenticates via Netatmo's web login flow (cookie-based session).
/// Required for the /api/truetemperature endpoint which doesn't accept third-party OAuth tokens.
/// </summary>
public class WebSessionAuth
{
    private const string AuthBase = "https://auth.netatmo.com";
    private const string UserAgent = "netatmo-home";

    private readonly CookieContainer _cookies = new();
    private readonly HttpClient _http;
    private string? _accessToken;

    public string? AccessToken => _accessToken;

    public WebSessionAuth()
    {
        var handler = new HttpClientHandler { CookieContainer = _cookies, AllowAutoRedirect = true };
        _http = new HttpClient(handler);
        _http.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        // Step 1: Get initial session cookie
        var loginPage = await _http.GetAsync($"{AuthBase}/en-us/access/login");
        if (!loginPage.IsSuccessStatusCode)
            throw new Exception($"Failed to load login page: {loginPage.StatusCode}");

        // Step 2: Set required cookie
        _cookies.Add(new Uri("https://netatmo.com"), new Cookie("netatmocomlast_app_used", "app_thermostat", "/", ".netatmo.com"));

        // Step 3: Get CSRF token
        var csrfResponse = await _http.GetAsync($"{AuthBase}/access/csrf");
        if (!csrfResponse.IsSuccessStatusCode)
            throw new Exception($"Failed to get CSRF token: {csrfResponse.StatusCode}");

        var csrfJson = await csrfResponse.Content.ReadAsStringAsync();
        var csrfDoc = JsonDocument.Parse(csrfJson);
        var csrfToken = csrfDoc.RootElement.GetProperty("token").GetString()
            ?? throw new Exception("CSRF token not found in response");

        // Step 4: Submit login credentials
        var loginPayload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = password,
            ["stay_logged"] = "on",
            ["_token"] = csrfToken,
        });

        await _http.PostAsync($"{AuthBase}/access/postlogin", loginPayload);

        // Step 5: Complete authentication flow
        await _http.GetAsync($"{AuthBase}/access/keychain?next_url=https://my.netatmo.com");

        // Step 6: Extract access token from cookies
        var allCookies = _cookies.GetCookies(new Uri("https://netatmo.com"));
        var tokenCookie = allCookies["netatmocomaccess_token"];

        if (tokenCookie is null)
        {
            // Also check .netatmo.com domain
            allCookies = _cookies.GetCookies(new Uri("https://auth.netatmo.com"));
            tokenCookie = allCookies["netatmocomaccess_token"];
        }

        if (tokenCookie is null)
        {
            // Try my.netatmo.com
            allCookies = _cookies.GetCookies(new Uri("https://my.netatmo.com"));
            tokenCookie = allCookies["netatmocomaccess_token"];
        }

        if (tokenCookie is null)
            throw new Exception("Login succeeded but access token cookie not found. Check your credentials.");

        _accessToken = tokenCookie.Value.Replace("%7C", "|");
        return true;
    }

    public async Task SetTrueTemperatureAsync(string homeId, string roomId, double currentTemp, double correctedTemp)
    {
        if (_accessToken is null)
            throw new Exception("Not authenticated. Call LoginAsync first.");

        var payload = new TrueTemperatureRequest
        {
            HomeId = homeId,
            RoomId = roomId,
            CurrentTemperature = currentTemp,
            CorrectedTemperature = correctedTemp
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.netatmo.com/api/truetemperature");
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, AppJsonContext.Default.TrueTemperatureRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"truetemperature failed ({response.StatusCode}): {responseJson}");
    }
}
