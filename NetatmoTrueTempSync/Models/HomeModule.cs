using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record HomeModule
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("room_id")]
    public string? RoomId { get; init; }

    [JsonPropertyName("bridge")]
    public string? Bridge { get; init; }
}
