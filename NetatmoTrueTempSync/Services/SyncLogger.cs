using System.Globalization;

namespace NetatmoTrueTempSync.Services;

public sealed class SyncLogger(TextWriter writer) : IAsyncDisposable
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const string Header = "Timestamp,CycleId,Room,Sensor,SensorTemp,ValveTemp,Delta,Action";
    private readonly string _cycleId = Guid.NewGuid().ToString()[..8];

    public async Task LogAsync(string room, string sensor, double sensorTemp, double valveTemp, SyncAction action)
    {
        var delta = sensorTemp - valveTemp;
        var timestamp = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);

        await writer.WriteLineAsync(
            $"{timestamp},{_cycleId},{Escape(room)},{Escape(sensor)},{sensorTemp:F1},{valveTemp:F1},{delta:+0.0;-0.0},{action.ToLogString()}");

        await writer.FlushAsync();
    }

    public async ValueTask DisposeAsync() => await writer.DisposeAsync();

    public static async Task<SyncLogger> CreateForFileAsync(string logPath)
    {
        var dir = Path.GetDirectoryName(logPath)!;
        Directory.CreateDirectory(dir);

        RotateIfNeeded(logPath);

        var isNew = !File.Exists(logPath) || new FileInfo(logPath).Length == 0;
        var streamWriter = new StreamWriter(logPath, append: true);

        if (isNew)
        {
            await streamWriter.WriteLineAsync(Header);
        }

        return new SyncLogger(streamWriter);
    }

    private static void RotateIfNeeded(string logPath)
    {
        if (!File.Exists(logPath))
        {
            return;
        }

        var info = new FileInfo(logPath);
        if (info.Length < MaxFileSize)
        {
            return;
        }

        var rotated = logPath + ".1";
        if (File.Exists(rotated))
        {
            File.Delete(rotated);
        }

        File.Move(logPath, rotated);
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

public enum SyncAction
{
    Synced,
    NoCorrection,
    DeltaTooLarge,
}

public static class SyncActionExtensions
{
    public static string ToLogString(this SyncAction action) => action switch
    {
        SyncAction.Synced => "synced",
        SyncAction.NoCorrection => "no-correction",
        SyncAction.DeltaTooLarge => "delta-too-large",
        _ => "unknown",
    };
}
