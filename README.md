# NetatmoTrueTempSync

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
./NetatmoTrueTempSync auth login
```

The `auth login` command prompts for your Netatmo email and password, then performs a web session login. Credentials and session tokens are stored securely using the OS secret store (Keychain on macOS, secret-tool on Linux, file-based fallback otherwise).

Use `auth logout` to clear all stored credentials and session data.

## Usage

```sh
# Authenticate with Netatmo
NetatmoTrueTempSync auth login

# Clear stored credentials and session
NetatmoTrueTempSync auth logout

# Show current temperatures and device status
NetatmoTrueTempSync status

# Preview what would be synced
NetatmoTrueTempSync sync --dry-run

# Sync true temperature corrections
NetatmoTrueTempSync sync

# Sync a specific home
NetatmoTrueTempSync sync --home "My Home"

# Dump raw API data for debugging
NetatmoTrueTempSync dump

# Install as a background service (runs sync every 10 minutes)
NetatmoTrueTempSync service install

# Check service status
NetatmoTrueTempSync service status

# Remove the background service
NetatmoTrueTempSync service uninstall
```

## Sensor mapping

By default, rooms are matched to indoor sensors by name. If your room and sensor names don't match, add a `sensor_map` to `~/.config/netatmo-truetempsync/config.json`:

```json
{
  "sensor_map": {
    "Living Room": "Indoor Module 1",
    "Office": "Indoor Module 2"
  }
}
```

Keys are room names, values are sensor module names.

## Running as a background service

The `service` commands set up the app to run `sync` automatically every 10 minutes using the platform's native service manager:

- **macOS**: launchd plist in `~/Library/LaunchAgents/`
- **Linux**: systemd user timer in `~/.config/systemd/user/`

```sh
# Publish the app first
dotnet publish -c Release

# Install and start the service
./NetatmoTrueTempSync service install

# Check if the service is installed and running
./NetatmoTrueTempSync service status

# Stop and remove the service
./NetatmoTrueTempSync service uninstall
```

The service uses the path of the currently running binary, so make sure you run `service install` from the published executable (not via `dotnet run`).

## License

MIT
