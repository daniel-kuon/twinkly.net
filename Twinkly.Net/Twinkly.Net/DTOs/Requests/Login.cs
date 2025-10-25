using System.Text.Json;
using System.Text.Json.Serialization;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net.DTOs.Requests;

/// <summary>
/// Request to authenticate and get an access token.
/// </summary>
/// <param name="Challenge">Random 32 byte string encoded with base64</param>
public record LoginRequest(string Challenge) : Request<LoginResponse>(HttpMethod.Post, "login", false);

/// <summary>
/// Base request class for all Twinkly API requests.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
/// <param name="Method">HTTP method</param>
/// <param name="Path">API endpoint path (without /xled/v1/ prefix)</param>
/// <param name="RequiresAuthentication">Whether this request requires authentication</param>
public abstract record Request<TResponse>([property: JsonIgnore] HttpMethod Method, string Path, [property: JsonIgnore] bool RequiresAuthentication = true)
    where TResponse : ICodeResponse
{
    [JsonIgnore] public string Path
    {
        get => $"/xled/v1/{field}";
    } = Path;
}

/// <summary>
/// Verifies the authentication token retrieved by Login.
/// </summary>
/// <param name="ChallengeResponse">Value returned by login request</param>
public record VerifyRequest(
    [property: JsonPropertyName("challenge-response")]
    string ChallengeResponse) : Request<ICodeResponse>(HttpMethod.Post, "verify");

/// <summary>
/// Invalidates the current access token.
/// </summary>
public record LogoutRequest() : Request<ICodeResponse>(HttpMethod.Post, "logout");

/// <summary>
/// Gets detailed information about the device (gestalt).
/// </summary>
public record DeviceDetailsRequest() : Request<IDeviceDetailsResponse>(HttpMethod.Get, "gestalt");

/// <summary>
/// Sets the device name.
/// </summary>
/// <param name="Name">Desired device name (max 32 characters)</param>
public record SetDeviceNameRequest(string Name) : Request<DeviceNameResponse>(HttpMethod.Post, "device_name");

/// <summary>
/// Gets the current device name.
/// </summary>
public record GetDeviceNameRequest() : Request<DeviceNameResponse>(HttpMethod.Get, "device_name");

/// <summary>
/// Sets the LED operation mode.
/// </summary>
/// <param name="Mode">The operation mode to set</param>
public record SetLedModeRequest(OperationMode Mode) : Request<LedOperationModeResponse>(HttpMethod.Post, "led/mode");

/// <summary>
/// Gets the current LED operation mode.
/// </summary>
public record GetLedModeRequest() : Request<LedOperationModeResponse>(HttpMethod.Get, "led/mode");

/// <summary>
/// Sets LED color using HSV color model.
/// </summary>
/// <param name="Hue">Hue value (0-255)</param>
/// <param name="Saturation">Saturation value (0-255)</param>
/// <param name="Value">Value/brightness (0-255)</param>
public record SetHsvColorRequest(byte Hue, byte Saturation, byte Value)
    : Request<ICodeResponse>(HttpMethod.Post, "led/color");

/// <summary>
/// Sets LED color using RGB color model.
/// </summary>
/// <param name="Red">Red component (0-255)</param>
/// <param name="Green">Green component (0-255)</param>
/// <param name="Blue">Blue component (0-255)</param>
public record SetRgbColorRequest(byte Red, byte Green, byte Blue) : Request<ICodeResponse>(HttpMethod.Post, "led/color");

/// <summary>
/// Sets LED color using RGBW color model (RGB + White).
/// </summary>
/// <param name="Red">Red component (0-255)</param>
/// <param name="Green">Green component (0-255)</param>
/// <param name="Blue">Blue component (0-255)</param>
/// <param name="White">White component (0-255)</param>
public record SetRgbwColorRequest(
    byte Red,
    byte Green,
    byte Blue,
    byte White) : Request<ICodeResponse>(HttpMethod.Post, "led/color");

/// <summary>
/// Sets LED color using AWW (Amber + Warm White + Cold White) color model.
/// </summary>
/// <param name="amber">Amber component (0-255)</param>
/// <param name="warmWhite">Warm white component (0-255)</param>
/// <param name="coldWhite">Cold white component (0-255)</param>
public record SetAwwColorRequest : SetRgbColorRequest
{
    public SetAwwColorRequest(byte amber, byte warmWhite, byte coldWhite) : base(amber, warmWhite, coldWhite)
    {
    }
}

/// <summary>
/// Gets the current LED color.
/// </summary>
public record GetColorRequest() : Request<ColorResponse>(HttpMethod.Get, "led/color");

/// <summary>
/// Sets brightness configuration for the LEDs.
/// </summary>
public record SetBrightnessRequest : Request<ICodeResponse>
{
    public SetBrightnessRequest(int brightness, BrightnessMode mode = BrightnessMode.Enabled,
                                BrightnessType type = BrightnessType.Absolute) : this(brightness, type, mode)
    {
    }

