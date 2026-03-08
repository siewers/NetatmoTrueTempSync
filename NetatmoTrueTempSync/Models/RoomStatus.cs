using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record RoomStatus
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("reachable")]
    public bool Reachable { get; init; }

    [JsonPropertyName("therm_measured_temperature")]
    public double? MeasuredTemperature { get; init; }

    [JsonPropertyName("therm_setpoint_temperature")]
    public double? SetpointTemperature { get; init; }

    [JsonPropertyName("therm_setpoint_mode")]
    public string? SetpointMode { get; init; }

    [JsonPropertyName("heating_power_request")]
    public int? HeatingPowerRequest { get; init; }

    [JsonPropertyName("anticipating")]
    public bool? Anticipating { get; init; }

    [JsonPropertyName("open_window")]
    public bool? OpenWindow { get; init; }
}
