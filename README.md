# Twinkly.Net

A comprehensive .NET client library for controlling Twinkly LED devices via their REST API.

## Features

- **Full REST API Coverage**: Implements 39+ endpoints from the Twinkly REST API
- **Type-Safe**: Strongly typed DTOs with XML documentation
- **Async/Await**: Modern asynchronous API design
- **Automatic Authentication**: Handles authentication and token refresh automatically
- **Comprehensive LED Control**: Support for RGB, RGBW, and AWW LED profiles
- **Real-Time Control**: Both HTTP and UDP protocols for frame streaming
- **Effect Management**: Control built-in effects, movies, and playlists
- **Network Management**: Configure device network settings
- **Layout Management**: Upload and manage 3D LED coordinates

## Installation

```bash
dotnet add package Twinkly.Net
```

## Quick Start

```csharp
using System.Net;
using Microsoft.Extensions.Logging;
using Twinkly.Net;

// Create logger
var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<TwinklyClient>();

// Create HTTP client
using var httpClient = new HttpClient();

// Connect to device
var client = await TwinklyClient.Create(
    IPAddress.Parse("192.168.1.100"), 
    logger, 
    httpClient
);

// Set LED color to red
await client.SetRgbColor(255, 0, 0);

// Turn on lights
await client.SetLedMode(OperationMode.Color);
```

## Supported Endpoints

### Authentication & Device Management
- `POST /xled/v1/login` - Authenticate and get access token
- `POST /xled/v1/verify` - Verify authentication token
- `POST /xled/v1/logout` - Invalidate access token
- `GET /xled/v1/gestalt` - Get detailed device information
- `GET /xled/v1/device_name` - Get device name
- `POST /xled/v1/device_name` - Set device name
- `GET /xled/v1/fw/version` - Get firmware version
- `GET /xled/v1/status` - Get device status
- `POST /xled/v1/echo` - Echo test endpoint
- `GET /xled/v1/summary` - Get device summary

### LED Control
- `GET /xled/v1/led/mode` - Get current operation mode
- `POST /xled/v1/led/mode` - Set operation mode (Off, Color, Demo, Effect, Movie, Playlist, RT)
- `GET /xled/v1/led/color` - Get current LED color
- `POST /xled/v1/led/color` - Set LED color (HSV, RGB, RGBW, AWW)
- `GET /xled/v1/led/out/brightness` - Get brightness settings
- `POST /xled/v1/led/out/brightness` - Set brightness
- `GET /xled/v1/led/out/saturation` - Get saturation settings
- `POST /xled/v1/led/out/saturation` - Set saturation
- `POST /xled/v1/led/rt/frame` - Send real-time frame (HTTP/UDP)

### LED Configuration
- `GET /xled/v1/led/config` - Get LED string configuration
- `POST /xled/v1/led/config` - Set LED string configuration
- `GET /xled/v1/led/layout/full` - Get 3D LED layout
- `POST /xled/v1/led/layout/full` - Upload 3D LED layout
- `DELETE /xled/v1/led/layout/full` - Delete LED layout

### Effects & Movies
- `GET /xled/v1/led/effects` - Get available effects
- `GET /xled/v1/led/effects/current` - Get current effect
- `POST /xled/v1/led/effects/current` - Set current effect
- `GET /xled/v1/led/movie/config` - Get movie configuration
- `POST /xled/v1/led/movie/config` - Set movie configuration
- `GET /xled/v1/led/movies/current` - Get current movie
- `POST /xled/v1/led/movies/current` - Set current movie
- `GET /xled/v1/movies` - List all movies
- `DELETE /xled/v1/movies` - Delete all movies

### Playlist & Scheduling
- `GET /xled/v1/playlist` - Get playlist
- `DELETE /xled/v1/playlist` - Delete playlist
- `GET /xled/v1/timer` - Get timer settings
- `POST /xled/v1/timer` - Set timer for automatic on/off

### Network & Integration
- `GET /xled/v1/network/status` - Get network status
- `GET /xled/v1/mqtt/config` - Get MQTT configuration
- `GET /xled/v1/mic/config` - Get microphone configuration
- `GET /xled/v1/mic/sample` - Get microphone sample

## Usage Examples

### Setting Colors

```csharp
// RGB color
await client.SetRgbColor(255, 0, 0); // Red

// HSV color
await client.SetHsvColor(120, 255, 255); // Green

// RGBW (only for RGBW devices)
await client.SetRgbwColor(255, 255, 255, 128);

// AWW - Amber/Warm/Cold White (only for AWW devices)
await client.SetAwwColor(255, 128, 64);
```

### Controlling Operation Modes

```csharp
// Turn off
await client.SetLedMode(OperationMode.Off);

// Show static color
await client.SetLedMode(OperationMode.Color);

// Play effect
await client.SetLedMode(OperationMode.Effect);

// Play movie
await client.SetLedMode(OperationMode.Movie);

// Real-time mode
await client.SetLedMode(OperationMode.Rt);
```

