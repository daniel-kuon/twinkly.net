using System.Buffers.Text;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net;

public partial class TwinklyClient
{
    /// <summary>
    /// Sets the device name.
    /// </summary>
    /// <param name="name">New device name (max 32 characters)</param>
    public Task SetDeviceName(string name) => ExecuteRequest(new SetDeviceNameRequest(name));

    /// <summary>
    /// Gets the current device name.
    /// </summary>
    /// <returns>Device name</returns>
    public Task<string> GetDeviceName() => ExecuteRequest(new GetDeviceNameRequest()).ContinueWith(r => r.Result.DeviceName);

    /// <summary>
    /// Gets the current LED operation mode.
    /// </summary>
    /// <returns>Current operation mode</returns>
    public Task<OperationMode> GetLedMode() => ExecuteRequest(new GetLedModeRequest()).ContinueWith(r => r.Result.Mode);

    /// <summary>
    /// Sets LED color using HSV color model.
    /// </summary>
    /// <param name="hue">Hue value (0-255)</param>
    /// <param name="saturation">Saturation value (0-255)</param>
    /// <param name="value">Value/brightness (0-255)</param>
    public Task SetHsvColor(byte hue, byte saturation, byte value) => SendColorRequest(new SetHsvColorRequest(hue, saturation, value));

    /// <summary>
    /// Sets LED color using RGB color model.
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    public Task SetRgbColor(byte red, byte green, byte blue) => SendColorRequest(new SetRgbColorRequest(red, green, blue));

    /// <summary>
    /// Sets LED color using RGBW color model (RGB + White). Only available on RGBW devices.
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="white">White component (0-255)</param>
    /// <exception cref="Exception">Thrown when device doesn't support RGBW</exception>
    public Task SetRgbwColor(byte red, byte green, byte blue, byte white)
    {
        if(LedProfile != LedProfile.Rgbw)
            throw new Exception($"This device does not support RGBW. Current profile is {LedProfile}");

        return SendColorRequest(new SetRgbwColorRequest(red, green, blue, white));
    }

    /// <summary>
    /// Sets LED color using AWW (Amber + Warm White + Cold White) color model. Only available on AWW devices.
    /// </summary>
    /// <param name="amber">Amber component (0-255)</param>
    /// <param name="warmWhite">Warm white component (0-255)</param>
    /// <param name="coldWhite">Cold white component (0-255)</param>
    /// <exception cref="Exception">Thrown when device doesn't support AWW</exception>
    public Task SetAwwColor(byte amber, byte warmWhite, byte coldWhite)
    {
        if(LedProfile != LedProfile.Aww)
            throw new Exception($"This device does not support AWW. Current profile is {LedProfile}");

        return SendColorRequest(new SetAwwColorRequest(amber, warmWhite, coldWhite));
    }

    /// <summary>
    /// Gets the current LED color in both HSV and RGB formats.
    /// </summary>
    /// <returns>Color information</returns>
    public Task<ColorResponse> GetColor() => ExecuteRequest(new GetColorRequest());

    /// <summary>
    /// Sets brightness level.
    /// </summary>
    /// <param name="brightness">Brightness value (0-100)</param>
    public Task SetBrightness(int brightness) =>  SendBrightnessRequest(new SetBrightnessRequest(brightness));

    /// <summary>
    /// Sets brightness mode.
    /// </summary>
    /// <param name="mode">Brightness mode (Enabled/Disabled)</param>
    public Task SetBrightness(BrightnessMode mode) =>  SendBrightnessRequest(new SetBrightnessRequest(mode));

    /// <summary>
    /// Sets brightness level and mode.
    /// </summary>
    /// <param name="brightness">Brightness value (0-100)</param>
    /// <param name="mode">Brightness mode (Enabled/Disabled)</param>
    public  Task SetBrightness(int brightness, BrightnessMode mode) => SendBrightnessRequest(new SetBrightnessRequest(brightness, mode));

    /// <summary>
    /// Sets brightness level, mode, and type.
    /// </summary>
    /// <param name="brightness">Brightness value</param>
    /// <param name="mode">Brightness mode (Enabled/Disabled)</param>
    /// <param name="type">Brightness type (Absolute/Relative)</param>
    public Task SetBrightness(int brightness, BrightnessMode mode, BrightnessType type) => SendBrightnessRequest(new SetBrightnessRequest(brightness, mode, type));

