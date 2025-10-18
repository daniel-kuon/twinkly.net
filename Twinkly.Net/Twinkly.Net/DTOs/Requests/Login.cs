using System.Text.Json;
using System.Text.Json.Serialization;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net.DTOs.Requests;

public record LoginRequest(string Challenge) : Request<LoginResponse>(HttpMethod.Post, "login", false);

public abstract record Request<TResponse>([property: JsonIgnore] HttpMethod Method, string Path, [property: JsonIgnore] bool RequiresAuthentication = true)
    where TResponse : ICodeResponse
{
    [JsonIgnore] public string Path
    {
        get => $"/xled/v1/{field}";
    } = Path;
}
public record VerifyRequest(
    [property: JsonPropertyName("challenge-response")]
    string ChallengeResponse) : Request<ICodeResponse>(HttpMethod.Post, "verify");

public record LogoutRequest() : Request<ICodeResponse>(HttpMethod.Post, "logout");

public record DeviceDetailsRequest() : Request<IDeviceDetailsResponse>(HttpMethod.Get, "gestalt");

public record SetDeviceNameRequest(string Name) : Request<DeviceNameResponse>(HttpMethod.Post, "device_name");

public record GetDeviceNameRequest() : Request<DeviceNameResponse>(HttpMethod.Get, "device_name");

public record SetLedModeRequest(OperationMode Mode) : Request<LedOperationModeResponse>(HttpMethod.Post, "led/mode");

public record GetLedModeRequest() : Request<LedOperationModeResponse>(HttpMethod.Get, "led/mode");

public record SetHsvColorRequest(byte Hue, byte Saturation, byte Value)
    : Request<ICodeResponse>(HttpMethod.Post, "led/color");

public record SetRgbColorRequest(byte Red, byte Green, byte Blue) : Request<ICodeResponse>(HttpMethod.Post, "led/color");

public record SetRgbwColorRequest(
    byte Red,
    byte Green,
    byte Blue,
    byte White) : Request<ICodeResponse>(HttpMethod.Post, "led/color");

public record SetAwwColorRequest : SetRgbColorRequest
{
    public SetAwwColorRequest(byte amber, byte warmWhite, byte coldWhite) : base(amber, warmWhite, coldWhite)
    {
    }
}

public record GetColorRequest() : Request<ColorResponse>(HttpMethod.Get, "led/color");

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