    public SetBrightnessRequest(BrightnessMode mode) : this(null, null, mode)
    {
    }

    private SetBrightnessRequest(int? brightness, BrightnessType? type, BrightnessMode mode) : base(HttpMethod.Post,
        "led/out/brightness")
    {
        Brightness = brightness;
        Type = type;
        Mode = mode;
    }

    public BrightnessMode Mode { get; init; }

    public BrightnessType? Type { get; init; }

    public int? Brightness { get; init; }
}

public enum BrightnessType
{
    /// <summary>
    /// Absolute brightness (0-100)
    /// </summary>
    [JsonValue("A")]
    Absolute,

    /// <summary>
    /// Relative brightness (-100 - 100)
    /// </summary>
    [JsonValue("R")]
    Relative
}

[EnumCase(Case.Lower)]
public enum BrightnessMode
{
    Enabled,
    Disabled
}

public record GetBrightnessRequest() : Request<BrightnessResponse>(HttpMethod.Get, "led/out/brightness");

/// <summary>
/// Gets time when lights should be turned on and time to turn them off.
/// </summary>
public record GetTimerRequest() : Request<TimerResponse>(HttpMethod.Get, "timer");

/// <summary>
/// Sets time when lights should be turned on and time to turn them off.
/// </summary>
/// <param name="TimeNow">Current time in seconds after midnight</param>
/// <param name="TimeOn">Time when to turn lights on in seconds after midnight. -1 if not set</param>
/// <param name="TimeOff">Time when to turn lights off in seconds after midnight. -1 if not set</param>
public record SetTimerRequest(int TimeNow, int TimeOn, int TimeOff) : Request<ICodeResponse>(HttpMethod.Post, "timer");

/// <summary>
/// Echo endpoint - responds with requested message.
/// </summary>
/// <typeparam name="T">The type of message to echo</typeparam>
/// <param name="Message">The message to echo back</param>
public record EchoRequest<T>(T Message) : Request<EchoResponse<T>>(HttpMethod.Post, "echo");

/// <summary>
/// Gets information about LED effects.
/// </summary>
public record GetLedEffectsRequest() : Request<LedEffectsResponse>(HttpMethod.Get, "led/effects");

/// <summary>
/// Gets the id of the effect shown when in effect mode.
/// </summary>
public record GetCurrentEffectRequest() : Request<CurrentEffectResponse>(HttpMethod.Get, "led/effects/current");

/// <summary>
/// Sets which effect to show when in effect mode.
/// </summary>
/// <param name="EffectId">ID of the effect to set</param>
public record SetCurrentEffectRequest(int EffectId) : Request<ICodeResponse>(HttpMethod.Post, "led/effects/current");

/// <summary>
/// Gets LED configuration.
/// </summary>
public record GetLedConfigRequest() : Request<LedConfigResponse>(HttpMethod.Get, "led/config");

/// <summary>
/// Sets LED configuration.
/// </summary>
/// <param name="Strings">Array of LED string configurations</param>
public record SetLedConfigRequest(LedString[] Strings) : Request<ICodeResponse>(HttpMethod.Post, "led/config");

/// <summary>
/// Represents a string of LEDs with a starting ID and length.
/// </summary>
/// <param name="FirstLedId">ID of the first LED in the string</param>
/// <param name="Length">Number of LEDs in the string</param>
public record LedString(int FirstLedId, int Length);

/// <summary>
/// Gets firmware version information.
/// </summary>
public record GetFirmwareVersionRequest() : Request<FirmwareVersionResponse>(HttpMethod.Get, "fw/version");

/// <summary>
/// Gets device status.
/// </summary>
public record GetStatusRequest() : Request<StatusResponse>(HttpMethod.Get, "status");

/// <summary>
/// Gets saturation settings.
/// </summary>
public record GetSaturationRequest() : Request<SaturationResponse>(HttpMethod.Get, "led/out/saturation");

/// <summary>
/// Sets saturation settings.
/// </summary>
/// <param name="Saturation">Saturation value (0-100)</param>
/// <param name="Mode">Saturation mode</param>
/// <param name="Type">Saturation type (Absolute or Relative)</param>
public record SetSaturationRequest : Request<ICodeResponse>
{
    public SetSaturationRequest(int saturation, SaturationMode mode = SaturationMode.Enabled,
                                SaturationType type = SaturationType.Absolute) : this(saturation, type, mode)
    {
    }

    public SetSaturationRequest(SaturationMode mode) : this(null, null, mode)
    {
    }

    private SetSaturationRequest(int? saturation, SaturationType? type, SaturationMode mode) : base(HttpMethod.Post,
        "led/out/saturation")
    {
        Saturation = saturation;
        Type = type;
        Mode = mode;
    }

