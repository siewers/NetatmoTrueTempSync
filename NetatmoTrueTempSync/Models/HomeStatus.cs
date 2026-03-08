using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record HomeStatus
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("rooms")]
    public List<RoomStatus> Rooms { get; init; } = [];

    [JsonPropertyName("modules")]
    public List<ModuleStatus> Modules { get; init; } = [];
}
