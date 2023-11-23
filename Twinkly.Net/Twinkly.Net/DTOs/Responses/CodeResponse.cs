using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;

namespace Twinkly.Net.DTOs.Responses;

public class CodeResponse
{
    public CodeResponse(ResponseCode code)
    {
        Code = code;
    }

    public ResponseCode Code { get; }

    public bool IsSuccess => Code is ResponseCode.Ok or ResponseCode.Ok1107 or ResponseCode.Ok1108;
}

public class LedOperationModeResponse : CodeResponse
{
    public LedOperationModeResponse(ResponseCode code, OperationMode mode) : base(code)
    {
        Mode = mode;
    }

    public OperationMode Mode { get; }
}

public class ColorResponse : CodeResponse
{
    public ColorResponse(ResponseCode code, int hue, int saturation, int value, int red, int green, int blue) : base(code)
    {
        Hue = hue;
        Saturation = saturation;
        Value = value;
        Red = red;
        Green = green;
        Blue = blue;
    }

    public int Hue { get; }
    public int Saturation { get; }
    public int Value { get; }

    public int Red { get; }
    public int Green { get; }
    public int Blue { get; }

}

public class BrightnessResponse : CodeResponse
{
    public BrightnessResponse(ResponseCode code, int brightness, string mode, string type) : base(code)
    {
        Brightness = brightness;
        Mode = mode switch
        {
            "enabled" => BrightnessMode.Enabled,
            "disabled" => BrightnessMode.Disabled,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
        Type = type switch
        {
            "A" => BrightnessType.Absolute,
            "R" => BrightnessType.Relative,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public int Brightness { get; }
    public BrightnessMode Mode { get; }
    public BrightnessType Type { get; }
}
