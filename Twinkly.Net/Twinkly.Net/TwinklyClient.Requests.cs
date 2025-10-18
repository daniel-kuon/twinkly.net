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

    public Task SetDeviceName(string name) => ExecuteRequest(new SetDeviceNameRequest(name));

    public Task<string> GetDeviceName() => ExecuteRequest(new GetDeviceNameRequest()).ContinueWith(r => r.Result.DeviceName);

    public Task<OperationMode> GetLedMode() => ExecuteRequest(new GetLedModeRequest()).ContinueWith(r => r.Result.Mode);

    public Task SetHsvColor(byte hue, byte saturation, byte value) => SendColorRequest(new SetHsvColorRequest(hue, saturation, value));

    public Task SetRgbColor(byte red, byte green, byte blue) => SendColorRequest(new SetRgbColorRequest(red, green, blue));

    public Task SetRgbwColor(byte red, byte green, byte blue, byte white)
    {
        if(LedProfile != LedProfile.Rgbw)
            throw new Exception($"This device does not support RGBW. Current profile is {LedProfile}");

        return SendColorRequest(new SetRgbwColorRequest(red, green, blue, white));
    }

    public Task SetAwwColor(byte amber, byte warmWhite, byte coldWhite)
    {
        if(LedProfile != LedProfile.Aww)
            throw new Exception($"This device does not support AWW. Current profile is {LedProfile}");

        return SendColorRequest(new SetAwwColorRequest(amber, warmWhite, coldWhite));
    }

    public Task<ColorResponse> GetColor() => ExecuteRequest(new GetColorRequest());

    public Task SetBrightness(int brightness) =>  SendBrightnessRequest(new SetBrightnessRequest(brightness));

    public Task SetBrightness(BrightnessMode mode) =>  SendBrightnessRequest(new SetBrightnessRequest(mode));

    public  Task SetBrightness(int brightness, BrightnessMode mode) => SendBrightnessRequest(new SetBrightnessRequest(brightness, mode));

    public Task SetBrightness(int brightness, BrightnessMode mode, BrightnessType type) => SendBrightnessRequest(new SetBrightnessRequest(brightness, mode, type));

    public Task<BrightnessResponse> GetBrightness() => ExecuteRequest(new GetBrightnessRequest());

}
