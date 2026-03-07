using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record HomeStatusBody
{
    [JsonPropertyName("home")]
    public HomeStatus? Home { get; init; }
}
