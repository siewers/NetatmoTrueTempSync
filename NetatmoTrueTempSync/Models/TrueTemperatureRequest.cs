using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record TrueTemperatureRequest
{
    [JsonPropertyName("home_id")]
    public string HomeId { get; init; } = "";

    [JsonPropertyName("room_id")]
    public string RoomId { get; init; } = "";

    [JsonPropertyName("current_temperature")]
    public double CurrentTemperature { get; init; }

    [JsonPropertyName("corrected_temperature")]
    public double CorrectedTemperature { get; init; }
}