### Brightness Control

```csharp
// Set brightness to 50%
await client.SetBrightness(50);

// Disable brightness control
await client.SetBrightness(BrightnessMode.Disabled);

// Get current brightness
var brightness = await client.GetBrightness();
Console.WriteLine($"Brightness: {brightness.Brightness}%");
```

### Effects Management

```csharp
// Get available effects
var effects = await client.GetLedEffects();
Console.WriteLine($"Available effects: {effects.EffectsNumber}");

// Set current effect
await client.SetCurrentEffect(0);

// Get current effect
var currentEffect = await client.GetCurrentEffect();
```

### Timer Control

```csharp
// Set timer to turn on at 6 PM and off at 11 PM
// Times are in seconds after midnight
var timeNow = (int)DateTime.Now.TimeOfDay.TotalSeconds;
var timeOn = 18 * 3600; // 6 PM
var timeOff = 23 * 3600; // 11 PM

await client.SetTimer(timeNow, timeOn, timeOff);

// Get timer settings
var timer = await client.GetTimer();
```

### Real-Time LED Control (Advanced)

```csharp
// Prepare frame data (one byte array per LED)
var frame = new byte[client.LedsCount][];
for (int i = 0; i < client.LedsCount; i++)
{
    if (client.LedProfile == LedProfile.Rgbw)
    {
        frame[i] = new byte[] { 255, 0, 0, 0 }; // Red with no white
    }
    else
    {
        frame[i] = new byte[] { 255, 0, 0 }; // Red
    }
}

// Send via HTTP (slower but more reliable)
await client.SendFrame(frame);

// Send via UDP (faster, for animations)
client.MinTimeBetweenFramesMs = 40; // 25 FPS
await client.SendUdpFrame(frame);
```

### LED Layout Management

```csharp
// Get current layout
var layout = await client.GetLedLayout();

// Upload new 3D coordinates
var coordinates = new LedCoordinate[]
{
    new(0.0, 0.0, 0.0),
    new(1.0, 0.0, 0.0),
    new(2.0, 0.0, 0.0),
    // ... more coordinates
};

await client.SetLedLayout(
    aspectXY: 1000,
    aspectXZ: 1000,
    coordinates: coordinates,
    source: LayoutSource.ThreeD,
    synthesized: false,
    uuid: Guid.NewGuid().ToString()
);
```

## Device Properties

The `TwinklyClient` provides access to device information:

```csharp
// LED profile (RGB, RGBW, or AWW)
var profile = client.LedProfile;

// Number of LEDs
var ledCount = client.LedsCount;

// Bytes per LED (3 for RGB/AWW, 4 for RGBW)
var bytesPerLed = client.LedByteCount;

// Detailed device information
var deviceName = client.DeviceDetails.DeviceName;
var productCode = client.DeviceDetails.ProductCode;
var firmwareFamily = client.DeviceDetails.FwFamily;
var macAddress = client.DeviceDetails.Mac;
```

## Enums

### OperationMode
- `Off` - Lights are turned off
- `Color` - Show static color
- `Demo` - Cycle through pre-defined effects
- `Effect` - Play a predefined effect
- `Movie` - Play an uploaded movie
- `Playlist` - Cycle through playlist
- `Rt` - Real-time mode

### LedProfile
- `RGB` - Standard RGB LEDs
- `RGBW` - RGB + White LEDs
- `AWW` - Amber + Warm White + Cold White LEDs

### BrightnessMode / SaturationMode
- `Enabled` - Control is enabled
- `Disabled` - Control is disabled

### BrightnessType / SaturationType
- `Absolute` - Absolute value (0-100)
- `Relative` - Relative adjustment (-100 to +100)

### LayoutSource
- `Linear` - Linear layout
- `TwoD` - 2D layout
- `ThreeD` - 3D layout

## Architecture

The library uses:
- **Attribute-driven enum serialization**: Custom `EnumCaseAttribute` and `JsonValueAttribute` for flexible enum handling
- **Cached reflection**: Enum converters use static caching for optimal performance
- **Polymorphic responses**: Device details responses vary by firmware family (D, F, G)
- **Partial classes**: Client implementation split across multiple files for maintainability
- **Modern C# features**: Records, primary constructors, and field-backed properties

## Contributing

Contributions are welcome! The library is designed to be easily extensible:

1. Add new request DTOs to `DTOs/Requests/Login.cs`
2. Add new response DTOs to `DTOs/Responses/ICodeResponse.cs` or `LoginResponse.cs`
3. Add public methods to `TwinklyClient.Requests.cs`
4. Add XML documentation for IntelliSense support

## License

See LICENSE file for details.

## Credits

Based on the excellent API documentation from the [xled-docs](https://github.com/xled/xled-docs) project.
