using NetatmoTrueTempSync.Models;
using NetatmoTrueTempSync.Services;

namespace NetatmoTrueTempSync.Tests;

public class SyncServiceTests
{
    // --- ExtractIndoorReadings ---

    [Test]
    public async Task ExtractIndoorReadings_includes_base_station_with_temperature()
    {
        var stations = new List<WeatherStation>
        {
            new()
            {
                Type = "NAMain",
                ModuleName = "Living Room",
                DashboardData = new DashboardData { Temperature = 21.5 },
            },
        };

        var readings = SyncService.ExtractIndoorReadings(stations);

        await Assert.That(readings).HasSingleItem();
        await Assert.That(readings[0].Name).IsEqualTo("Living Room");
        await Assert.That(readings[0].Temperature).IsEqualTo(21.5);
    }

    [Test]
    public async Task ExtractIndoorReadings_excludes_base_station_without_temperature()
    {
        var stations = new List<WeatherStation>
        {
            new() { Type = "NAMain", ModuleName = "Station", DashboardData = new DashboardData() },
        };

        var readings = SyncService.ExtractIndoorReadings(stations);

        await Assert.That(readings).IsEmpty();
    }

    [Test]
    public async Task ExtractIndoorReadings_includes_reachable_indoor_modules()
    {
        var stations = new List<WeatherStation>
        {
            new()
            {
                Type = "NAMain",
                ModuleName = "Station",
                Modules =
                [
                    new WeatherModule
                    {
                        Type = "NAModule4",
                        ModuleName = "Bedroom",
                        Reachable = true,
                        DashboardData = new DashboardData { Temperature = 19.3 },
                    },
                ],
            },
        };

        var readings = SyncService.ExtractIndoorReadings(stations);

        await Assert.That(readings).Contains(new SyncService.IndoorReading("Bedroom", 19.3));
    }

    [Test]
    public async Task ExtractIndoorReadings_excludes_unreachable_modules()
    {
        var stations = new List<WeatherStation>
        {
            new()
            {
                Type = "NAMain",
                ModuleName = "Station",
                Modules =
                [
                    new WeatherModule
                    {
                        Type = "NAModule4",
                        ModuleName = "Bedroom",
                        Reachable = false,
                        DashboardData = new DashboardData { Temperature = 19.3 },
                    },
                ],
            },
        };

        var readings = SyncService.ExtractIndoorReadings(stations);

        await Assert.That(readings).DoesNotContain(new SyncService.IndoorReading("Bedroom", 19.3));
    }

    [Test]
    public async Task ExtractIndoorReadings_excludes_non_indoor_modules()
    {
        var stations = new List<WeatherStation>
        {
            new()
            {
                Type = "NAMain",
                ModuleName = "Station",
                Modules =
                [
                    new WeatherModule
                    {
                        Type = "NAModule1",
                        ModuleName = "Outdoor",
                        Reachable = true,
                        DashboardData = new DashboardData { Temperature = 5.0 },
                    },
                ],
            },
        };

        var readings = SyncService.ExtractIndoorReadings(stations);

        await Assert.That(readings).DoesNotContain(new SyncService.IndoorReading("Outdoor", 5.0));
    }

    [Test]
    public async Task ExtractIndoorReadings_returns_empty_for_no_stations()
    {
        var readings = SyncService.ExtractIndoorReadings([]);

        await Assert.That(readings).IsEmpty();
    }

    // --- FindSensorForRoom ---

    private static readonly List<SyncService.IndoorReading> TestReadings =
    [
        new("Living Room", 21.0),
        new("Bedroom", 19.5),
        new("Kitchen", 20.0),
    ];

