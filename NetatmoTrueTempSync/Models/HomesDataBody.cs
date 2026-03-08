using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record HomesDataBody
{
    [JsonPropertyName("homes")]
    public List<Home> Homes { get; init; } = [];
}
