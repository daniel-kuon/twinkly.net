using System.Net.WebSockets;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;
using System.Text.Json.Serialization;

namespace Twinkly.Net.DTOs.Responses;

/// <summary>
/// Base interface for all Twinkly API responses.
/// </summary>
public interface ICodeResponse
{
    public ResponseCode Code { get; }
    public bool IsSuccess => Code is ResponseCode.Ok or ResponseCode.Ok1107 or ResponseCode.Ok1108;
}

/// <summary>
/// Response containing LED operation mode.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Mode">Current operation mode</param>
public record LedOperationModeResponse(ResponseCode Code, OperationMode Mode) : ICodeResponse;

/// <summary>
/// Response containing LED color information in both HSV and RGB formats.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Hue">Hue value</param>
/// <param name="Saturation">Saturation value</param>
/// <param name="Value">Value/brightness</param>
/// <param name="Red">Red component</param>
/// <param name="Green">Green component</param>
/// <param name="Blue">Blue component</param>
public record ColorResponse(
    ResponseCode Code,
    int Hue,
    int Saturation,
    int Value,
    int Red,
    int Green,
    int Blue) : ICodeResponse;

/// <summary>
/// Response containing brightness configuration.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Brightness">Brightness value</param>
/// <param name="Mode">Brightness mode</param>
/// <param name="Type">Brightness type</param>
public record BrightnessResponse(
    ResponseCode Code,
    int Brightness,
    BrightnessMode Mode,
    BrightnessType Type
) : ICodeResponse;

/// <summary>
/// Response from timer endpoints containing timer configuration.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="TimeNow">Current time in seconds after midnight</param>
/// <param name="TimeOn">Time when to turn lights on in seconds after midnight. -1 if not set</param>
/// <param name="TimeOff">Time when to turn lights off in seconds after midnight. -1 if not set</param>
public record TimerResponse(
    ResponseCode Code,
    int TimeNow,
    int TimeOn,
    int TimeOff
) : ICodeResponse;

/// <summary>
/// Response from echo endpoint.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Json">The echoed JSON object</param>
public record EchoResponse(
    ResponseCode Code,
    object Json
) : ICodeResponse;

/// <summary>
/// Response containing LED effects information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="EffectsNumber">Number of available effects</param>
/// <param name="UniqueIds">Array of effect UUIDs (since firmware 2.5.6)</param>
public record LedEffectsResponse(
    ResponseCode Code,
    int EffectsNumber,
    string[]? UniqueIds = null
) : ICodeResponse;

/// <summary>
/// Response containing current effect information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="EffectId">ID of the current effect</param>
/// <param name="UniqueId">UUID of the current effect (since firmware 2.5.6)</param>
public record CurrentEffectResponse(
    ResponseCode Code,
    int EffectId,
    string? UniqueId = null
) : ICodeResponse;

/// <summary>
/// Response containing LED configuration.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Strings">Array of LED string configurations</param>
public record LedConfigResponse(
    ResponseCode Code,
    LedString[] Strings
) : ICodeResponse;

/// <summary>
/// Response containing firmware version information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Version">Firmware version string</param>
public record FirmwareVersionResponse(
    ResponseCode Code,
    string Version
) : ICodeResponse;

/// <summary>
/// Response containing device status information.
/// </summary>
/// <param name="Code">Response code</param>
public record StatusResponse(
    ResponseCode Code
) : ICodeResponse;

/// <summary>
/// Response containing saturation information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Saturation">Saturation value</param>
/// <param name="Mode">Saturation mode</param>
/// <param name="Type">Saturation type</param>
public record SaturationResponse(
    ResponseCode Code,
    int Saturation,
    SaturationMode Mode,
    SaturationType Type
) : ICodeResponse;

/// <summary>
/// Response containing network status information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Mode">Network mode (e.g., 1 for station mode, 2 for AP mode)</param>
/// <param name="Station">Station configuration</param>
/// <param name="Ap">Access point configuration</param>
public record NetworkStatusResponse(
    ResponseCode Code,
    int Mode,
    NetworkStation? Station = null,
    NetworkAp? Ap = null
) : ICodeResponse;

/// <summary>
/// Network station configuration.
/// </summary>
/// <param name="Ssid">WiFi SSID</param>
/// <param name="Ip">IP address</param>
/// <param name="Gateway">Gateway address</param>
/// <param name="Mask">Network mask</param>
public record NetworkStation(
    string Ssid,
    string Ip,
    string Gateway,
    string Mask
);

