using NetatmoTrueTempSync.Services;

namespace NetatmoTrueTempSync.Tests;

public class SyncLoggerTests
{
    [Test]
    public async Task Log_writes_csv_entry()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Living Room", "Sensor1", 21.5, 20.8, SyncAction.Synced);

        var output = writer.ToString();
        await Assert.That(output).Contains("Living Room,Sensor1,21.5,20.8,+0.7,synced");
    }

    [Test]
    public async Task Log_appends_multiple_entries()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Room1", "S1", 21.0, 20.5, SyncAction.Synced);
        await logger.LogAsync("Room2", "S2", 22.0, 23.5, SyncAction.DeltaTooLarge);
        await logger.LogAsync("Room3", "S3", 21.0, 21.0, SyncAction.NoCorrection);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        await Assert.That(lines).Count().IsEqualTo(3);
        await Assert.That(lines[0]).Contains("synced");
        await Assert.That(lines[1]).Contains("delta-too-large");
        await Assert.That(lines[2]).Contains("no-correction");
    }

    [Test]
    public async Task Log_escapes_commas_in_room_name()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Room, Main", "Sensor1", 21.0, 20.0, SyncAction.Synced);

        await Assert.That(writer.ToString()).Contains("\"Room, Main\"");
    }

    [Test]
    public async Task Log_includes_iso_timestamp()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Room1", "S1", 21.0, 20.0, SyncAction.Synced);

        // ISO 8601 format starts with year
        var line = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[0];
        await Assert.That(line).StartsWith("20");
    }

    [Test]
    public async Task Log_uses_same_cycle_id_for_all_entries()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Room1", "S1", 21.0, 20.0, SyncAction.Synced);
        await logger.LogAsync("Room2", "S2", 22.0, 21.0, SyncAction.Synced);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var cycleId1 = lines[0].Split(',')[1];
        var cycleId2 = lines[1].Split(',')[1];

        await Assert.That(cycleId1).IsEqualTo(cycleId2);
        await Assert.That(cycleId1).Length().IsEqualTo(8);
    }

    [Test]
    public async Task Log_formats_negative_delta()
    {
        await using var writer = new StringWriter();
        await using var logger = new SyncLogger(writer);

        await logger.LogAsync("Room1", "S1", 20.0, 21.5, SyncAction.Synced);

        await Assert.That(writer.ToString()).Contains("-1.5");
    }

    [Test]
    public async Task CreateForFile_writes_header_to_new_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sync-test-{Guid.NewGuid()}.log");

        try
        {
            await using (var logger = await SyncLogger.CreateForFileAsync(path))
            {
                await logger.LogAsync("Room1", "S1", 21.0, 20.0, SyncAction.Synced);
            }

            var lines = await File.ReadAllLinesAsync(path);

            await Assert.That(lines[0]).IsEqualTo("Timestamp,CycleId,Room,Sensor,SensorTemp,ValveTemp,Delta,Action");
            await Assert.That(lines).Count().IsEqualTo(2);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task CreateForFile_rotates_when_file_exceeds_max_size()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sync-test-{Guid.NewGuid()}.log");
        var rotatedPath = path + ".1";

        try
        {
            await File.WriteAllTextAsync(path, new string('x', 5 * 1024 * 1024 + 1));

            await using (var logger = await SyncLogger.CreateForFileAsync(path))
            {
                await logger.LogAsync("Room1", "S1", 21.0, 20.0, SyncAction.Synced);
            }

            await Assert.That(File.Exists(rotatedPath)).IsTrue();

            var lines = await File.ReadAllLinesAsync(path);
            await Assert.That(lines).Count().IsEqualTo(2);
        }
        finally
        {
            File.Delete(path);
            File.Delete(rotatedPath);
        }
    }
}
