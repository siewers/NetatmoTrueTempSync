using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record DashboardData
{
    [JsonPropertyName("Temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("Humidity")]
    public int? Humidity { get; init; }

    [JsonPropertyName("CO2")]
    public int? Co2 { get; init; }

    [JsonPropertyName("time_utc")]
    public long? TimeUtc { get; init; }
}
