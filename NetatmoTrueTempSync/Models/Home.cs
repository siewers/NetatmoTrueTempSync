using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record Home
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("rooms")]
    public List<Room> Rooms { get; init; } = [];

    [JsonPropertyName("modules")]
    public List<HomeModule> Modules { get; init; } = [];
}
