using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

[JsonSerializable(typeof(NetatmoResponse<HomesDataBody>))]
[JsonSerializable(typeof(NetatmoResponse<HomeStatusBody>))]
[JsonSerializable(typeof(NetatmoResponse<StationsDataBody>))]
[JsonSerializable(typeof(TokenData))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(TrueTemperatureRequest))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class AppJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(NetatmoResponse<HomesDataBody>))]
[JsonSerializable(typeof(NetatmoResponse<HomeStatusBody>))]
[JsonSerializable(typeof(NetatmoResponse<StationsDataBody>))]
[JsonSerializable(typeof(TokenData))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(TrueTemperatureRequest))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class AppJsonIndentedContext : JsonSerializerContext;

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
