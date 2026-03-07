using System.Text.Json.Serialization;

namespace NetatmoThermoSync.Models;

public record NetatmoResponse<T>
{
    [JsonPropertyName("body")]
    public T? Body { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("time_server")]
    public long TimeServer { get; init; }
}

// --- /homesdata ---

public record HomesDataBody
{
    [JsonPropertyName("homes")]
    public List<Home> Homes { get; init; } = [];
}

public record Home
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("rooms")]
    public List<Room> Rooms { get; init; } = [];

    [JsonPropertyName("modules")]
    public List<HomeModule> Modules { get; init; } = [];
}

public record Room
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("module_ids")]
    public List<string>? ModuleIds { get; init; }
}

public record HomeModule
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("room_id")]
    public string? RoomId { get; init; }

    [JsonPropertyName("bridge")]
    public string? Bridge { get; init; }
}

// --- /homestatus ---

public record HomeStatusBody
{
    [JsonPropertyName("home")]
    public HomeStatus? Home { get; init; }
}

public record HomeStatus
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("rooms")]
    public List<RoomStatus> Rooms { get; init; } = [];

    [JsonPropertyName("modules")]
    public List<ModuleStatus> Modules { get; init; } = [];
}

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

// --- /getstationsdata ---

public record StationsDataBody
{
    [JsonPropertyName("devices")]
    public List<WeatherStation> Devices { get; init; } = [];
}

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