    [Test]
    public async Task FindSensorForRoom_uses_sensor_map_when_configured()
    {
        var config = new AppConfig
        {
            SensorMap = new Dictionary<string, string> { { "Lounge", "Living Room" } },
        };

        var result = SyncService.FindSensorForRoom("Lounge", TestReadings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Living Room");
    }

    [Test]
    public async Task FindSensorForRoom_sensor_map_is_case_insensitive()
    {
        var config = new AppConfig
        {
            SensorMap = new Dictionary<string, string> { { "Lounge", "living room" } },
        };

        var result = SyncService.FindSensorForRoom("Lounge", TestReadings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Living Room");
    }

    [Test]
    public async Task FindSensorForRoom_falls_back_to_name_matching()
    {
        var config = new AppConfig();

        var result = SyncService.FindSensorForRoom("Bedroom", TestReadings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Bedroom");
    }

    [Test]
    public async Task FindSensorForRoom_matches_partial_room_contains_sensor()
    {
        var readings = new List<SyncService.IndoorReading> { new("Living", 21.0) };
        var config = new AppConfig();

        var result = SyncService.FindSensorForRoom("Living Room", readings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Living");
    }

    [Test]
    public async Task FindSensorForRoom_matches_partial_sensor_contains_room()
    {
        var readings = new List<SyncService.IndoorReading> { new("Living Room Sensor", 21.0) };
        var config = new AppConfig();

        var result = SyncService.FindSensorForRoom("Living Room", readings, config);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task FindSensorForRoom_name_matching_is_case_insensitive()
    {
        var config = new AppConfig();

        var result = SyncService.FindSensorForRoom("bedroom", TestReadings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Bedroom");
    }

    [Test]
    public async Task FindSensorForRoom_returns_null_when_no_match()
    {
        var config = new AppConfig();

        var result = SyncService.FindSensorForRoom("Garage", TestReadings, config);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindSensorForRoom_falls_back_to_name_matching_when_sensor_map_entry_not_found()
    {
        var config = new AppConfig
        {
            SensorMap = new Dictionary<string, string> { { "Bedroom", "Nonexistent Sensor" } },
        };

        var result = SyncService.FindSensorForRoom("Bedroom", TestReadings, config);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Bedroom");
    }

    // --- ShouldSync ---

    [Test]
    [Arguments(21.0, 20.0, true)]
    [Arguments(20.0, 21.0, true)]
    [Arguments(21.0, 20.95, true)]
    [Arguments(21.0, 20.96, false)]
    [Arguments(21.0, 21.0, false)]
    [Arguments(21.0, 21.04, false)]
    public async Task ShouldSync_respects_threshold(double sensorTemp, double valveTemp, bool expected)
    {
        await Assert.That(SyncService.ShouldSync(sensorTemp, valveTemp)).IsEqualTo(expected);
    }

    // --- IsDeltaSafe ---

    [Test]
    [Arguments(21.0, 21.5, true)]
    [Arguments(21.0, 22.0, true)]
    [Arguments(21.0, 20.0, true)]
    [Arguments(21.0, 22.1, false)]
    [Arguments(21.0, 19.9, false)]
    [Arguments(18.0, 22.0, false)]
    public async Task IsDeltaSafe_respects_max_delta(double sensorTemp, double valveTemp, bool expected)
    {
        await Assert.That(SyncService.IsDeltaSafe(sensorTemp, valveTemp)).IsEqualTo(expected);
    }

    // --- FindHome ---

    private static readonly List<Home> TestHomes =
    [
        new() { Id = "abc123", Name = "My Home" },
        new() { Id = "def456", Name = "Summer House" },
    ];

    [Test]
    public async Task FindHome_returns_first_home_when_no_name_specified()
    {
        var home = SyncService.FindHome(TestHomes, null);

        await Assert.That(home.Id).IsEqualTo("abc123");
    }

    [Test]
    public async Task FindHome_finds_by_name_case_insensitive()
    {
        var home = SyncService.FindHome(TestHomes, "summer house");

        await Assert.That(home.Id).IsEqualTo("def456");
    }

    [Test]
    public async Task FindHome_finds_by_id()
    {
        var home = SyncService.FindHome(TestHomes, "def456");

        await Assert.That(home.Name).IsEqualTo("Summer House");
    }

    [Test]
    public async Task FindHome_throws_when_name_not_found()
    {
        await Assert.That(() => SyncService.FindHome(TestHomes, "Unknown")).ThrowsExactly<NetatmoException>();
    }

    [Test]
    public async Task FindHome_throws_when_no_homes_exist()
    {
        await Assert.That(() => SyncService.FindHome([], null)).ThrowsExactly<NetatmoException>();
    }
}