    public SaturationMode Mode { get; init; }

    public SaturationType? Type { get; init; }

    public int? Saturation { get; init; }
}

/// <summary>
/// Gets network status information.
/// </summary>
public record GetNetworkStatusRequest() : Request<NetworkStatusResponse>(HttpMethod.Get, "network/status");

/// <summary>
/// Gets LED layout (3D coordinates).
/// </summary>
public record GetLedLayoutRequest() : Request<LedLayoutResponse>(HttpMethod.Get, "led/layout/full");

/// <summary>
/// Uploads LED layout (3D coordinates).
/// </summary>
/// <param name="AspectXY">Aspect ratio XY</param>
/// <param name="AspectXZ">Aspect ratio XZ</param>
/// <param name="Coordinates">Array of 3D coordinates</param>
/// <param name="Source">Source type</param>
/// <param name="Synthesized">Whether layout is synthesized</param>
/// <param name="Uuid">Layout UUID</param>
public record SetLedLayoutRequest(
    int AspectXY,
    int AspectXZ,
    LedCoordinate[] Coordinates,
    LayoutSource Source,
    bool Synthesized,
    string Uuid) : Request<ICodeResponse>(HttpMethod.Post, "led/layout/full");

/// <summary>
/// Deletes LED layout.
/// </summary>
public record DeleteLedLayoutRequest() : Request<ICodeResponse>(HttpMethod.Delete, "led/layout/full");

/// <summary>
/// Gets movie configuration.
/// </summary>
public record GetMovieConfigRequest() : Request<MovieConfigResponse>(HttpMethod.Get, "led/movie/config");

/// <summary>
/// Sets movie configuration.
/// </summary>
/// <param name="FrameDelay">Delay between frames in milliseconds</param>
/// <param name="LedsNumber">Number of LEDs</param>
/// <param name="LoopType">Loop type (0 = no loop, 1 = loop)</param>
public record SetMovieConfigRequest(int FrameDelay, int LedsNumber, int LoopType) : Request<ICodeResponse>(HttpMethod.Post, "led/movie/config");

/// <summary>
/// Gets the current movie.
/// </summary>
public record GetCurrentMovieRequest() : Request<CurrentMovieResponse>(HttpMethod.Get, "led/movies/current");

/// <summary>
/// Sets the current movie to play.
/// </summary>
/// <param name="Id">Movie ID</param>
public record SetCurrentMovieRequest(int Id) : Request<ICodeResponse>(HttpMethod.Post, "led/movies/current");

/// <summary>
/// Gets list of movies.
/// </summary>
public record GetMoviesRequest() : Request<MoviesResponse>(HttpMethod.Get, "movies");

/// <summary>
/// Deletes all movies.
/// </summary>
public record DeleteMoviesRequest() : Request<ICodeResponse>(HttpMethod.Delete, "movies");

/// <summary>
/// Gets the current playlist.
/// </summary>
public record GetPlaylistRequest() : Request<PlaylistResponse>(HttpMethod.Get, "playlist");

/// <summary>
/// Deletes the playlist.
/// </summary>
public record DeletePlaylistRequest() : Request<ICodeResponse>(HttpMethod.Delete, "playlist");

/// <summary>
/// Gets MQTT configuration.
/// </summary>
public record GetMqttConfigRequest() : Request<MqttConfigResponse>(HttpMethod.Get, "mqtt/config");

/// <summary>
/// Gets microphone configuration.
/// </summary>
public record GetMicConfigRequest() : Request<MicConfigResponse>(HttpMethod.Get, "mic/config");

/// <summary>
/// Gets microphone sample.
/// </summary>
public record GetMicSampleRequest() : Request<MicSampleResponse>(HttpMethod.Get, "mic/sample");

/// <summary>
/// Gets device summary information.
/// </summary>
public record GetSummaryRequest() : Request<SummaryResponse>(HttpMethod.Get, "summary");

/// <summary>
/// Represents a 3D coordinate for LED layout.
/// </summary>
/// <param name="X">X coordinate</param>
/// <param name="Y">Y coordinate</param>
/// <param name="Z">Z coordinate</param>
public record LedCoordinate(double X, double Y, double Z);

/// <summary>
/// Layout source type.
/// </summary>
[EnumCase(Case.Lower)]
public enum LayoutSource
{
    Linear,
    [JsonValue("2d")]
    TwoD,
    [JsonValue("3d")]
    ThreeD
}

[EnumCase(Case.Lower)]
public enum SaturationMode
{
    Enabled,
    Disabled
}

public enum SaturationType
{
    /// <summary>
    /// Absolute saturation (0-100)
    /// </summary>
    [JsonValue("A")]
    Absolute,

    /// <summary>
    /// Relative saturation (-100 - 100)
    /// </summary>
    [JsonValue("R")]
    Relative
}
