using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public sealed record AppConfig
{
    [JsonPropertyName("netatmo_email")]
    public string? NetatmoEmail { get; init; }

    [JsonPropertyName("netatmo_password")]
    public string? NetatmoPassword { get; init; }

    [JsonPropertyName("sensor_map")]
    public Dictionary<string, string>? SensorMap { get; init; }

    public NetatmoCredentials GetNetatmoCredentials() =>
        !string.IsNullOrEmpty(NetatmoEmail) && !string.IsNullOrEmpty(NetatmoPassword)
            ? new NetatmoCredentials(NetatmoEmail, NetatmoPassword)
            : throw new InvalidOperationException("Netatmo account credentials not configured. Run 'auth' to set them up.");
}