/// <summary>
/// Network access point configuration.
/// </summary>
/// <param name="Ssid">Access point SSID</param>
/// <param name="Channel">WiFi channel</param>
/// <param name="Ip">IP address</param>
/// <param name="Enc">Encryption type</param>
/// <param name="SsidHidden">Whether SSID is hidden</param>
/// <param name="MaxConnections">Maximum number of connections</param>
/// <param name="PasswordChanged">Whether password has been changed</param>
public record NetworkAp(
    string Ssid,
    int Channel,
    string Ip,
    int Enc,
    int SsidHidden,
    int MaxConnections,
    int PasswordChanged
);

/// <summary>
/// Response containing LED layout information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="AspectXY">Aspect ratio XY</param>
/// <param name="AspectXZ">Aspect ratio XZ</param>
/// <param name="Coordinates">Array of 3D coordinates</param>
/// <param name="Source">Layout source type</param>
/// <param name="Synthesized">Whether layout is synthesized</param>
/// <param name="Uuid">Layout UUID</param>
public record LedLayoutResponse(
    ResponseCode Code,
    int AspectXY,
    int AspectXZ,
    LedCoordinate[] Coordinates,
    LayoutSource Source,
    bool Synthesized,
    string Uuid
) : ICodeResponse;

/// <summary>
/// Response containing movie configuration.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="FrameDelay">Delay between frames</param>
/// <param name="LedsNumber">Number of LEDs</param>
/// <param name="LoopType">Loop type</param>
public record MovieConfigResponse(
    ResponseCode Code,
    int FrameDelay,
    int LedsNumber,
    int LoopType
) : ICodeResponse;

/// <summary>
/// Response containing current movie information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Id">Movie ID</param>
/// <param name="Name">Movie name</param>
/// <param name="UniqueId">Movie UUID</param>
/// <param name="DescriptorType">Descriptor type</param>
public record CurrentMovieResponse(
    ResponseCode Code,
    int Id,
    string? Name = null,
    string? UniqueId = null,
    string? DescriptorType = null
) : ICodeResponse;

/// <summary>
/// Response containing list of movies.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Movies">Array of movies</param>
/// <param name="AvailableFrames">Available frame capacity</param>
/// <param name="MaxCapacity">Maximum capacity</param>
public record MoviesResponse(
    ResponseCode Code,
    MovieInfo[] Movies,
    int AvailableFrames,
    int MaxCapacity
) : ICodeResponse;

/// <summary>
/// Information about a movie.
/// </summary>
/// <param name="Id">Movie ID</param>
/// <param name="Name">Movie name</param>
/// <param name="UniqueId">Movie UUID</param>
/// <param name="DescriptorType">Descriptor type</param>
/// <param name="LedsNumber">Number of LEDs</param>
/// <param name="FramesNumber">Number of frames</param>
/// <param name="Fps">Frames per second</param>
public record MovieInfo(
    int Id,
    string Name,
    string UniqueId,
    string DescriptorType,
    int LedsNumber,
    int FramesNumber,
    int Fps
);

/// <summary>
/// Response containing playlist information.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="Entries">Array of playlist entries</param>
public record PlaylistResponse(
    ResponseCode Code,
    PlaylistEntry[] Entries
) : ICodeResponse;

/// <summary>
/// Playlist entry information.
/// </summary>
/// <param name="Id">Movie ID</param>
/// <param name="UniqueId">Movie UUID</param>
/// <param name="Name">Movie name</param>
/// <param name="Duration">Duration in milliseconds</param>
public record PlaylistEntry(
    int Id,
    string UniqueId,
    string Name,
    int Duration
);

/// <summary>
/// Response containing MQTT configuration.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="BrokerHost">MQTT broker host</param>
/// <param name="BrokerPort">MQTT broker port</param>
/// <param name="ClientId">MQTT client ID</param>
/// <param name="User">MQTT username</param>
/// <param name="KeepAliveInterval">Keep alive interval</param>
public record MqttConfigResponse(
    ResponseCode Code,
    string BrokerHost,
    int BrokerPort,
    string ClientId,
    string User,
    int KeepAliveInterval
) : ICodeResponse;

/// <summary>
/// Response containing microphone configuration.
/// </summary>
/// <param name="Code">Response code</param>
public record MicConfigResponse(
    ResponseCode Code
) : ICodeResponse;

/// <summary>
/// Response containing microphone sample data.
/// </summary>
/// <param name="Code">Response code</param>
public record MicSampleResponse(
    ResponseCode Code
) : ICodeResponse;

/// <summary>
/// Response containing device summary information.
/// </summary>
/// <param name="Code">Response code</param>
public record SummaryResponse(
    ResponseCode Code
) : ICodeResponse;

