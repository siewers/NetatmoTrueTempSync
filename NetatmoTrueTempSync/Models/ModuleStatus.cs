using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record ModuleStatus
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("reachable")]
    public bool Reachable { get; init; }

    [JsonPropertyName("battery_level")]
    public int? BatteryLevel { get; init; }

    [JsonPropertyName("battery_state")]
    public string? BatteryState { get; init; }

    [JsonPropertyName("rf_strength")]
    public int? RfStrength { get; init; }

    [JsonPropertyName("firmware_revision")]
    public int? FirmwareRevision { get; init; }

    [JsonPropertyName("boiler_status")]
    public bool? BoilerStatus { get; init; }

    [JsonPropertyName("wifi_strength")]
    public int? WifiStrength { get; init; }
}
