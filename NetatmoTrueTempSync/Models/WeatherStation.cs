using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record WeatherStation
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("station_name")]
    public string StationName { get; init; } = "";

    [JsonPropertyName("module_name")]
    public string ModuleName { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("dashboard_data")]
    public DashboardData? DashboardData { get; init; }

    [JsonPropertyName("modules")]
    public List<WeatherModule> Modules { get; init; } = [];
}