    /// <summary>
    /// Gets the current brightness configuration.
    /// </summary>
    /// <returns>Brightness information</returns>
    public Task<BrightnessResponse> GetBrightness() => ExecuteRequest(new GetBrightnessRequest());

    /// <summary>
    /// Gets timer configuration - when lights should be turned on and off.
    /// </summary>
    /// <returns>Timer configuration including current time, on time, and off time (in seconds after midnight)</returns>
    public Task<TimerResponse> GetTimer() => ExecuteRequest(new GetTimerRequest());

    /// <summary>
    /// Sets timer configuration - when lights should be turned on and off.
    /// </summary>
    /// <param name="timeNow">Current time in seconds after midnight</param>
    /// <param name="timeOn">Time when to turn lights on in seconds after midnight. -1 if not set</param>
    /// <param name="timeOff">Time when to turn lights off in seconds after midnight. -1 if not set</param>
    public Task SetTimer(int timeNow, int timeOn, int timeOff) => ExecuteRequest(new SetTimerRequest(timeNow, timeOn, timeOff));

    /// <summary>
    /// Echo endpoint - responds with the requested message.
    /// </summary>
    /// <param name="message">The message to echo back</param>
    /// <returns>The echoed JSON object</returns>
    public Task<object> Echo(object message) => ExecuteRequest(new EchoRequest(message)).ContinueWith(r => r.Result.Json);

    /// <summary>
    /// Gets information about available LED effects.
    /// </summary>
    /// <returns>Information about effects including count and UUIDs</returns>
    public Task<LedEffectsResponse> GetLedEffects() => ExecuteRequest(new GetLedEffectsRequest());

    /// <summary>
    /// Gets the ID of the effect currently shown when in effect mode.
    /// </summary>
    /// <returns>Current effect information</returns>
    public Task<CurrentEffectResponse> GetCurrentEffect() => ExecuteRequest(new GetCurrentEffectRequest());

    /// <summary>
    /// Sets which effect to show when in effect mode.
    /// </summary>
    /// <param name="effectId">ID of the effect to set</param>
    public Task SetCurrentEffect(int effectId) => ExecuteRequest(new SetCurrentEffectRequest(effectId));

    /// <summary>
    /// Gets LED string configuration.
    /// </summary>
    /// <returns>LED configuration including string information</returns>
    public Task<LedConfigResponse> GetLedConfig() => ExecuteRequest(new GetLedConfigRequest());

    /// <summary>
    /// Sets LED string configuration.
    /// </summary>
    /// <param name="strings">Array of LED string configurations</param>
    public Task SetLedConfig(params LedString[] strings) => ExecuteRequest(new SetLedConfigRequest(strings));

    /// <summary>
    /// Gets firmware version information.
    /// </summary>
    /// <returns>Firmware version string</returns>
    public Task<string> GetFirmwareVersion() => ExecuteRequest(new GetFirmwareVersionRequest()).ContinueWith(r => r.Result.Version);

    /// <summary>
    /// Gets device status.
    /// </summary>
    public Task GetStatus() => ExecuteRequest(new GetStatusRequest());

    /// <summary>
    /// Gets saturation settings.
    /// </summary>
    /// <returns>Saturation configuration</returns>
    public Task<SaturationResponse> GetSaturation() => ExecuteRequest(new GetSaturationRequest());

    /// <summary>
    /// Sets saturation value.
    /// </summary>
    /// <param name="saturation">Saturation value (0-100)</param>
    public Task SetSaturation(int saturation) => ExecuteRequest(new SetSaturationRequest(saturation));

    /// <summary>
    /// Sets saturation mode.
    /// </summary>
    /// <param name="mode">Saturation mode (Enabled/Disabled)</param>
    public Task SetSaturation(SaturationMode mode) => ExecuteRequest(new SetSaturationRequest(mode));

    /// <summary>
    /// Sets saturation with all parameters.
    /// </summary>
    /// <param name="saturation">Saturation value</param>
    /// <param name="mode">Saturation mode (Enabled/Disabled)</param>
    public Task SetSaturation(int saturation, SaturationMode mode) => ExecuteRequest(new SetSaturationRequest(saturation, mode));

