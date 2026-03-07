using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record WebSessionData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
