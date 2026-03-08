using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record StationsDataBody
{
    [JsonPropertyName("devices")]
    public List<WeatherStation> Devices { get; init; } = [];
}
