using System.Text.Json.Serialization;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net.DTOs.Requests;

public class LoginRequest : Request<LoginResponse>
{
    public LoginRequest(string challenge) : base(HttpMethod.Post, "login", false)
    {
        Challenge = challenge;
    }

    public string Challenge { get; set; }
}

public abstract class Request<TResponse> where TResponse : CodeResponse
{
    private readonly string _path;

    protected Request(HttpMethod method, string path, bool requiresAuthentication = true)
    {
        _path = path;
        Method = method;
        RequiresAuthentication = requiresAuthentication;
    }

    [JsonIgnore] public HttpMethod Method { get; }

    [JsonIgnore] public string Path => $"/xled/v1/{_path}";


    [JsonIgnore] public bool RequiresAuthentication { get; }
}

public class VerifyRequest : Request<CodeResponse>
{
    public VerifyRequest(string challengeResponse) : base(HttpMethod.Post, "verify")
    {
        ChallengeResponse = challengeResponse;
    }

    [JsonPropertyName("challenge-response")]
    public string ChallengeResponse { get; set; }
}

public class LogoutRequest : Request<CodeResponse>
{
    public LogoutRequest() : base(HttpMethod.Post, "logout")
    {
    }
}

public class DeviceDetailsRequest : Request<DeviceDetailsResponse>
{
    public DeviceDetailsRequest() : base(HttpMethod.Get, "gestalt")
    {
    }
}

public class SetDeviceNameRequest : Request<DeviceNameResponse>
{
    public SetDeviceNameRequest(string name) : base(HttpMethod.Post, "device_name")
    {
        Name = name;
    }

    public string Name { get; set; }
}

public class GetDeviceNameRequest : Request<DeviceNameResponse>
{
    public GetDeviceNameRequest() : base(HttpMethod.Get, "device_name")
    {
    }
}

public class SetLedModeRequest : Request<LedOperationModeResponse>
{
    public SetLedModeRequest(OperationMode mode) : base(HttpMethod.Post, "led/mode")
    {
        Mode = mode;
    }

    public OperationMode Mode { get; set; }
}

public class GetLedModeRequest : Request<LedOperationModeResponse>
{
    public GetLedModeRequest() : base(HttpMethod.Get, "led/mode")
    {
    }
}

public class SetHsvColorRequest : Request<CodeResponse>
{
    public SetHsvColorRequest(int hue, int saturation, int value) : base(HttpMethod.Post, "led/color")
    {
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public int Hue { get; set; }
    public int Saturation { get; set; }
    public int Value { get; set; }
}

public class SetRgbColorRequest : Request<CodeResponse>
{
    public SetRgbColorRequest(int red, int green, int blue) : base(HttpMethod.Post, "led/color")
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    public int Red { get; set; }
    public int Green { get; set; }
    public int Blue { get; set; }
}

public class GetColorRequest : Request<ColorResponse>
{
    public GetColorRequest() : base(HttpMethod.Get, "led/color")
    {
    }
}

public class SetBrightnessRequest : Request<CodeResponse>
{
    public SetBrightnessRequest(int brightness, BrightnessMode? mode = BrightnessMode.Enabled,
        BrightnessType? type = BrightnessType.Absolute) : this(brightness, type, mode)
    {
    }

    public SetBrightnessRequest(BrightnessMode mode) : this(null, null, mode)
    {
    }

    private SetBrightnessRequest(int? brightness, BrightnessType? type, BrightnessMode? mode) : base(HttpMethod.Post,
        "led/out/brightness")
    {
        Brightness = brightness;
        if (type.HasValue)
        {
            Type = type.Value switch
            {
                BrightnessType.Absolute => "A",
                BrightnessType.Relative => "R",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        Mode = mode?.ToString().ToLower();
    }

    public string? Mode { get; }

    public string? Type { get; }

    public int? Brightness { get; }
}

public enum BrightnessType
{
    /// <summary>
    /// Absolute brightness (0-100)
    /// </summary>
    Absolute,

    /// <summary>
    /// Relative brightness (-100 - 100)
    /// </summary>
    Relative
}

public enum BrightnessMode
{
    Enabled,
    Disabled
}

public class GetBrightnessRequest : Request<BrightnessResponse>
{
    public GetBrightnessRequest() : base(HttpMethod.Get, "led/out/brightness")
    {
    }
}
