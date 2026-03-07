using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record HomesDataBody
{
    [JsonPropertyName("homes")]
    public List<Home> Homes { get; init; } = [];
}
