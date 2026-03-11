using NetatmoTrueTempSync.Models;

namespace NetatmoTrueTempSync.Services;

public static class SyncService
{
    private const double SyncThreshold = 0.05;
    private const double MaxDelta = 1.0;

    public static List<IndoorReading> ExtractIndoorReadings(IEnumerable<WeatherStation> stations)
    {
        var readings = new List<IndoorReading>();

        foreach (var station in stations)
        {
            if (station is { Type: "NAMain", DashboardData.Temperature: not null })
            {
                readings.Add(new IndoorReading(station.ModuleName, station.DashboardData.Temperature.Value));
            }

            foreach (var module in station.Modules.Where(m => m is { Type: "NAModule4", Reachable: true }))
            {
                if (module.DashboardData?.Temperature is not null)
                {
                    readings.Add(new IndoorReading(module.ModuleName, module.DashboardData.Temperature.Value));
                }
            }
        }

        return readings;
    }

    public static IndoorReading? FindSensorForRoom(string roomName, List<IndoorReading> readings, AppConfig config)
    {
        if (config.SensorMap is not null &&
            config.SensorMap.TryGetValue(roomName, out var mappedSensor))
        {
            var mapped = readings.FirstOrDefault(r => r.Name.Equals(mappedSensor, StringComparison.OrdinalIgnoreCase));

            if (mapped is not null)
            {
                return mapped;
            }
        }

        return readings.FirstOrDefault(r => roomName.Contains(r.Name, StringComparison.OrdinalIgnoreCase) ||
                                            r.Name.Contains(roomName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ShouldSync(double sensorTemp, double valveTemp) =>
        Math.Abs(sensorTemp - valveTemp) >= SyncThreshold;

    public static bool IsDeltaSafe(double sensorTemp, double valveTemp) =>
        Math.Abs(sensorTemp - valveTemp) <= MaxDelta;

    public static Home FindHome(List<Home> homes, string? homeName)
    {
        if (!string.IsNullOrEmpty(homeName))
        {
            return homes.FirstOrDefault(h => h.Name.Equals(homeName, StringComparison.OrdinalIgnoreCase) ||
                                             h.Id == homeName) ??
                   throw new NetatmoException($"Home '{homeName}' not found.");
        }

        return homes.FirstOrDefault() ?? throw new NetatmoException("No homes found.");
    }

    public sealed record IndoorReading(string Name, double Temperature);
}
