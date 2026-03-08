using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record Room
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("module_ids")]
    public List<string>? ModuleIds { get; init; }
}
