using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record HomeStatusBody
{
    [JsonPropertyName("home")]
    public HomeStatus? Home { get; init; }
}