    /// <summary>
    /// Sets saturation with all parameters including type.
    /// </summary>
    /// <param name="saturation">Saturation value</param>
    /// <param name="mode">Saturation mode (Enabled/Disabled)</param>
    /// <param name="type">Saturation type (Absolute/Relative)</param>
    public Task SetSaturation(int saturation, SaturationMode mode, SaturationType type) => ExecuteRequest(new SetSaturationRequest(saturation, mode, type));

    /// <summary>
    /// Gets network status information.
    /// </summary>
    /// <returns>Network status including mode, station, and AP configuration</returns>
    public Task<NetworkStatusResponse> GetNetworkStatus() => ExecuteRequest(new GetNetworkStatusRequest());

    /// <summary>
    /// Gets LED layout (3D coordinates).
    /// </summary>
    /// <returns>LED layout information</returns>
    public Task<LedLayoutResponse> GetLedLayout() => ExecuteRequest(new GetLedLayoutRequest());

    /// <summary>
    /// Uploads LED layout (3D coordinates).
    /// </summary>
    /// <param name="aspectXY">Aspect ratio XY</param>
    /// <param name="aspectXZ">Aspect ratio XZ</param>
    /// <param name="coordinates">Array of 3D coordinates</param>
    /// <param name="source">Layout source type</param>
    /// <param name="synthesized">Whether layout is synthesized</param>
    /// <param name="uuid">Layout UUID</param>
    public Task SetLedLayout(int aspectXY, int aspectXZ, LedCoordinate[] coordinates, LayoutSource source, bool synthesized, string uuid) 
        => ExecuteRequest(new SetLedLayoutRequest(aspectXY, aspectXZ, coordinates, source, synthesized, uuid));

    /// <summary>
    /// Deletes LED layout.
    /// </summary>
    public Task DeleteLedLayout() => ExecuteRequest(new DeleteLedLayoutRequest());

    /// <summary>
    /// Gets movie configuration.
    /// </summary>
    /// <returns>Movie configuration</returns>
    public Task<MovieConfigResponse> GetMovieConfig() => ExecuteRequest(new GetMovieConfigRequest());

    /// <summary>
    /// Sets movie configuration.
    /// </summary>
    /// <param name="frameDelay">Delay between frames in milliseconds</param>
    /// <param name="ledsNumber">Number of LEDs</param>
    /// <param name="loopType">Loop type (0 = no loop, 1 = loop)</param>
    public Task SetMovieConfig(int frameDelay, int ledsNumber, int loopType) => ExecuteRequest(new SetMovieConfigRequest(frameDelay, ledsNumber, loopType));

    /// <summary>
    /// Gets the current movie.
    /// </summary>
    /// <returns>Current movie information</returns>
    public Task<CurrentMovieResponse> GetCurrentMovie() => ExecuteRequest(new GetCurrentMovieRequest());

    /// <summary>
    /// Sets the current movie to play.
    /// </summary>
    /// <param name="id">Movie ID</param>
    public Task SetCurrentMovie(int id) => ExecuteRequest(new SetCurrentMovieRequest(id));

    /// <summary>
    /// Gets list of all movies.
    /// </summary>
    /// <returns>Movies information including available capacity</returns>
    public Task<MoviesResponse> GetMovies() => ExecuteRequest(new GetMoviesRequest());

    /// <summary>
    /// Deletes all movies.
    /// </summary>
    public Task DeleteMovies() => ExecuteRequest(new DeleteMoviesRequest());

    /// <summary>
    /// Gets the current playlist.
    /// </summary>
    /// <returns>Playlist information</returns>
    public Task<PlaylistResponse> GetPlaylist() => ExecuteRequest(new GetPlaylistRequest());

    /// <summary>
    /// Deletes the playlist.
    /// </summary>
    public Task DeletePlaylist() => ExecuteRequest(new DeletePlaylistRequest());

    /// <summary>
    /// Gets MQTT configuration.
    /// </summary>
    /// <returns>MQTT configuration</returns>
    public Task<MqttConfigResponse> GetMqttConfig() => ExecuteRequest(new GetMqttConfigRequest());

    /// <summary>
    /// Gets microphone configuration.
    /// </summary>
    public Task GetMicConfig() => ExecuteRequest(new GetMicConfigRequest());

    /// <summary>
    /// Gets microphone sample.
    /// </summary>
    public Task GetMicSample() => ExecuteRequest(new GetMicSampleRequest());

    /// <summary>
    /// Gets device summary information.
    /// </summary>
    public Task GetSummary() => ExecuteRequest(new GetSummaryRequest());

}

