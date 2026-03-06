using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using NetatmoThermoSync.Auth;
using NetatmoThermoSync.Models;

namespace NetatmoThermoSync.Api;

public sealed class NetatmoClient : IDisposable
{
    private const string BaseUrl = "https://api.netatmo.com/api";
    private readonly AppConfig _config;
    private readonly HttpClient _http = new();
    private TokenData _tokens;

    public NetatmoClient(AppConfig config, TokenData tokens)
    {
        _config = config;
        _tokens = tokens;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    public void Dispose() => _http.Dispose();

    private async Task EnsureTokenValidAsync()
    {
        if (!TokenStore.IsTokenExpired(_tokens))
        {
            return;
        }

        _tokens = await OAuthFlow.RefreshAsync(_config, _tokens);
        TokenStore.SaveTokens(_tokens);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
    }

    public async Task<NetatmoResponse<HomesDataBody>> GetHomesDataAsync()
    {
        await EnsureTokenValidAsync();
        var response = await _http.GetAsync($"{BaseUrl}/homesdata");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"homesdata failed ({response.StatusCode}): {json}");
        }

        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseHomesDataBody) ?? throw new NetatmoException("Failed to parse homesdata response.");
    }

    public async Task<NetatmoResponse<HomeStatusBody>> GetHomeStatusAsync(string homeId)
    {
        await EnsureTokenValidAsync();
        var response = await _http.GetAsync($"{BaseUrl}/homestatus?home_id={Uri.EscapeDataString(homeId)}");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"homestatus failed ({response.StatusCode}): {json}");
        }

        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseHomeStatusBody) ?? throw new NetatmoException("Failed to parse homestatus response.");
    }

    public async Task<NetatmoResponse<StationsDataBody>> GetStationsDataAsync()
    {
        await EnsureTokenValidAsync();
        var response = await _http.GetAsync($"{BaseUrl}/getstationsdata");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"getstationsdata failed ({response.StatusCode}): {json}");
        }

        return JsonSerializer.Deserialize(json, AppJsonContext.Default.NetatmoResponseStationsDataBody) ?? throw new NetatmoException("Failed to parse getstationsdata response.");
    }

    public async Task SetRoomThermPointAsync(string homeId, string roomId, double temp, int? endTime = null)
    {
        await EnsureTokenValidAsync();
        var parameters = new Dictionary<string, string>
        {
            ["home_id"] = homeId,
            ["room_id"] = roomId,
            ["mode"] = "manual",
            ["temp"] = temp.ToString("F1", CultureInfo.InvariantCulture),
        };

        if (endTime.HasValue)
        {
            parameters["endtime"] = endTime.Value.ToString(CultureInfo.InvariantCulture);
        }

        var content = new FormUrlEncodedContent(parameters);
        var response = await _http.PostAsync($"{BaseUrl}/setroomthermpoint", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new NetatmoException($"setroomthermpoint failed ({response.StatusCode}): {json}");
        }
    }
}
