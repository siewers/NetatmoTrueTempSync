using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public sealed record WebSessionData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
