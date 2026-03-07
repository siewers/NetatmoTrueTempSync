using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record StationsDataBody
{
    [JsonPropertyName("devices")]
    public List<WeatherStation> Devices { get; init; } = [];
}
