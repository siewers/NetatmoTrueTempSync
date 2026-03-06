using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record TokenData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; init; }

    [JsonPropertyName("scope")]
    public List<string>? Scope { get; init; }
}

public record AppConfig
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; init; } = "";

    [JsonPropertyName("netatmo_email")]
    public string? NetatmoEmail { get; init; }

    [JsonPropertyName("netatmo_password")]
    public string? NetatmoPassword { get; init; }

    [JsonPropertyName("sensor_map")]
    public Dictionary<string, string>? SensorMap { get; init; }
}
