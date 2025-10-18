using System.Net.WebSockets;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;
using System.Text.Json.Serialization;

namespace Twinkly.Net.DTOs.Responses;

public interface ICodeResponse
{
    public ResponseCode Code { get; }
    public bool IsSuccess => Code is ResponseCode.Ok or ResponseCode.Ok1107 or ResponseCode.Ok1108;
}

public record LedOperationModeResponse(ResponseCode Code, OperationMode Mode) : ICodeResponse;

public record ColorResponse(
    ResponseCode Code,
    int Hue,
    int Saturation,
    int Value,
    int Red,
    int Green,
    int Blue) : ICodeResponse;

public record BrightnessResponse(
    ResponseCode Code,
    int Brightness,
    BrightnessMode Mode,
    BrightnessType Type
) : ICodeResponse;
