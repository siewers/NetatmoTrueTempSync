using System.Text.Json.Serialization;

namespace NetatmoTrueTempSync.Models;

public record NetatmoResponse<T>
{
    [JsonPropertyName("body")]
    public T? Body { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("time_server")]
    public long TimeServer { get; init; }
}
