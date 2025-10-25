# Twinkly.Net API Reference

Complete reference for all API endpoints and methods available in the Twinkly.Net library.

## Table of Contents

- [TwinklyClient](#twinklyclient)
  - [Properties](#properties)
  - [Static Methods](#static-methods)
  - [Authentication Methods](#authentication-methods)
  - [Device Management](#device-management)
  - [LED Control](#led-control)
  - [Color Control](#color-control)
  - [Brightness & Saturation](#brightness--saturation)
  - [Effects & Movies](#effects--movies)
  - [LED Configuration](#led-configuration)
  - [LED Layout](#led-layout)
  - [Timer & Scheduling](#timer--scheduling)
  - [Network & Integration](#network--integration)
  - [Real-Time Control](#real-time-control)

## TwinklyClient

### Properties

#### `LedProfile LedProfile { get; }`
Gets the LED profile of the device (RGB, RGBW, or AWW).

#### `LedByteMode LedByteMode { get; }`
Gets the LED byte mode (3 bytes for RGB/AWW, 4 bytes for RGBW).

#### `byte LedByteCount { get; }`
Gets the number of bytes per LED.

#### `int LedsCount { get; }`
Gets the total number of LEDs in the device.

#### `int MinTimeBetweenFramesMs { get; set; }`
Gets or sets the minimum time between frames in milliseconds for UDP frame sending. Set this to avoid flickering or dropped frames.

#### `IDeviceDetailsResponse DeviceDetails { get; }`
Gets the detailed device information retrieved during client creation. Contains properties like:
- `ProductName` - Product name (e.g., "Twinkly")
- `DeviceName` - User-assigned device name
- `ProductCode` - Product code (e.g., "TWS250STP")
- `HardwareVersion` - Hardware version
- `FwFamily` - Firmware family ('D', 'F', or 'G')
- `Mac` - MAC address
- `Uuid` - Device UUID
- `NumberOfLed` - Number of LEDs
- `LedProfile` - LED profile (RGB, RGBW, AWW)
- `FrameRate` - Frame rate
- `MovieCapacity` - Movie storage capacity

### Static Methods

#### `static async Task<TwinklyClient> Create(IPAddress ipAddress, ILogger<TwinklyClient> logger, HttpClient httpClient)`
Creates and initializes a new TwinklyClient instance. Automatically authenticates and retrieves device details.

**Parameters:**
- `ipAddress` - IP address of the Twinkly device
- `logger` - Logger instance for logging operations
- `httpClient` - HTTP client for making requests

**Returns:** Initialized TwinklyClient

**Example:**
```csharp
var client = await TwinklyClient.Create(
    IPAddress.Parse("192.168.1.100"),
    logger,
    httpClient
);
```

### Authentication Methods

Authentication is handled automatically by the client. These methods are used internally:
- Login - Sends challenge and receives authentication token
- Verify - Verifies the received token
- Token refresh - Automatically handled on 401 responses

### Device Management

#### `Task SetDeviceName(string name)`
Sets the device name.

**Parameters:**
- `name` - New device name (max 32 characters)

#### `Task<string> GetDeviceName()`
Gets the current device name.

**Returns:** Device name

#### `Task<string> GetFirmwareVersion()`
Gets firmware version information.

**Returns:** Firmware version string

#### `Task GetStatus()`
Gets device status.

#### `Task GetSummary()`
Gets device summary information.

#### `Task<T> Echo<T>(T message)`
Echo endpoint - responds with the requested message. Useful for testing connectivity.

**Type Parameters:**
- `T` - The type of message to echo

**Parameters:**
- `message` - The message to echo back

**Returns:** The echoed message of the same type as the input

**Example:**
```csharp
// Echo a string
var echoedString = await client.Echo("Hello!");

// Echo a complex object
var testObj = new { message = "test", value = 42 };
var echoedObj = await client.Echo(testObj);
```

### LED Control

#### `async Task SetLedMode(OperationMode mode)`
Sets the LED operation mode.

**Parameters:**
- `mode` - The operation mode to set:
  - `OperationMode.Off` - Lights are turned off
  - `OperationMode.Color` - Show static color
  - `OperationMode.Demo` - Cycle through pre-defined effects
  - `OperationMode.Effect` - Play a predefined effect
  - `OperationMode.Movie` - Play an uploaded movie
  - `OperationMode.Playlist` - Cycle through playlist
  - `OperationMode.Rt` - Real-time mode

#### `Task<OperationMode> GetLedMode()`
Gets the current LED operation mode.

**Returns:** Current operation mode

### Color Control

#### `Task SetHsvColor(byte hue, byte saturation, byte value)`
Sets LED color using HSV color model.

**Parameters:**
- `hue` - Hue value (0-255)
- `saturation` - Saturation value (0-255)
- `value` - Value/brightness (0-255)

#### `Task SetRgbColor(byte red, byte green, byte blue)`
Sets LED color using RGB color model.

**Parameters:**
- `red` - Red component (0-255)
- `green` - Green component (0-255)
- `blue` - Blue component (0-255)

#### `Task SetRgbwColor(byte red, byte green, byte blue, byte white)`
Sets LED color using RGBW color model (RGB + White). Only available on RGBW devices.

**Parameters:**
- `red` - Red component (0-255)
- `green` - Green component (0-255)
- `blue` - Blue component (0-255)
- `white` - White component (0-255)

**Throws:** Exception when device doesn't support RGBW

#### `Task SetAwwColor(byte amber, byte warmWhite, byte coldWhite)`
Sets LED color using AWW (Amber + Warm White + Cold White) color model. Only available on AWW devices.

**Parameters:**
- `amber` - Amber component (0-255)
- `warmWhite` - Warm white component (0-255)
- `coldWhite` - Cold white component (0-255)

**Throws:** Exception when device doesn't support AWW

#### `Task<ColorResponse> GetColor()`
Gets the current LED color in both HSV and RGB formats.

**Returns:** Color information with properties:
- `Hue` - Hue value
- `Saturation` - Saturation value
- `Value` - Value/brightness
- `Red` - Red component
- `Green` - Green component
- `Blue` - Blue component

### Brightness & Saturation

#### `Task SetBrightness(int brightness)`
Sets brightness level.

**Parameters:**
- `brightness` - Brightness value (0-100)

#### `Task SetBrightness(BrightnessMode mode)`
Sets brightness mode.

**Parameters:**
- `mode` - Brightness mode (Enabled/Disabled)

#### `Task SetBrightness(int brightness, BrightnessMode mode)`
Sets brightness level and mode.

**Parameters:**
- `brightness` - Brightness value (0-100)
- `mode` - Brightness mode (Enabled/Disabled)

#### `Task SetBrightness(int brightness, BrightnessMode mode, BrightnessType type)`
Sets brightness level, mode, and type.

**Parameters:**
- `brightness` - Brightness value
- `mode` - Brightness mode (Enabled/Disabled)
- `type` - Brightness type (Absolute/Relative)

#### `Task<BrightnessResponse> GetBrightness()`
Gets the current brightness configuration.

**Returns:** Brightness information including value, mode, and type

#### `Task SetSaturation(int saturation)`
Sets saturation value.

**Parameters:**
- `saturation` - Saturation value (0-100)

#### `Task SetSaturation(SaturationMode mode)`
Sets saturation mode.

**Parameters:**
- `mode` - Saturation mode (Enabled/Disabled)

#### `Task SetSaturation(int saturation, SaturationMode mode)`
Sets saturation with value and mode.

**Parameters:**
- `saturation` - Saturation value (0-100)
- `mode` - Saturation mode (Enabled/Disabled)

#### `Task SetSaturation(int saturation, SaturationMode mode, SaturationType type)`
Sets saturation with all parameters.

**Parameters:**
- `saturation` - Saturation value
- `mode` - Saturation mode (Enabled/Disabled)
- `type` - Saturation type (Absolute/Relative)

#### `Task<SaturationResponse> GetSaturation()`
Gets saturation settings.

**Returns:** Saturation configuration

### Effects & Movies

#### `Task<LedEffectsResponse> GetLedEffects()`
Gets information about available LED effects.

**Returns:** Information about effects including:
- `EffectsNumber` - Number of available effects
- `UniqueIds` - Array of effect UUIDs (firmware 2.5.6+)

#### `Task<CurrentEffectResponse> GetCurrentEffect()`
Gets the ID of the effect currently shown when in effect mode.

**Returns:** Current effect information with:
- `EffectId` - ID of the current effect
- `UniqueId` - UUID of the current effect (firmware 2.5.6+)

#### `Task SetCurrentEffect(int effectId)`
Sets which effect to show when in effect mode.

**Parameters:**
- `effectId` - ID of the effect to set

#### `Task<MovieConfigResponse> GetMovieConfig()`
Gets movie configuration.

**Returns:** Movie configuration including:
- `FrameDelay` - Delay between frames
- `LedsNumber` - Number of LEDs
- `LoopType` - Loop type

#### `Task SetMovieConfig(int frameDelay, int ledsNumber, int loopType)`
Sets movie configuration.

**Parameters:**
- `frameDelay` - Delay between frames in milliseconds
- `ledsNumber` - Number of LEDs
- `loopType` - Loop type (0 = no loop, 1 = loop)

#### `Task<CurrentMovieResponse> GetCurrentMovie()`
Gets the current movie.

**Returns:** Current movie information

#### `Task SetCurrentMovie(int id)`
Sets the current movie to play.

**Parameters:**
- `id` - Movie ID

#### `Task<MoviesResponse> GetMovies()`
Gets list of all movies.

**Returns:** Movies information including:
- `Movies` - Array of movie information
- `AvailableFrames` - Available frame capacity
- `MaxCapacity` - Maximum capacity

#### `Task DeleteMovies()`
Deletes all movies.

### LED Configuration

#### `Task<LedConfigResponse> GetLedConfig()`
Gets LED string configuration.

**Returns:** LED configuration including:
- `Strings` - Array of LED string configurations with:
  - `FirstLedId` - ID of first LED in string
  - `Length` - Number of LEDs in string

#### `Task SetLedConfig(params LedString[] strings)`
Sets LED string configuration.

**Parameters:**
- `strings` - Array of LED string configurations

**Example:**
```csharp
await client.SetLedConfig(
    new LedString(FirstLedId: 0, Length: 100),
    new LedString(FirstLedId: 100, Length: 50)
);
```

### LED Layout

#### `Task<LedLayoutResponse> GetLedLayout()`
Gets LED layout (3D coordinates).

**Returns:** LED layout information including:
- `AspectXY` - Aspect ratio XY
- `AspectXZ` - Aspect ratio XZ
- `Coordinates` - Array of 3D coordinates
- `Source` - Layout source type (Linear, 2D, 3D)
- `Synthesized` - Whether layout is synthesized
- `Uuid` - Layout UUID

#### `Task SetLedLayout(int aspectXY, int aspectXZ, LedCoordinate[] coordinates, LayoutSource source, bool synthesized, string uuid)`
Uploads LED layout (3D coordinates).

**Parameters:**
- `aspectXY` - Aspect ratio XY
- `aspectXZ` - Aspect ratio XZ
- `coordinates` - Array of 3D coordinates (X, Y, Z)
- `source` - Layout source type
- `synthesized` - Whether layout is synthesized
- `uuid` - Layout UUID

#### `Task DeleteLedLayout()`
Deletes LED layout.

### Timer & Scheduling

#### `Task<TimerResponse> GetTimer()`
Gets timer configuration - when lights should be turned on and off.

**Returns:** Timer configuration including:
- `TimeNow` - Current time in seconds after midnight
- `TimeOn` - Time when to turn lights on (-1 if not set)
- `TimeOff` - Time when to turn lights off (-1 if not set)

#### `Task SetTimer(int timeNow, int timeOn, int timeOff)`
Sets timer configuration - when lights should be turned on and off.

**Parameters:**
- `timeNow` - Current time in seconds after midnight
- `timeOn` - Time when to turn lights on in seconds after midnight (-1 to disable)
- `timeOff` - Time when to turn lights off in seconds after midnight (-1 to disable)

**Example:**
```csharp
// Turn on at 6 PM, off at 11 PM
var now = (int)DateTime.Now.TimeOfDay.TotalSeconds;
await client.SetTimer(now, 18 * 3600, 23 * 3600);
```

### Network & Integration

#### `Task<NetworkStatusResponse> GetNetworkStatus()`
Gets network status information.

**Returns:** Network status including:
- `Mode` - Network mode (1 for station, 2 for AP)
- `Station` - Station configuration (if connected)
- `Ap` - Access point configuration

#### `Task<PlaylistResponse> GetPlaylist()`
Gets the current playlist.

**Returns:** Playlist information with array of entries

#### `Task DeletePlaylist()`
Deletes the playlist.

#### `Task<MqttConfigResponse> GetMqttConfig()`
Gets MQTT configuration.

**Returns:** MQTT configuration including:
- `BrokerHost` - MQTT broker host
- `BrokerPort` - MQTT broker port
- `ClientId` - MQTT client ID
- `User` - MQTT username
- `KeepAliveInterval` - Keep alive interval

#### `Task GetMicConfig()`
Gets microphone configuration.

#### `Task GetMicSample()`
Gets microphone sample.

### Real-Time Control

#### `Task<ICodeResponse> SendFrame(byte[][] bytes)`
Sends a frame to the Twinkly device in real-time mode using HTTP protocol. Less efficient than UDP but more reliable.

**Parameters:**
- `bytes` - Jagged array where each element represents the color data for a single LED. Each LED's data should be in the format defined by the device's LED profile (4 bytes per LED for RGBW, 3 bytes per LED for RGB or AWW)

**Returns:** Response indicating success

**Example:**
```csharp
var frame = new byte[client.LedsCount][];
for (int i = 0; i < client.LedsCount; i++)
{
    frame[i] = new byte[] { 255, 0, 0 }; // Red (RGB)
}
await client.SendFrame(frame);
```

#### `Task SendUdpFrame(byte[][] bytes)`
Sends a frame to the Twinkly device in real-time mode using UDP protocol. More efficient than HTTP and allows for higher frame rates.

**Parameters:**
- `bytes` - Jagged array where each element represents the color data for a single LED

**Remarks:** This API can send data at a higher rate than the device can process, which will lead to flickering or dropped frames. Set the `MinTimeBetweenFramesMs` property to ensure adequate timing between frames.

**Example:**
```csharp
client.MinTimeBetweenFramesMs = 40; // 25 FPS max
var frame = new byte[client.LedsCount][];
// ... populate frame
await client.SendUdpFrame(frame);
```

## Response Codes

All responses include a `Code` property with one of the following values:

- `1000` - Ok
- `1001` - Error
- `1101` - Invalid argument value
- `1102` - Error
- `1103` - Error - value too long or missing required object key
- `1104` - Error - malformed JSON on input
- `1105` - Invalid argument key
- `1107` - Ok (variant)
- `1108` - Ok (variant)
- `1205` - Firmware upgrade error - SHA1SUM does not match

## Error Handling

The client automatically handles:
- Authentication token expiration (401 responses trigger re-authentication)
- HTTP error responses (throws exception with status code)
- Invalid response codes (throws exception if response code is not success)

All methods may throw:
- `Exception` - For general errors including network issues, invalid responses, or unsupported operations
- `ArgumentOutOfRangeException` - For invalid parameter values (e.g., wrong number of LEDs in frame data)
- `InvalidOperationException` - When operations are attempted before client is fully initialized
