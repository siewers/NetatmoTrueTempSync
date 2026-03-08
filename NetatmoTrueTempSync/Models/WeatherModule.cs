using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record WeatherModule
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("module_name")]
    public string ModuleName { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("dashboard_data")]
    public DashboardData? DashboardData { get; init; }

    [JsonPropertyName("reachable")]
    public bool Reachable { get; init; }

    [JsonPropertyName("battery_percent")]
    public int? BatteryPercent { get; init; }
}
