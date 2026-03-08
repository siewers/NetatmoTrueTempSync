using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public sealed record AppConfig
{
    [JsonPropertyName("sensor_map")]
    public Dictionary<string, string>? SensorMap { get; init; }
}
