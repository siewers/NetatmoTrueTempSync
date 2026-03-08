using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

[JsonSerializable(typeof(NetatmoResponse<HomesDataBody>))]
[JsonSerializable(typeof(NetatmoResponse<HomeStatusBody>))]
[JsonSerializable(typeof(NetatmoResponse<StationsDataBody>))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(WebSessionData))]
[JsonSerializable(typeof(TrueTemperatureRequest))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class AppJsonContext : JsonSerializerContext;
