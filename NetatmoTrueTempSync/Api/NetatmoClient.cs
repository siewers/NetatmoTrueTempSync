using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetatmoTrueTempSync.Auth;
using NetatmoTrueTempSync.Models;

namespace NetatmoTrueTempSync.Api;

public sealed class NetatmoClient(NetatmoCredentials credentials) : IDisposable
{
    private const string BaseUrl = "https://api.netatmo.com/api";
    private readonly WebSessionAuth _auth = new(credentials);
    private readonly HttpClient _http = new();
    private bool _authenticated;

    public void Dispose()
    {
        _http.Dispose();
        _auth.Dispose();
    }

    public async Task<NetatmoResponse<HomesDataBody>> GetHomesDataAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync($"{BaseUrl}/homesdata", cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseHomesDataBody) ?? throw new NetatmoException("Failed to parse homesdata response.");
    }

    public async Task<NetatmoResponse<HomeStatusBody>> GetHomeStatusAsync(string homeId, CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync($"{BaseUrl}/homestatus?home_id={Uri.EscapeDataString(homeId)}", cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseHomeStatusBody) ?? throw new NetatmoException("Failed to parse homestatus response.");
    }

    public async Task<NetatmoResponse<StationsDataBody>> GetStationsDataAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync($"{BaseUrl}/getstationsdata", cancellationToken);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseStationsDataBody) ?? throw new NetatmoException("Failed to parse getstationsdata response.");
    }

    public async Task SetTrueTemperatureAsync(string homeId, string roomId, double currentTemp, double correctedTemp, CancellationToken cancellationToken = default)
    {
        using var response = await SendWithRetryAsync(HttpMethod.Post, $"{BaseUrl}/truetemperature", () =>
            JsonContent.Create(new TrueTemperatureRequest
            {
                HomeId = homeId,
                RoomId = roomId,
                CurrentTemperature = currentTemp,
                CorrectedTemperature = correctedTemp,
            }, AppJsonContext.Default.TrueTemperatureRequest), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NetatmoException($"truetemperature failed ({response.StatusCode}): {json}");
        }
    }

    private async Task<string> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(HttpMethod.Get, url, null, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"API call failed ({response.StatusCode}): {json}");
        }

        return json;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string url, Func<HttpContent>? contentFactory, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        using var request = new HttpRequestMessage(method, url);
        if (contentFactory is not null)
        {
            request.Content = contentFactory();
        }

        var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            if (await _auth.TryReauthenticateAsync(cancellationToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

                using var retryRequest = new HttpRequestMessage(method, url);
                if (contentFactory is not null)
                {
                    retryRequest.Content = contentFactory();
                }

                response = await _http.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_authenticated)
        {
            return;
        }

        await _auth.LoginAsync(cancellationToken);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        _authenticated = true;
    }
}
