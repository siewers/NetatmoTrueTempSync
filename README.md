# NetatmoThermoSync

A CLI tool that syncs Netatmo Weather Station indoor module temperatures to smart radiator valve (NRV) true temperature corrections.

## Why?

Netatmo smart radiator valves measure temperature at the radiator, which is typically warmer than the actual room temperature. Netatmo's "true temperature" feature allows correcting this, but only through their app UI. This tool automates that correction using readings from Netatmo Weather Station indoor modules placed elsewhere in the room.

## How it works

1. Reads indoor temperatures from your Netatmo Weather Station base station and additional indoor modules (NAModule4)
2. Compares them to the valve-reported temperatures for each room
3. Applies the difference as a true temperature correction via Netatmo's `/api/truetemperature` endpoint

Rooms are matched to sensors by name (case-insensitive partial match), or explicitly via `sensor_map` in the config.

## Requirements

- .NET 10 SDK (for building)
- A Netatmo account with smart radiator valves and a Weather Station with indoor modules

## Setup

```sh
# Build
dotnet publish -c Release

# Authenticate
./NetatmoThermoSync auth login

# Clear stored credentials and session
./NetatmoThermoSync auth logout
```

The `auth login` command prompts for your Netatmo email and password, then performs a web session login. Credentials and session tokens are stored securely using the OS secret store (Keychain on macOS, secret-tool on Linux, file-based fallback otherwise).

Use `auth logout` to clear all stored credentials and session data.

## Usage

```sh
# Show current temperatures and device status
NetatmoThermoSync status

# Preview what would be synced
NetatmoThermoSync sync --dry-run

# Sync true temperature corrections
NetatmoThermoSync sync

# Sync a specific home
NetatmoThermoSync sync --home "My Home"

# Dump raw API data for debugging
NetatmoThermoSync dump
```

## Sensor mapping

By default, rooms are matched to indoor sensors by name. If your room and sensor names don't match, add a `sensor_map` to `~/.config/netatmo-thermosync/config.json`:

```json
{
  "sensor_map": {
    "Living Room": "Indoor Module 1",
    "Office": "Indoor Module 2"
  }
}
```

Keys are room names, values are sensor module names.

## Running as a service (macOS)

Create a launchd plist at `~/Library/LaunchAgents/com.netatmo.thermosync.plist`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.netatmo.thermosync</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Users/YOU/.local/bin/NetatmoThermoSync</string>
        <string>sync</string>
    </array>
    <key>StartInterval</key>
    <integer>600</integer>
    <key>StandardOutPath</key>
    <string>/tmp/netatmo-thermosync.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/netatmo-thermosync.err</string>
</dict>
</plist>
```

```sh
launchctl load ~/Library/LaunchAgents/com.netatmo.thermosync.plist
```

## License

MIT
